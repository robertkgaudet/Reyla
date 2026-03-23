using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.UI;
using Newtonsoft.Json;
using Reyla.Services;
using Reyla.Services.Models;

namespace Reyla.Secure.Prospects
{
    public partial class Comps : Page
    {
        // ---------------------------------------------------------------
        // Default comp search settings — read from web.config
        // ---------------------------------------------------------------

        private static decimal DefaultRadiusMiles
        {
            get
            {
                decimal v;
                return decimal.TryParse(
                    ConfigurationManager.AppSettings["Prospect.DefaultRadiusMiles"], out v) ? v : 0.5m;
            }
        }

        private static int DefaultMaxComps
        {
            get
            {
                int v;
                return int.TryParse(
                    ConfigurationManager.AppSettings["Prospect.DefaultMaxComps"], out v) ? v : 25;
            }
        }

        private static int DefaultMonthsBack
        {
            get
            {
                int v;
                return int.TryParse(
                    ConfigurationManager.AppSettings["Prospect.DefaultMonthsBack"], out v) ? v : 12;
            }
        }

        // ---------------------------------------------------------------
        // Current user helpers
        // ---------------------------------------------------------------

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
                    var userId  = CurrentUserId;
                    var profile = db.Profiles.FirstOrDefault(p => p.UserId == userId);
                    return profile?.OrganizationId ?? Guid.Empty;
                }
            }
        }

        // ---------------------------------------------------------------
        // Page Load
        // ---------------------------------------------------------------

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                string eventTarget = Request.Form["__EVENTTARGET"] ?? string.Empty;
                if (eventTarget == "SaveComps")    HandleSaveComps();
                if (eventTarget == "RefreshComps") HandleRefreshComps();
                return;
            }

            // First load — read prospectId from querystring
            string prospectIdStr = Request.QueryString["prospectId"];

            if (!Guid.TryParse(prospectIdStr, out Guid prospectId))
            {
                SetMessage("Invalid prospect ID.", "danger");
                return;
            }

            Guid organizationId = CurrentOrganizationId;
            Guid userId         = CurrentUserId;

            if (organizationId == Guid.Empty || userId == Guid.Empty)
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            // Load subject property from DB
            using (var db = new DCReyla())
            {
                var prospect = db.Prospects
                    .FirstOrDefault(p =>
                        p.ProspectId     == prospectId     &&
                        p.OrganizationId == organizationId &&
                        p.IsDeleted      == false);

                if (prospect == null)
                {
                    SetMessage("Prospect not found.", "danger");
                    return;
                }

                var property = prospect.Property;

                // Populate subject hidden fields
                hdnProspectId.Value  = prospectId.ToString();
                hdnPropertyId.Value  = property.PropertyId.ToString();
                hdnSubjectClip.Value = property.Clip;
                hdnSubjectLat.Value  = property.Latitude.ToString();
                hdnSubjectLng.Value  = property.Longitude.ToString();
                hdnSubjectAddr.Value = property.StreetAddress +
                    (string.IsNullOrEmpty(property.CityNameRaw) ? "" : ", " + property.CityNameRaw) +
                    (string.IsNullOrEmpty(property.ZipCodeRaw)  ? "" : " " + property.ZipCodeRaw);
                hdnRadiusMiles.Value = DefaultRadiusMiles.ToString();

                // Subject stat tags — pull from the latest PropertySnapshot
                // Non-fatal: if this fails, tags simply won't show but comps still load.
                try
                {
                    var snapshot = db.PropertySnapshots
                        .Where(s => s.PropertyId == property.PropertyId)
                        .OrderByDescending(s => s.CreatedAtUtc)
                        .FirstOrDefault();

                    if (snapshot != null)
                    {
                        var bldg = db.PropertySnapshotBuildings
                            .Where(b => b.PropertySnapshotId == snapshot.PropertySnapshotId)
                            .FirstOrDefault();

                        var transfer = db.PropertySnapshotOwnershipTransfers
                            .Where(t => t.PropertySnapshotId == snapshot.PropertySnapshotId)
                            .OrderByDescending(t => t.SaleDate)
                            .FirstOrDefault();

                        if (bldg != null)
                        {
                            if (bldg.GrossLivingArea.HasValue && bldg.GrossLivingArea.Value > 0)
                                hdnSubjectSqFt.Value = ((int)bldg.GrossLivingArea.Value).ToString();

                            if (bldg.YearBuilt.HasValue && bldg.YearBuilt.Value > 0)
                                hdnSubjectYearBuilt.Value = bldg.YearBuilt.Value.ToString();
                        }

                        if (transfer != null)
                        {
                            if (transfer.SaleAmount.HasValue && transfer.SaleAmount.Value > 0)
                                hdnSubjectSalePrice.Value = transfer.SaleAmount.Value.ToString("F2");

                            if (transfer.SaleDate.HasValue)
                                hdnSubjectSaleDate.Value = transfer.SaleDate.Value.ToString("yyyyMMdd");

                            if (transfer.SaleAmount.HasValue && transfer.SaleAmount.Value > 0
                                && bldg?.GrossLivingArea.HasValue == true && bldg.GrossLivingArea.Value > 0)
                            {
                                var ppsf = transfer.SaleAmount.Value / (decimal)bldg.GrossLivingArea.Value;
                                hdnSubjectPpsf.Value = Math.Round(ppsf, 2).ToString("F2");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning("[Comps] Subject stat lookup failed (non-fatal): {0}", ex.Message);
                }
            }

            // Populate Street View API key for client-side use
            hdnStreetViewKey.Value =
                System.Configuration.ConfigurationManager.AppSettings["Google.StreetViewApiKey"] ?? string.Empty;

            // Fetch comps — from cache if fresh, otherwise from CoreLogic
            FetchAndStoreComps(prospectId, hdnSubjectClip.Value);
        }

        // ---------------------------------------------------------------
        // Fetch comps — serves from ProspectCompsCache if fresh,
        // otherwise calls CoreLogic and writes a new cache entry.
        // Cache expiry is controlled by Prospect.CompsCacheExpiryDays
        // in web.config (default 7 days).
        // ---------------------------------------------------------------

        private static int CompsCacheExpiryDays
        {
            get
            {
                int v;
                return int.TryParse(
                    ConfigurationManager.AppSettings["Prospect.CompsCacheExpiryDays"], out v) ? v : 7;
            }
        }

        private void FetchAndStoreComps(Guid prospectId, string clip)
        {
            if (string.IsNullOrEmpty(clip)) return;

            try
            {
                var now = DateTime.UtcNow;

                // Check cache first
                using (var db = new DCReyla())
                {
                    var cached = db.ProspectCompsCaches
                        .Where(c => c.ProspectId == prospectId && c.ExpiresAtUtc > now)
                        .OrderByDescending(c => c.CreatedAtUtc)
                        .FirstOrDefault();

                    if (cached != null)
                    {
                        hdnCompsJson.Value = cached.CompsJson;
                        return;
                    }
                }

                // Cache miss — call CoreLogic
                var api    = new CoreLogicPropertyService();
                var result = api.GetComparables(
                    clip,
                    searchDistance : DefaultRadiusMiles,
                    maxComps       : DefaultMaxComps,
                    monthsBack     : DefaultMonthsBack);

                var comps = result?.Comparables ?? new List<ComparableProperty>();

                // Filter out the subject property itself if CoreLogic includes it
                comps = comps.Where(c => c.Clip != clip).ToList();

                // Calculate $/sqft for any comps where CoreLogic did not provide it
                foreach (var comp in comps)
                {
                    if (comp.PricePerSquareFoot == null
                        && comp.SalePrice != null
                        && comp.BuildingSquareFeet != null
                        && comp.BuildingSquareFeet > 0)
                    {
                        comp.PricePerSquareFoot =
                            Math.Round(comp.SalePrice.Value / comp.BuildingSquareFeet.Value, 2);
                    }
                }

                var json = JsonConvert.SerializeObject(comps);

                // Write to cache
                using (var db = new DCReyla())
                {
                    db.ProspectCompsCaches.InsertOnSubmit(new ProspectCompsCache
                    {
                        ProspectCompsCacheId = Guid.NewGuid(),
                        ProspectId           = prospectId,
                        CompsJson            = json,
                        CreatedAtUtc         = now,
                        ExpiresAtUtc         = now.AddDays(CompsCacheExpiryDays)
                    });
                    db.SubmitChanges();
                }

                hdnCompsJson.Value = json;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "[Comps] FetchAndStoreComps error for Clip {0}: {1}", clip, ex.Message);
                hdnCompsJson.Value = "[]";
                SetMessage("Could not load comparables: " + ex.Message, "danger");
            }
        }

        // ---------------------------------------------------------------
        // Refresh comps — deletes cache row then redirects to self,
        // forcing a fresh CoreLogic call on the next Page_Load.
        // ---------------------------------------------------------------

        private void HandleRefreshComps()
        {
            string prospectIdStr = hdnProspectId.Value?.Trim();

            if (!Guid.TryParse(prospectIdStr, out Guid prospectId))
            {
                SetMessage("Invalid prospect ID.", "danger");
                return;
            }

            using (var db = new DCReyla())
            {
                var stale = db.ProspectCompsCaches
                    .Where(c => c.ProspectId == prospectId)
                    .ToList();

                db.ProspectCompsCaches.DeleteAllOnSubmit(stale);
                db.SubmitChanges();
            }

            // Redirect to self — Page_Load will see no cache and call CoreLogic fresh
            Response.Redirect(
                string.Format("~/Secure/Prospects/Comps.aspx?prospectId={0}", prospectId),
                endResponse: true);
        }

        // ---------------------------------------------------------------
        // Save selected comps postback
        // ---------------------------------------------------------------

        private void HandleSaveComps()
        {
            string prospectIdStr   = hdnProspectId.Value?.Trim();
            string selectedClipsJson = hdnSelectedClips.Value?.Trim();
            string compsJson       = hdnCompsJson.Value?.Trim();

            if (!Guid.TryParse(prospectIdStr, out Guid prospectId))
            {
                SetMessage("Invalid prospect ID.", "danger");
                return;
            }

            List<string> selectedClips;
            List<ComparableProperty> allComps;

            try
            {
                selectedClips = JsonConvert.DeserializeObject<List<string>>(selectedClipsJson)
                                ?? new List<string>();
                allComps      = JsonConvert.DeserializeObject<List<ComparableProperty>>(compsJson)
                                ?? new List<ComparableProperty>();
            }
            catch
            {
                SetMessage("Could not read comp selection. Please try again.", "danger");
                return;
            }

            if (selectedClips.Count == 0)
            {
                SetMessage("No comps selected.", "danger");
                return;
            }

            Guid organizationId = CurrentOrganizationId;
            Guid userId         = CurrentUserId;

            decimal subjectLat, subjectLng, radiusMiles;
            decimal.TryParse(hdnSubjectLat.Value,  out subjectLat);
            decimal.TryParse(hdnSubjectLng.Value,  out subjectLng);
            decimal.TryParse(hdnRadiusMiles.Value, out radiusMiles);

            try
            {
                var service = new ProspectService();
                var result  = service.SaveSelectedComps(
                    prospectId,
                    hdnSubjectClip.Value,
                    subjectLat,
                    subjectLng,
                    hdnSubjectAddr.Value,
                    radiusMiles,
                    selectedClips,
                    allComps,
                    organizationId,
                    userId);

                if (!result.Success)
                {
                    SetMessage(result.ErrorMessage ?? "Failed to save comps.", "danger");
                    return;
                }

                // Redirect to property detail page
                Response.Redirect(
                    string.Format("~/Secure/Prospects/PropertyDetail.aspx?clip={0}&prospectId={1}&saved={2}",
                        hdnSubjectClip.Value, prospectId, result.CompsInserted),
                    endResponse: true);
            }
            catch (Exception ex)
            {
                SetMessage("Error saving comps: " + ex.Message, "danger");
                System.Diagnostics.Trace.TraceError("[Comps] HandleSaveComps error: {0}", ex);
            }
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private void SetMessage(string message, string type)
        {
            hdnMessage.Value     = message;
            hdnMessageType.Value = type;
        }
    }
}
