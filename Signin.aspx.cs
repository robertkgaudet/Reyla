using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Script.Serialization;

namespace Reyla
{
	public partial class Signin : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			// If already authenticated, only redirect to Secure when user is authorized for it.
			if (User.Identity.IsAuthenticated)
			{
				if (Roles.Enabled)
				{
					bool isAllowed = Roles.IsUserInRole(User.Identity.Name, "Realtor") ||
									 Roles.IsUserInRole(User.Identity.Name, "Administrator");

					if (isAllowed)
					{
						Response.Redirect("~/Secure/Index.aspx");
					}
					else
					{
						Response.Redirect("~/AccessDenied.aspx");
					}
				}
				else
				{
					// Role manager is not available — don't assume access.
					Response.Redirect("~/AccessDenied.aspx");
				}
			}

			// Pre-fill username if provided
			string usernameFromQueryString = Request.QueryString["username"];
			if (!string.IsNullOrEmpty(usernameFromQueryString))
			{
				txtUsername.Text = usernameFromQueryString;
			}
		}

		protected void btnLogin_Click(object sender, EventArgs e)
		{
			string username = txtUsername.Text.Trim();
			string password = txtPassword.Text;

			try
			{
				if (Membership.ValidateUser(username, password))
				{
					// Verify role membership before issuing auth cookie / redirect to protected area.
					if (!Roles.Enabled)
					{
						ShowSweetAlert("Access denied", "Role service not available. Contact support.", "error");
						ValidationSummary1.HeaderText = "Role service not available. Contact support.";
						return;
					}

					bool isAllowed = Roles.IsUserInRole(username, "Realtor") ||
									 Roles.IsUserInRole(username, "Administrator");

					if (!isAllowed)
					{
						ShowSweetAlert("Access denied", "Your account does not have access to that area.", "error");
						ValidationSummary1.HeaderText = "Your account does not have access to that area.";
						return;
					}

					// Issue the auth ticket and cookie
					FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
						1,                      // version
						username,               // name
						DateTime.Now,           // issueDate
						DateTime.Now.AddDays(30), // expiration
						true,                   // isPersistent
						string.Empty            // userData
					);

					string encryptedTicket = FormsAuthentication.Encrypt(ticket);
					HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
					{
						Expires = ticket.Expiration
					};

					Response.Cookies.Add(authCookie);

					// Respect ReturnUrl if local
					string returnUrl = Request.QueryString["ReturnUrl"];
					if (!string.IsNullOrEmpty(returnUrl) && IsLocalUrl(returnUrl))
					{
						Response.Redirect(returnUrl);
					}

					Response.Redirect("~/Secure/Index.aspx");
				}
				else
				{
					ShowSweetAlert("Sign-in failed", "Invalid username or password.", "error");
					ValidationSummary1.HeaderText = "Invalid username or password.";
				}
			}
			catch (Exception ex)
			{
				// TODO: server-side logging for ex
				ShowSweetAlert("Error", "An error occurred: " + ex.Message, "error");
				ValidationSummary1.HeaderText = "An error occurred: " + ex.Message;
			}
		}

		// Helper to emit a SweetAlert2 popup on the client
		private void ShowSweetAlert(string title, string message, string icon = "error")
		{
			var payload = new { icon = icon, title = title, text = message };
			string json = new JavaScriptSerializer().Serialize(payload);
			string script = $"Swal.fire({json});";
			ClientScript.RegisterStartupScript(this.GetType(), "swal_alert", script, true);
		}

		// Local URL check similar to Url.IsLocalUrl used in MVC
		private bool IsLocalUrl(string url)
		{
			if (string.IsNullOrEmpty(url))
				return false;

			// allow absolute application-relative (~) and app-relative (/)
			if (url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\')))
				return true;

			if (url.Length > 1 && url[0] == '~' && url[1] == '/')
				return true;

			return false;
		}
	}
}