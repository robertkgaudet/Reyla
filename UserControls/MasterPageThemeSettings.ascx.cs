using System;
using System.Web;

namespace Reyla.UserControls
{
    public partial class MasterPageThemeSettings : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Hide the settings offcanvas for anonymous users —
            // the trigger button in the topbar is already hidden,
            // but we suppress the panel too so it isn't in the DOM at all.
            pnlThemeSettings.Visible = HttpContext.Current.User?.Identity?.IsAuthenticated == true;
        }
    }
}
