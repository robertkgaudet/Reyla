using System;
using System.Configuration;
using System.Linq;
using System.Web.Security;
using Newtonsoft.Json;
using Reyla.Services;

namespace Reyla.Secure.Prospects
{
    public partial class ProspectMap : System.Web.UI.Page
    {
        // Exposed to ASPX as JS variables
        protected bool   HasRegridToken  { get; private set; }
        protected string StreetViewKey   { get; private set; }
        protected string ProspectsJson   { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                LoadPage();
        }

        private void LoadPage()
        {
            var regridToken = ConfigurationManager.AppSettings["Regrid.ApiToken"] ?? string.Empty;
            HasRegridToken = !string.IsNullOrWhiteSpace(regridToken);
            StreetViewKey  = ConfigurationManager.AppSettings["Google.StreetViewApiKey"] ?? string.Empty;

            // Load existing Reyla prospects to show as reference markers on the map
            var orgId  = GetCurrentOrganizationId();
            var userId = GetCurrentUserId();

            if (orgId == Guid.Empty)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            ProspectsJson = BuildProspectsJson(orgId);
        }

        private string BuildProspectsJson(Guid orgId)
        {
            try
            {
                using (var db = new DCReyla())
                {
                    // Join prospects → properties to get coordinates
                    var rows = (from p in db.Prospects
                                join pr in db.Properties on p.PropertyId equals pr.PropertyId
                                where p.OrganizationId == orgId
                                   && p.IsDeleted      == false
                                   && p.IsActive       == true
                                select new
                                {
                                    address = pr.StreetAddress ?? string.Empty,
                                    lat     = pr.Latitude,
                                    lng     = pr.Longitude
                                })
                               .Where(r => r.lat != 0 || r.lng != 0)
                               .Take(200)   // safety limit for map performance
                               .ToList();

                    return JsonConvert.SerializeObject(rows);
                }
            }
            catch
            {
                return "[]";
            }
        }

        private Guid GetCurrentUserId()
        {
            var user = Membership.GetUser();
            return user != null ? (Guid)user.ProviderUserKey : Guid.Empty;
        }

        private Guid GetCurrentOrganizationId()
        {
            using (var db = new DCReyla())
            {
                var uid     = GetCurrentUserId();
                var profile = db.Profiles.FirstOrDefault(p => p.UserId == uid);
                return profile?.OrganizationId ?? Guid.Empty;
            }
        }
    }
}
