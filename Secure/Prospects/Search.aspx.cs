using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Newtonsoft.Json;
using Reyla.Services;
using Reyla.Services.Models;

namespace Reyla.Secure.Prospects
{
    public partial class Search : Page
    {
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
            if (!IsPostBack)
            {
                hdnStreetViewKey.Value =
                    System.Configuration.ConfigurationManager.AppSettings["Google.StreetViewApiKey"] ?? string.Empty;

                // Support deep-link from Parcel Map: ?addr=&city=&state=&zip=&autoSearch=1
                // Each field is passed separately — no comma-splitting needed.
                // Legacy ?q= single-string fallback retained for any existing links.
                var addr       = (Request.QueryString["addr"]       ?? string.Empty).Trim();
                var city       = (Request.QueryString["city"]       ?? string.Empty).Trim();
                var state      = (Request.QueryString["state"]      ?? string.Empty).Trim();

                // Strip ZIP+4 suffix (e.g. "75227-1612" → "75227") — CoreLogic expects 5-digit ZIP only
                var zip = (Request.QueryString["zip"] ?? string.Empty).Trim();
                var zipDash = zip.IndexOf('-');
                if (zipDash > 0) zip = zip.Substring(0, zipDash);

                // Legacy ?q= fallback — best-effort parse of "123 Main St, Dallas, TX 75201"
                if (string.IsNullOrEmpty(addr))
                {
                    var q     = (Request.QueryString["q"] ?? string.Empty).Trim();
                    if (!string.IsNullOrEmpty(q))
                    {
                        var parts     = q.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        addr          = parts.Length > 0 ? parts[0].Trim() : q;
                        city          = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                        var stateZip  = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                        var szParts   = stateZip.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        state         = szParts.Length > 0 ? szParts[0] : string.Empty;
                        zip           = szParts.Length > 1 ? szParts[1] : string.Empty;
                    }
                }

                if (!string.IsNullOrEmpty(addr))
                {
                    var jsAddr  = Newtonsoft.Json.JsonConvert.SerializeObject(addr);
                    var jsCity  = Newtonsoft.Json.JsonConvert.SerializeObject(city);
                    var jsState = Newtonsoft.Json.JsonConvert.SerializeObject(state.ToUpper());
                    var jsZip   = Newtonsoft.Json.JsonConvert.SerializeObject(zip);

                    // Build a self-submitting hidden form — no dependency on __doPostBack,
                    // ScriptManager, or MicrosoftAjax. Uses UniqueID (the form field name)
                    // rather than ClientID so ASP.NET recognises the posted values.
                    var script = string.Format(
                        @"(function(){{
                            var f = document.createElement('form');
                            f.method = 'POST';
                            f.action = '/Secure/Prospects/Search';
                            function h(n,v){{ var i=document.createElement('input'); i.type='hidden'; i.name=n; i.value=v; f.appendChild(i); }}
                            h('__EVENTTARGET',   'SearchProperty');
                            h('__EVENTARGUMENT', '');
                            h('{0}', {1});
                            h('{2}', {3});
                            h('{4}', {5});
                            h('{6}', {7});
                            var svk = document.getElementById('{8}');
                            h('{9}', svk ? svk.value : '');
                            document.body.appendChild(f);
                            f.submit();
                        }})();",
                        txtStreetAddress.UniqueID, jsAddr,
                        txtCity.UniqueID,          jsCity,
                        txtState.UniqueID,         jsState,
                        txtZip.UniqueID,           jsZip,
                        hdnStreetViewKey.ClientID,
                        hdnStreetViewKey.UniqueID);

