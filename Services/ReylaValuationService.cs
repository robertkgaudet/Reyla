using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Reyla.Services.Models;

namespace Reyla.Services
{
    /// <summary>
    /// Computes the Reyla Valuation — a broker-curated AVM estimate for a subject property.
    ///
    /// Algorithm:
    ///   1. Load the prospect's saved comps from ProspectCompsCache (the JSON the user
    ///      reviewed and approved on Comps.aspx).
    ///   2. Filter to comps that have both SalePrice and BuildingSquareFeet.
    ///   3. Weight each comp by two factors:
    ///        - Recency:  sales within 6 months = 1.0, 6-12 mo = 0.75, 12-18 mo = 0.5, older = 0.25
    ///        - Distance: 0-0.1 mi = 1.0, 0.1-0.25 mi = 0.85, 0.25-0.5 mi = 0.65, beyond = 0.4
    ///   4. Compute a weighted median PPSF from the qualifying comps.
    ///   5. Multiply by subject BuildingSquareFeet.
    ///   6. Apply a year-built adjustment: ±0.5% per decade difference from subject.
    ///   7. Return a point estimate and ±10% confidence range, plus comp breakdown.
    /// </summary>
    public class ReylaValuationService
    {
        // ---------------------------------------------------------------
        // Result
        // ---------------------------------------------------------------

        public class ValuationResult
        {
            public bool    Success            { get; set; }
            public string  ErrorMessage       { get; set; }

            // Core estimate
            public decimal PointEstimate      { get; set; }   // mid-point
            public decimal RangeLow           { get; set; }   // -10%
            public decimal RangeHigh          { get; set; }   // +10%
            public decimal WeightedMedianPpsf { get; set; }
            public int     SubjectSqFt        { get; set; }
            public int?    SubjectYearBuilt   { get; set; }
            public int     CompsUsed          { get; set; }
            public int     CompsAvailable     { get; set; }

            // Confidence: "High" (10+ comps), "Medium" (5-9), "Low" (<5)
            public string  ConfidenceLabel    { get; set; }
            public string  ConfidenceColor    { get; set; }  // Bootstrap text-bg-* suffix

            // Per-comp breakdown for transparency
            public List<CompWeight> CompBreakdown { get; set; } = new List<CompWeight>();

            public static ValuationResult Fail(string msg) =>
                new ValuationResult { Success = false, ErrorMessage = msg };
        }

        public class CompWeight
        {
            public string  Address       { get; set; }
            public string  CityLine      { get; set; }
            public decimal SalePrice     { get; set; }
            public int     SqFt          { get; set; }
            public decimal Ppsf          { get; set; }
            public decimal Distance      { get; set; }
            public string  SaleDate      { get; set; }
            public int?    YearBuilt     { get; set; }
            public decimal RecencyWeight { get; set; }
            public decimal DistanceWeight{ get; set; }
            public decimal TotalWeight   { get; set; }
        }

        // ---------------------------------------------------------------
        // Entry point
        // ---------------------------------------------------------------

