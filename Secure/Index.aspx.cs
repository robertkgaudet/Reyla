using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Reyla.Services;

namespace Reyla.Secure
{
    public partial class Index : System.Web.UI.Page
    {
        protected string ProspectsJson = "[]";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                BindProspectMap();
        }

        private void BindProspectMap()
        {
            try
            {
                var memberUser = Membership.GetUser();
                if (memberUser == null) return;

                var userId = (Guid)memberUser.ProviderUserKey;
                Guid orgId = Guid.Empty;

                using (var db = new DCReyla())
                {
                    var profile = db.Profiles.FirstOrDefault(p => p.UserId == userId);
                    if (profile?.OrganizationId != null)
                        orgId = profile.OrganizationId.Value;

                    // Load all active prospects for this user with their property coords
                    var prospects = (
                        from pr in db.Prospects
                        join prop in db.Properties on pr.PropertyId equals prop.PropertyId
                        where pr.UserId       == userId
                           && pr.OrganizationId == orgId
                           && pr.IsDeleted    == false
                           && prop.Latitude   != 0
                           && prop.Longitude  != 0
                        select new
                        {
                            pr.ProspectId,
                            pr.PropertyId,
                            prop.Clip,
                            prop.StreetAddress,
                            prop.CityNameRaw,
                            prop.ZipCodeRaw,
                            prop.Latitude,
                            prop.Longitude,
                            prop.PropertyUseCode,
                            pr.SelectedCompsJson,
                            pr.ContactedDate,
                            pr.CreatedAtUtc
                        }).ToList();

                    var dtos = new List<object>();
                    foreach (var p in prospects)
                    {
                        // Comp count
                        int compCount = 0;
                        try
                        {
                            if (!string.IsNullOrEmpty(p.SelectedCompsJson))
                            {
                                var comps = JsonConvert.DeserializeObject<List<object>>(p.SelectedCompsJson);
                                compCount = comps?.Count ?? 0;
                            }
                        }
                        catch { }

                        // Latest snapshot for sqft + year built
                        int?    sqft      = null;
                        int?    yearBuilt = null;
                        decimal? avm      = null;

                        var snap = db.PropertySnapshots
                            .Where(s => s.PropertyId == p.PropertyId && !s.IsDeleted)
                            .OrderByDescending(s => s.CreatedAtUtc)
                            .FirstOrDefault();

                        if (snap != null)
                        {
                            var bldg = db.PropertySnapshotBuildings
                                .FirstOrDefault(b => b.PropertySnapshotId == snap.PropertySnapshotId);
                            if (bldg != null)
                            {
                                sqft      = bldg.GrossLivingArea.HasValue ? (int?)bldg.GrossLivingArea.Value : null;
                                yearBuilt = bldg.YearBuilt;
                            }

                            var avmRow = db.PropertySnapshotAvms
                                .FirstOrDefault(a => a.PropertySnapshotId == snap.PropertySnapshotId);
                            if (avmRow != null)
                                avm = avmRow.EstimatedValue;
                        }

                        // Street view thumbnail
                        string streetViewUrl = string.Format(
                            "https://maps.googleapis.com/maps/api/streetview?size=300x200&location={0},{1}&key=YOUR_GOOGLE_KEY",
                            p.Latitude, p.Longitude);

                        string address = (p.StreetAddress ?? "").ToUpper();
                        string cityState = string.Join(", ",
                            new[] { p.CityNameRaw, p.ZipCodeRaw }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));

                        dtos.Add(new
                        {
                            prospectId   = p.ProspectId.ToString(),
                            clip         = p.Clip,
                            address      = address,
                            cityState    = cityState,
                            lat          = (double)p.Latitude,
                            lng          = (double)p.Longitude,
                            propertyUse  = p.PropertyUseCode ?? "",
                            compCount    = compCount,
                            sqft         = sqft,
                            yearBuilt    = yearBuilt,
                            avm          = avm.HasValue ? (long?)((long)avm.Value) : null,
                            contacted    = p.ContactedDate.HasValue,
                            addedDate    = p.CreatedAtUtc.ToLocalTime().ToString("MMM d, yyyy"),
                            streetViewUrl = streetViewUrl,
                            detailUrl    = "/Secure/Prospects/PropertyDetail.aspx?clip=" + HttpUtility.UrlEncode(p.Clip),
                            compsUrl     = "/Secure/Prospects/Comps.aspx?prospectId=" + p.ProspectId.ToString()
                        });
                    }

                    ProspectsJson = JsonConvert.SerializeObject(dtos);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[Index.BindProspectMap] {0}", ex.Message);
                ProspectsJson = "[]";
            }
        }
    }
}