                    ClientScript.RegisterStartupScript(GetType(), "autoSearch", script, addScriptTags: true);
                }

                return;
            }

            string eventTarget = Request.Form["__EVENTTARGET"] ?? string.Empty;

            switch (eventTarget)
            {
                case "SearchProperty":
                    HandleSearch();
                    break;

                case "CreateProspect":
                    HandleCreateProspect();
                    break;

                case "ViewDetail":
                    HandleViewDetail();
                    break;
            }
        }

        // ---------------------------------------------------------------
        // Search handler
        // Calls CoreLogic search + site-location, returns a list of matches.
        // Checks each Clip against the Prospect table so the UI knows
        // which results are already prospects for this org.
        // ---------------------------------------------------------------

        private void HandleSearch()
        {
            string streetAddress = txtStreetAddress.Text.Trim();
            string city          = txtCity.Text.Trim();
            string state         = txtState.Text.Trim().ToUpper();
            string zip           = txtZip.Text.Trim();

            if (string.IsNullOrEmpty(streetAddress))
            {
                SetMessage("Please enter a street address.", "danger");
                return;
            }

            Guid organizationId = CurrentOrganizationId;
            Guid userId         = CurrentUserId;

            if (organizationId == Guid.Empty || userId == Guid.Empty)
            {
                SetMessage("Session error — please log in again.", "danger");
                return;
            }

            try
            {
                var api           = new CoreLogicPropertyService();
                var geocodeResult = api.SearchByAddressWithGeocode(streetAddress, city, state, zip);

                if (geocodeResult?.Items == null || !geocodeResult.Items.Any())
                {
                    SetMessage("No properties found. Try adding a zip code or check the spelling.", "danger");
                    hdnResultsJson.Value = string.Empty;
                    return;
                }

                // Check which returned clips are already prospects for this org.
                // Build a Clip -> ProspectId map so the UI can link to Comps.aspx.
                var clips = geocodeResult.Items
                    .Where(i => !string.IsNullOrEmpty(i.Clip))
                    .Select(i => i.Clip)
                    .ToList();

                Dictionary<string, Guid> clipProspectMap;

                using (var db = new DCReyla())
                {
                    clipProspectMap = (
                        from prospect in db.Prospects
                        where prospect.OrganizationId == organizationId && prospect.IsDeleted == false
                        join property in db.Properties on prospect.PropertyId equals property.PropertyId
                        where clips.Contains(property.Clip)
                        select new { property.Clip, prospect.ProspectId }
                    ).ToDictionary(x => x.Clip, x => x.ProspectId);
                }

                var resultList = geocodeResult.Items.Select(item => new
                {
                    Clip          = item.Clip,
                    StreetAddress = item.PropertyAddress?.StreetAddress,
                    City          = item.PropertyAddress?.City,
                    State         = item.PropertyAddress?.State,
                    ZipCode       = item.PropertyAddress?.ZipCode,
                    Lat           = item.Geocode?.Latitude,
                    Lng           = item.Geocode?.Longitude,
                    IsProspect    = clipProspectMap.ContainsKey(item.Clip ?? string.Empty),
                    ProspectId    = clipProspectMap.ContainsKey(item.Clip ?? string.Empty)
                                        ? clipProspectMap[item.Clip].ToString()
                                        : (string)null
                }).ToList();

                hdnResultsJson.Value = JsonConvert.SerializeObject(resultList);

                // Detect address mismatch — if CoreLogic returned a different street
                // address than what the user searched, show an informational banner.
                bool mismatchFound = resultList.Any(r =>
                    !string.IsNullOrEmpty(r.StreetAddress) &&
                    !string.Equals(
                        r.StreetAddress.Trim(),
                        streetAddress.Trim(),
                        StringComparison.OrdinalIgnoreCase));

                if (mismatchFound)
                {
                    var returnedAddresses = resultList
                        .Where(r => !string.IsNullOrEmpty(r.StreetAddress))
                        .Select(r => r.StreetAddress.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    string addressList = string.Join(" and ", returnedAddresses);

                    SetMessage(
                        "No exact match found for \"" + streetAddress + "\". CoreLogic matched this to " + addressList + ", which may represent both addresses. Please verify the result below is the correct property.",
                        "info");
                }
                else
                {
                    SetMessage(string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                // Log the full technical error for diagnostics, but show a clean message to the user
                System.Diagnostics.Trace.TraceError("[Search] HandleSearch error: {0}", ex);
                SetMessage("No properties found matching that address. Please check the spelling or try a different address.", "danger");
                hdnResultsJson.Value = string.Empty;
            }
        }

        // ---------------------------------------------------------------
        // View Detail handler
        // Ensures the Property/snapshot row exists (upserts via GetProfile),
        // then redirects to PropertyDetail.aspx.  Does NOT create a Prospect.
        // ---------------------------------------------------------------

        private void HandleViewDetail()
        {
            string clip           = hdnDetailClip.Value?.Trim();
            Guid   organizationId = CurrentOrganizationId;
            Guid   userId         = CurrentUserId;

            if (string.IsNullOrEmpty(clip))
            {
                SetMessage("No property selected.", "danger");
                return;
            }

            try
            {
                // Upsert Property + snapshot so PropertyDetail.aspx has data to show
                string streetAddress = txtStreetAddress.Text.Trim();
                string city          = txtCity.Text.Trim();
                string state         = txtState.Text.Trim().ToUpper();
                string zip           = txtZip.Text.Trim();

                var snapshotService = new PropertySnapshotService();
                var profile = snapshotService.GetProfile(
                    streetAddress, city, state, zip, organizationId, userId);

                if (profile.PropertyNotFound || profile.Property == null)
                {
                    SetMessage("Could not retrieve property data. Please try again.", "danger");
                    return;
                }

                Response.Redirect(
                    string.Format("~/Secure/Prospects/PropertyDetail.aspx?clip={0}", clip),
                    endResponse: true);
            }
            catch (Exception ex)
            {
                SetMessage("Error loading property: " + ex.Message, "danger");
                System.Diagnostics.Trace.TraceError("[Search] HandleViewDetail error: {0}", ex);
            }
        }

        // ---------------------------------------------------------------
        // Create Prospect handler
        // Pulls the full property snapshot, creates the Prospect record,
        // then redirects to Comps.aspx for the user to review and save comps.
        // ---------------------------------------------------------------

        private void HandleCreateProspect()
        {
            string clip         = hdnClip.Value?.Trim();
            string address      = hdnAddress.Value?.Trim();
            Guid   organizationId = CurrentOrganizationId;
            Guid   userId         = CurrentUserId;

            if (string.IsNullOrEmpty(clip))
            {
                SetMessage("No property selected.", "danger");
                return;
            }

            if (organizationId == Guid.Empty || userId == Guid.Empty)
            {
                SetMessage("Session error — please log in again.", "danger");
                return;
            }

            try
            {
                // Step 1 — Pull full snapshot to upsert Property record
                string streetAddress = txtStreetAddress.Text.Trim();
                string city          = txtCity.Text.Trim();
                string state         = txtState.Text.Trim().ToUpper();
                string zip           = txtZip.Text.Trim();

                var snapshotService = new PropertySnapshotService();
                var profile = snapshotService.GetProfile(
                    streetAddress, city, state, zip, organizationId, userId);

                if (profile.PropertyNotFound || profile.Property == null)
                {
                    SetMessage("Could not retrieve property data. Please try again.", "danger");
                    return;
                }

                // Step 2 — Create Prospect record (no comps saved here)
                var prospectService = new ProspectService();
                var result = prospectService.CreateProspect(
                    profile.Property.PropertyId,
                    organizationId,
                    userId);

                if (!result.Success)
                {
                    SetMessage(result.ErrorMessage ?? "Failed to create prospect.", "danger");
                    return;
                }

                // Step 3 — Auto-create a Deal in Lead stage so it appears on the Pipeline board
                if (!result.AlreadyExisted)
                {
                    try
                    {
                        string dealTitle = (profile.Property.StreetAddress ?? "Unknown Address").ToUpper();
                        string propAddr  = (profile.Property.StreetAddress ?? "") +
                                          (!string.IsNullOrWhiteSpace(profile.Property.CityNameRaw)
                                              ? ", " + profile.Property.CityNameRaw : "");

                        new DealService().CreateDeal(
                            organizationId:  organizationId,
                            userId:          userId,
                            title:           dealTitle,
                            prospectId:      result.ProspectId,
                            propertyAddress: propAddr,
                            stage:           DealService.Stages.Lead);
                    }
                    catch { /* Non-fatal — prospect was created, deal creation is best-effort */ }
                }

                // Step 4 — Redirect to Comps review page
                Response.Redirect(
                    string.Format("~/Secure/Prospects/Comps.aspx?prospectId={0}", result.ProspectId),
                    endResponse: true);
            }
            catch (Exception ex)
            {
                SetMessage("Error creating prospect: " + ex.Message, "danger");
                System.Diagnostics.Trace.TraceError("[Search] HandleCreateProspect error: {0}", ex);
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