        public ValuationResult Calculate(Guid prospectId, Guid organizationId)
        {
            using (var db = new DCReyla())
            {
                // Load prospect and its subject property
                var prospect = db.Prospects
                    .FirstOrDefault(p =>
                        p.ProspectId     == prospectId     &&
                        p.OrganizationId == organizationId &&
                        p.IsDeleted      == false);

                if (prospect == null)
                    return ValuationResult.Fail("Prospect not found.");

                var property = prospect.Property;
                if (property == null)
                    return ValuationResult.Fail("Subject property not found.");

                // Get subject sqft and year built from latest snapshot
                var snapshot = db.PropertySnapshots
                    .Where(s => s.PropertyId == property.PropertyId && s.IsDeleted == false)
                    .OrderByDescending(s => s.CreatedAtUtc)
                    .FirstOrDefault();

                var bldg = snapshot != null
                    ? db.PropertySnapshotBuildings
                          .FirstOrDefault(b => b.PropertySnapshotId == snapshot.PropertySnapshotId)
                    : null;

                int subjectSqFt = bldg?.GrossLivingArea.HasValue == true && bldg.GrossLivingArea.Value > 0
                    ? (int)bldg.GrossLivingArea.Value
                    : 0;

                int? subjectYearBuilt = bldg?.YearBuilt.HasValue == true && bldg.YearBuilt.Value > 0
                    ? bldg.YearBuilt.Value
                    : (int?)null;

                if (subjectSqFt <= 0)
                    return ValuationResult.Fail("Subject property square footage is not available. Try refreshing data.");

                // Load the user's curated comp set from Prospect.SelectedCompsJson
                // Falls back to ProspectCompsCache if SelectedCompsJson not yet populated
                // (supports prospects created before this feature was added)
                string compsJson = null;

                if (!string.IsNullOrEmpty(prospect.SelectedCompsJson))
                {
                    compsJson = prospect.SelectedCompsJson;
                }
                else
                {
                    var cache = db.ProspectCompsCaches
                        .Where(c => c.ProspectId == prospectId)
                        .OrderByDescending(c => c.CreatedAtUtc)
                        .FirstOrDefault();
                    compsJson = cache?.CompsJson;
                }

                if (string.IsNullOrEmpty(compsJson))
                    return ValuationResult.Fail("No saved comparables found. Go to Comps and save a selection first.");

                List<ComparableProperty> comps;
                try { comps = JsonConvert.DeserializeObject<List<ComparableProperty>>(compsJson); }
                catch { return ValuationResult.Fail("Could not load saved comparables."); }

                if (comps == null || !comps.Any())
                    return ValuationResult.Fail("No comparables in cache.");

                int totalAvailable = comps.Count;

                // Filter: must have SalePrice, SqFt, and SaleDate
                var usable = comps
                    .Where(c => c.SalePrice.HasValue && c.SalePrice > 0
                             && c.BuildingSquareFeet.HasValue && c.BuildingSquareFeet > 0
                             && !string.IsNullOrEmpty(c.SaleDate))
                    .ToList();

                if (!usable.Any())
                    return ValuationResult.Fail("None of the saved comparables have the required sale price and square footage data.");

                var now = DateTime.UtcNow;
                var breakdown = new List<CompWeight>();

                foreach (var comp in usable)
                {
                    var ppsf     = comp.SalePrice.Value / comp.BuildingSquareFeet.Value;
                    var recency  = GetRecencyWeight(comp.SaleDate, now);
                    var distWt   = GetDistanceWeight(comp.Distance);
                    var total    = recency * distWt;

                    int? compYear = null;
                    if (int.TryParse(comp.YearBuilt, out int y) && y > 0) compYear = y;

                    breakdown.Add(new CompWeight
                    {
                        Address        = comp.StreetAddress ?? "",
                        CityLine       = string.Join(", ", new[]{ comp.City, comp.State, comp.ZipCode }
                                            .Where(s => !string.IsNullOrEmpty(s))),
                        SalePrice      = comp.SalePrice.Value,
                        SqFt           = comp.BuildingSquareFeet.Value,
                        Ppsf           = Math.Round(ppsf, 2),
                        Distance       = comp.Distance ?? 0,
                        SaleDate       = comp.SaleDate,
                        YearBuilt      = compYear,
                        RecencyWeight  = recency,
                        DistanceWeight = distWt,
                        TotalWeight    = Math.Round(total, 3)
                    });
                }

                // Weighted median PPSF
                decimal weightedMedianPpsf = ComputeWeightedMedianPpsf(breakdown);

                // Year-built adjustment: ±0.5% per decade
                decimal yearAdjustment = 1.0m;
                if (subjectYearBuilt.HasValue)
                {
                    // Use median year built of comps as the baseline
                    var compYears = breakdown
                        .Where(b => b.YearBuilt.HasValue)
                        .Select(b => (decimal)b.YearBuilt.Value)
                        .OrderBy(v => v)
                        .ToList();

                    if (compYears.Any())
                    {
                        decimal medianCompYear = compYears[compYears.Count / 2];
                        decimal decadeDiff     = ((decimal)subjectYearBuilt.Value - medianCompYear) / 10m;
                        yearAdjustment         = 1m + (decadeDiff * 0.005m);  // 0.5% per decade
                        yearAdjustment         = Math.Max(0.85m, Math.Min(1.15m, yearAdjustment)); // cap ±15%
                    }
                }

                decimal adjustedPpsf  = weightedMedianPpsf * yearAdjustment;
                decimal pointEstimate = adjustedPpsf * subjectSqFt;

                // Confidence
                int     compsUsed = breakdown.Count;
                string  confLabel, confColor;
                if      (compsUsed >= 10) { confLabel = "High";   confColor = "success"; }
                else if (compsUsed >= 5)  { confLabel = "Medium"; confColor = "warning"; }
                else                      { confLabel = "Low";    confColor = "danger";  }

                return new ValuationResult
                {
                    Success            = true,
                    PointEstimate      = Math.Round(pointEstimate / 1000m) * 1000m,   // round to nearest $1k
                    RangeLow           = Math.Round(pointEstimate * 0.90m / 1000m) * 1000m,
                    RangeHigh          = Math.Round(pointEstimate * 1.10m / 1000m) * 1000m,
                    WeightedMedianPpsf = Math.Round(adjustedPpsf, 2),
                    SubjectSqFt        = subjectSqFt,
                    SubjectYearBuilt   = subjectYearBuilt,
                    CompsUsed          = compsUsed,
                    CompsAvailable     = totalAvailable,
                    ConfidenceLabel    = confLabel,
                    ConfidenceColor    = confColor,
                    CompBreakdown      = breakdown.OrderByDescending(b => b.TotalWeight).ToList()
                };
            }
        }

