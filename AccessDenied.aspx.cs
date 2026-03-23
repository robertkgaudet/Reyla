using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;

namespace Reyla
{
	public partial class AccessDenied : Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			// Intentionally simple - can be extended to log incidents or show support contact info.
		}

		protected void btnSignOut_Click(object sender, EventArgs e)
		{
			try
			{
				// Clear forms auth and abandon session
				FormsAuthentication.SignOut();

				// Remove auth cookie explicitly
				var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, string.Empty)
				{
					Expires = DateTime.UtcNow.AddDays(-1),
					Path = FormsAuthentication.FormsCookiePath
				};
				Response.Cookies.Add(authCookie);

				// Remove role cookie if present (match your role cookie name)
				var roleCookie = new HttpCookie(".REYLA_ASPROLES", string.Empty)
				{
					Expires = DateTime.UtcNow.AddDays(-1),
					Path = "/"
				};
				Response.Cookies.Add(roleCookie);

				// Clear session
				Session.Clear();
				Session.Abandon();

				// Redirect to sign-in page
				Response.Redirect("/SignIn.aspx", false);
				Context.ApplicationInstance.CompleteRequest();
			}
			catch (Exception)
			{
				// Keep the page simple; optionally log the exception server-side
				Response.Redirect("/SignIn.aspx");
			}
		}
	}
}