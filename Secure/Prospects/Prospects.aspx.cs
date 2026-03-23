using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Reyla.Secure.Prospects
{
    public partial class ProspectsList : System.Web.UI.Page
    {
        // ── View model ────────────────────────────────────────────────────

        private class ProspectRow
        {
            public Guid      ProspectId     { get; set; }
            public Guid      PropertyId     { get; set; }
            public string    Clip           { get; set; }
            public string    StreetAddress  { get; set; }
            public string    CityLine       { get; set; }   // "Lafayette, LA 70501"
            public string    Status         { get; set; }
            public DateTime  CreatedAtUtc   { get; set; }
            public int?      SqFt           { get; set; }
            public int?      YearBuilt      { get; set; }
            public DateTime? LastSaleDate   { get; set; }
            public decimal?  LastSaleAmount { get; set; }
            public int?      CompCount      { get; set; }   // null = comps not yet fetched
        }

        // ── Auth ──────────────────────────────────────────────────────────

        private Guid CurrentUserId
        {
            get
            {
                var user = System.Web.Security.Membership.GetUser();
                return user != null ? (Guid)user.ProviderUserKey : Guid.Empty;
            }
        }

        private Guid CurrentOrganizationId
        {
            get
            {
                using (var db = new DCReyla())
                {
                    var uid     = CurrentUserId;
                    var profile = db.Profiles.FirstOrDefault(p => p.UserId == uid);
                    return profile?.OrganizationId ?? Guid.Empty;
                }
            }
        }

        // ── Page lifecycle ────────────────────────────────────────────────

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                BindProspects();
        }

        // ── Data load ─────────────────────────────────────────────────────
        // DataTables handles client-side search and status filtering, so we
        // always render ALL rows and let the browser do the filtering work.
        // No postback filtering needed.

        private void BindProspects()
        {
            var orgId = CurrentOrganizationId;
            if (orgId == Guid.Empty)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            var rows = LoadRows(orgId);

            // Summary stat counters (always based on unfiltered full list)
            litTotalCount.Text  = $"<span>{rows.Count}</span>";
            litNewCount.Text    = $"<span>{rows.Count(r => r.Status == "New")}</span>";
            litActiveCount.Text = $"<span>{rows.Count(r => r.Status == "Active")}</span>";
            litClosedCount.Text = $"<span>{rows.Count(r => r.Status == "Closed")}</span>";
            litSubtitle.Text    = $"{rows.Count} propert{(rows.Count == 1 ? "y" : "ies")} tracked by your organization";

            if (rows.Count == 0)
            {
                pnlTable.Visible = false;
                pnlEmpty.Visible = true;
            }
            else
            {
                pnlTable.Visible = true;
                pnlEmpty.Visible = false;
                rptProspects.DataSource = rows;
                rptProspects.DataBind();
            }
        }

        private List<ProspectRow> LoadRows(Guid orgId)
        {
            using (var db = new DCReyla())
            {
                // All active prospects for this org, newest first
                var prospects = db.Prospects
                    .Where(p => p.OrganizationId == orgId && p.IsDeleted == false)
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .ToList();

                // Load comp cache counts for all prospects in one query
                var now         = DateTime.UtcNow;
                var prospectIds = prospects.Select(p => p.ProspectId).ToList();
                var compCaches  = db.ProspectCompsCaches
                    .Where(c => prospectIds.Contains(c.ProspectId) && c.ExpiresAtUtc > now)
                    .Select(c => new { c.ProspectId, c.CompsJson })
                    .ToList();

                var compCountByProspectId = new Dictionary<Guid, int>();
                foreach (var cache in compCaches)
                {
                    if (compCountByProspectId.ContainsKey(cache.ProspectId)) continue;
                    try
                    {
                        var arr = Newtonsoft.Json.Linq.JArray.Parse(cache.CompsJson ?? "[]");
                        compCountByProspectId[cache.ProspectId] = arr.Count;
                    }
                    catch { compCountByProspectId[cache.ProspectId] = 0; }
                }

                var rows = new List<ProspectRow>();

                foreach (var prospect in prospects)
                {
                    var prop = db.Properties.FirstOrDefault(p => p.PropertyId == prospect.PropertyId);
                    if (prop == null) continue;

                    // City line from City_Extended lookup
                    string cityLine = string.Empty;
                    if (prop.CityExtendedId.HasValue)
                    {
                        var ce = db.City_Extendeds.FirstOrDefault(
                            c => c.CityExtendedId == prop.CityExtendedId.Value);
                        if (ce != null)
                            cityLine = $"{ce.City}, {ce.Code} {ce.Zip}".Trim();
                    }
                    if (string.IsNullOrWhiteSpace(cityLine))
                        cityLine = ((prop.CityNameRaw ?? string.Empty) + " " +
                                    (prop.ZipCodeRaw  ?? string.Empty)).Trim();

                    // Latest snapshot for this org
                    var snapshot = db.PropertySnapshots
                        .Where(s =>
                            s.PropertyId     == prop.PropertyId &&
                            s.OrganizationId == orgId           &&
                            s.IsDeleted      == false)
                        .OrderByDescending(s => s.PulledAtUtc)
                        .FirstOrDefault();

                    // Building data
                    int? sqFt      = null;
                    int? yearBuilt = null;
                    if (snapshot != null)
                    {
                        var bldg = db.PropertySnapshotBuildings
                            .FirstOrDefault(b => b.PropertySnapshotId == snapshot.PropertySnapshotId);
                        if (bldg != null)
                        {
                            sqFt      = bldg.GrossLivingArea.HasValue
                                        ? (int?)((int)bldg.GrossLivingArea.Value) : null;
                            yearBuilt = bldg.YearBuilt;
                        }
                    }

                    // Last sale from ownership transfers
                    DateTime? lastSaleDate   = null;
                    decimal?  lastSaleAmount = null;
                    if (snapshot != null)
                    {
                        var lastTransfer = db.PropertySnapshotOwnershipTransfers
                            .Where(t => t.PropertySnapshotId == snapshot.PropertySnapshotId
                                     && t.SaleDate    != null
                                     && t.SaleAmount  > 0)
                            .OrderByDescending(t => t.SaleDate)
                            .FirstOrDefault();
                        if (lastTransfer != null)
                        {
                            lastSaleDate   = lastTransfer.SaleDate;
                            lastSaleAmount = lastTransfer.SaleAmount;
                        }
                    }

                    rows.Add(new ProspectRow
                    {
                        ProspectId     = prospect.ProspectId,
                        PropertyId     = prop.PropertyId,
                        Clip           = prop.Clip ?? string.Empty,
                        StreetAddress  = prop.StreetAddress ?? string.Empty,
                        CityLine       = cityLine,
                        Status         = prospect.Status ?? "New",
                        CreatedAtUtc   = prospect.CreatedAtUtc,
                        SqFt           = sqFt,
                        YearBuilt      = yearBuilt,
                        LastSaleDate   = lastSaleDate,
                        LastSaleAmount = lastSaleAmount,
                        CompCount      = compCountByProspectId.ContainsKey(prospect.ProspectId)
                                         ? compCountByProspectId[prospect.ProspectId]
                                         : (int?)null
                    });
                }

                return rows;
            }
        }

        // ── Repeater item bound ───────────────────────────────────────────

        protected void rptProspects_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item &&
                e.Item.ItemType != ListItemType.AlternatingItem) return;

            var row      = (ProspectRow)e.Item.DataItem;
            var lnkComps = (HyperLink)e.Item.FindControl("lnkComps");

            if (lnkComps != null)
            {
                lnkComps.Text = row.CompCount.HasValue
                    ? $"Comps ({row.CompCount.Value})"
                    : "Comps";
                lnkComps.NavigateUrl = ResolveUrl(
                    $"~/Secure/Prospects/Comps.aspx?prospectId={row.ProspectId}");
            }
        }

        // ── Format helpers ────────────────────────────────────────────────

        /// <summary>Returns the raw $/sqft decimal for use in data-order attributes.</summary>
        protected string GetPpsfRaw(object saleAmountVal, object sqFtVal)
        {
            if (saleAmountVal == null || sqFtVal == null) return "0";
            if (!decimal.TryParse(saleAmountVal.ToString(), out decimal amount) || amount <= 0) return "0";
            if (!int.TryParse(sqFtVal.ToString(), out int sqft) || sqft <= 0) return "0";
            return Math.Round(amount / sqft, 2).ToString("F2");
        }

        protected string GetStatusBadge(string status)
        {
            switch (status ?? "New")
            {
                case "Active": return "<span class='badge text-bg-success'>Active</span>";
                case "Closed": return "<span class='badge text-bg-secondary'>Closed</span>";
                case "Lost":   return "<span class='badge text-bg-danger'>Lost</span>";
                default:       return "<span class='badge text-bg-primary'>New</span>";
            }
        }

        protected string FormatCompCount(object val)
        {
            if (val == null || val == DBNull.Value)
                return "<span class='na' title='Open Comps page to load'>—</span>";
            if (int.TryParse(val.ToString(), out int n))
                return n > 0
                    ? $"<span class='comps-count'>{n}</span>"
                    : "<span class='comps-count zero'>0</span>";
            return "<span class='na'>—</span>";
        }

        protected string FormatInt(object val)
        {
            if (val == null || val == DBNull.Value) return "<span class='na'>—</span>";
            if (int.TryParse(val.ToString(), out int i) && i > 0)
                return i.ToString("N0");
            return "<span class='na'>—</span>";
        }

        protected string FormatDate(object val)
        {
            if (val == null || val == DBNull.Value) return "<span class='na'>—</span>";
            if (val is DateTime dt && dt != DateTime.MinValue)
                return dt.ToString("MMM yyyy");
            return "<span class='na'>—</span>";
        }

        protected string FormatMoney(object val)
        {
            if (val == null || val == DBNull.Value) return "<span class='na'>—</span>";
            if (decimal.TryParse(val.ToString(), out decimal d) && d > 0)
                return "$" + d.ToString("N0");
            return "<span class='na'>—</span>";
        }

        protected string FormatPpsf(object saleAmountVal, object sqFtVal)
        {
            if (saleAmountVal == null || sqFtVal == null) return "<span class='na'>—</span>";
            if (!decimal.TryParse(saleAmountVal.ToString(), out decimal amount) || amount <= 0)
                return "<span class='na'>—</span>";
            if (!int.TryParse(sqFtVal.ToString(), out int sqft) || sqft <= 0)
                return "<span class='na'>—</span>";
            return "$" + (amount / sqft).ToString("N0");
        }
    }
}