        // ---------------------------------------------------------------
        // Weighted median PPSF
        // Sort comps by PPSF, then find the point where cumulative weight
        // crosses 50% of total weight — that's the weighted median.
        // ---------------------------------------------------------------

        private decimal ComputeWeightedMedianPpsf(List<CompWeight> comps)
        {
            var sorted     = comps.OrderBy(c => c.Ppsf).ToList();
            decimal total  = sorted.Sum(c => c.TotalWeight);
            decimal cumul  = 0m;
            decimal target = total / 2m;

            foreach (var c in sorted)
            {
                cumul += c.TotalWeight;
                if (cumul >= target)
                    return c.Ppsf;
            }

            return sorted.Last().Ppsf;
        }

        // ---------------------------------------------------------------
        // Recency weight — how recent is the sale?
        // ---------------------------------------------------------------

        private decimal GetRecencyWeight(string saleDateStr, DateTime now)
        {
            if (string.IsNullOrEmpty(saleDateStr)) return 0.25m;

            // Parse YYYYMMDD or YYYY-MM-DD
            string clean = saleDateStr.Replace("-", "");
            if (clean.Length < 8) return 0.25m;

            if (!DateTime.TryParseExact(clean, "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime saleDate))
                return 0.25m;

            double months = (now - saleDate).TotalDays / 30.44;

            if      (months <=  6) return 1.00m;
            else if (months <= 12) return 0.75m;
            else if (months <= 18) return 0.50m;
            else if (months <= 36) return 0.30m;
            else                   return 0.15m;
        }

        // ---------------------------------------------------------------
        // Distance weight — how close is the comp?
        // ---------------------------------------------------------------

        private decimal GetDistanceWeight(decimal? distanceMiles)
        {
            if (!distanceMiles.HasValue) return 0.50m;

            double d = (double)distanceMiles.Value;

            if      (d <= 0.10) return 1.00m;
            else if (d <= 0.25) return 0.85m;
            else if (d <= 0.50) return 0.65m;
            else if (d <= 0.75) return 0.45m;
            else                return 0.30m;
        }
    }
}
