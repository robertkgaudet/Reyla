using System;
using System.Collections.Generic;
using Reyla.Services.Models;

namespace Reyla.Services
{
    /// <summary>
    /// Composite return object from PropertySnapshotService.GetProfile().
    /// Contains the property record, the snapshot ID, and all 13 motivated-seller
    /// data points. Any individual result may be null if the CoreLogic endpoint
    /// returned no data or failed for that property.
    /// </summary>
    public class PropertyProfileResult
    {
        // ---------------------------------------------------------------
        // Meta
        // ---------------------------------------------------------------

        /// <summary>True if the property was not found in CoreLogic.</summary>
        public bool PropertyNotFound { get; set; }

        /// <summary>True if data was loaded from DB cache rather than a live API pull.</summary>
        public bool IsFromCache { get; set; }

        /// <summary>The PropertySnapshotId that was created or loaded from cache.</summary>
        public Guid PropertySnapshotId { get; set; }

        // ---------------------------------------------------------------
        // Core record
        // ---------------------------------------------------------------

        public Property Property { get; set; }

        // ---------------------------------------------------------------
        // 13 API result slots
        // ---------------------------------------------------------------

        public OwnershipResult              Ownership              { get; set; }
        public OwnershipTransfersResult     OwnershipTransfers     { get; set; }
        public MortgageResult               Mortgage               { get; set; }
        public EnrichedVoluntaryLiensResult EnrichedVoluntaryLiens { get; set; }
        public InvoluntaryLiensResult       InvoluntaryLiens       { get; set; }
        public TaxAssessmentResult          TaxAssessment          { get; set; }
        public BuildingResult               Building               { get; set; }
        public BuildingPermitsResult        BuildingPermits        { get; set; }
        public AvmResult                    Avm                    { get; set; }
        public PropensityResult             Propensity             { get; set; }
        public HoaResult                    Hoa                    { get; set; }
        public ClimateRiskResult            ClimateRisk            { get; set; }

        /// <summary>
        /// Raw response from /property-detail composite endpoint.
        /// Populated during a live API pull; used by BackfillFromPropertyDetail
        /// to fill gaps when individual endpoints return no data.
        /// Not persisted directly — its data flows into the standard slots above.
        /// </summary>
        public PropertyDetailResponse       PropertyDetail         { get; set; }

        // ---------------------------------------------------------------
        // Convenience properties — pre-calculated motivated-seller signals
        // ---------------------------------------------------------------

        /// <summary>True if occupancyCode indicates absentee owner (non-resident).</summary>
        public bool IsAbsenteeOwner =>
            string.Equals(
                Ownership?.Data?.CurrentOwners?.OccupancyCode, "Y",
                StringComparison.OrdinalIgnoreCase);

        /// <summary>True if any involuntary liens (tax liens, judgments) exist.</summary>
        public bool HasInvoluntaryLiens =>
            InvoluntaryLiens?.Data != null && InvoluntaryLiens.Data.Count > 0;

        /// <summary>True if tax assessment shows a delinquent year.</summary>
        public bool HasTaxDelinquency =>
            TaxAssessment?.Items?.Count > 0 &&
            TaxAssessment.Items[0]?.TaxAmount?.DelinquentYear.HasValue == true &&
            TaxAssessment.Items[0].TaxAmount.DelinquentYear.Value > 0;

        /// <summary>True if any building permits are open or expired.</summary>
        public bool HasOpenOrExpiredPermits =>
            BuildingPermits?.Data != null &&
            BuildingPermits.Data.Exists(p =>
                p.Status != null &&
                (p.Status.IndexOf("Open",    StringComparison.OrdinalIgnoreCase) >= 0 ||
                 p.Status.IndexOf("Expired", StringComparison.OrdinalIgnoreCase) >= 0));

        /// <summary>CoreLogic propensity score — higher = more likely to sell.</summary>
        public decimal? PropensityScore =>
            Propensity?.Data?.PropensityScore;

        /// <summary>Estimated equity: AVM value minus total outstanding mortgage balances.</summary>
        public decimal? EstimatedEquity
        {
            get
            {
                if (Avm?.Data?.EstimatedValue == null) return null;

                decimal totalDebt = 0;
                if (Mortgage?.Items != null)
                    foreach (var item in Mortgage.Items)
                        if (item?.TransactionDetail?.Amount.HasValue == true)
                            totalDebt += item.TransactionDetail.Amount.Value;

                return Avm.Data.EstimatedValue - totalDebt;
            }
        }

        // ---------------------------------------------------------------
        // Static factory for not-found case
        // ---------------------------------------------------------------

        public static PropertyProfileResult NotFound()
        {
            return new PropertyProfileResult { PropertyNotFound = true };
        }
    }
}
