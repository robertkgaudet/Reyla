using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;

namespace Reyla.UserControls
{
    public partial class MasterPageSidenavMenu : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            bool isAuth = HttpContext.Current.User?.Identity?.IsAuthenticated == true;

            // Hide entire nav bar for anonymous users
            pnlNav.Visible = isAuth;

            if (!IsPostBack)
            {
                string displayName = "Guest";

                if (isAuth)
                {
                    // Prefer the aggregated profile exposed by the master base class
                    var master = this.Page?.Master as Reyla.ReylaMasterBase;
                    if (master?.CurrentUserProfile != null)
                    {
                        var profile = master.CurrentUserProfile;
                        // Use FullName if present, otherwise fall back to username
                        displayName = !string.IsNullOrWhiteSpace(profile.FullName)
                            ? profile.FullName
                            : (!string.IsNullOrWhiteSpace(profile.UserName) ? profile.UserName : "User");
                    }
                    else
                    {
                        // Fallback: get the membership username
                        try
                        {
                            var mem = Membership.GetUser(Page.User.Identity.Name);
                            if (mem != null && !string.IsNullOrWhiteSpace(mem.UserName))
                                displayName = mem.UserName;
                        }
                        catch
                        {
                            // ignore and use Guest
                        }
                    }
                }

                lblFullName.Text       = HttpUtility.HtmlEncode(displayName);
                lblFullNameMobile.Text = HttpUtility.HtmlEncode(displayName);
            }
        }
    }
}
