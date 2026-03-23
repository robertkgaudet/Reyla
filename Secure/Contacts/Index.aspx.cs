using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using Reyla.Services;
using Newtonsoft.Json;

namespace Reyla.Secure.Contacts
{
    public partial class Index : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                BindPage();
        }

        private void BindPage()
        {
            try
            {
                // Contact types always load — lookup table, not org-specific
                var svc   = new ContactService();
                var types = svc.GetContactTypes();
                hdnContactTypesJson.Value = JsonConvert.SerializeObject(types);

                var memberUser = Membership.GetUser(User.Identity.Name);
                if (memberUser == null)
                {
                    hdnContactsJson.Value = "[]";
                    hdnCanSeeAll.Value    = "false";
                    badgeCount.InnerText  = "0 contacts";
                    return;
                }

                var userId = (Guid)memberUser.ProviderUserKey;
                Guid orgId;

                // OrgId from Profile.OrganizationId — matches CompToggle/DealToggle
                using (var db = new DCReyla())
                {
                    var profile = db.Profiles.FirstOrDefault(p => p.UserId == userId);
                    if (profile == null || !profile.OrganizationId.HasValue)
                    {
                        hdnContactsJson.Value = "[]";
                        hdnCanSeeAll.Value    = "false";
                        badgeCount.InnerText  = "0 contacts";
                        return;
                    }
                    orgId = profile.OrganizationId.Value;
                }

                var username  = User.Identity.Name;
                var contacts  = svc.GetContactsForOrg(orgId, userId, username);
                var canSeeAll = ContactService.CanSeeAllContacts(username);

                hdnContactsJson.Value = JsonConvert.SerializeObject(contacts);
                hdnCanSeeAll.Value    = canSeeAll.ToString().ToLower();
                badgeCount.InnerText  = contacts.Count + " contact" + (contacts.Count != 1 ? "s" : "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[Contacts.Index.BindPage] {0}", ex);
                hdnContactsJson.Value     = "[]";
                hdnContactTypesJson.Value = "[]";
                hdnCanSeeAll.Value        = "false";
                badgeCount.InnerText      = "0 contacts";
            }
        }
    }
}
