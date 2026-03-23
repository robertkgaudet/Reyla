using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Reyla
{
	public partial class Register : System.Web.UI.Page
	{
		public bool registrationComplete = false;
		Guid userId = Guid.Empty;

		protected void Page_Load(object sender, EventArgs e)
		{
			// If already signed in, redirect to Secure
			if (User.Identity.IsAuthenticated)
			{
				Response.Redirect("~/Secure/Index.aspx");
			}
		}

		/// <summary>
		/// Server-side validation for the Terms of Service checkbox.
		/// Required by CoreLogic MSA Licensing Addendum Section 3 —
		/// Permitted Users must agree in writing that their use of the
		/// Services complies with the Permitted Applications, and the
		/// agreement must name CoreLogic Solutions, LLC as an express
		/// third-party beneficiary.
		/// </summary>
		protected void cvTerms_ServerValidate(object source, ServerValidateEventArgs args)
		{
			args.IsValid = chkAcceptTerms.Checked;
		}

		protected void btnRegister_Click(object sender, EventArgs e)
		{
			if (!Page.IsValid) return;

			string username  = txtUsername.Text.Trim();
			string email     = txtEmail.Text.Trim();
			string password  = txtPassword.Text;
			string firstname = txtFirstname.Text;
			string phone     = txtPhone.Text;

			try
			{
				MembershipCreateStatus status;
				Membership.CreateUser(username, password, email, null, null, true, out status);
				if (status != MembershipCreateStatus.Success)
				{
					ValidationSummary1.HeaderText = "Registration failed: " + status.ToString();
					return;
				}

				// Retrieve the newly created user and extract the ProviderUserKey (GUID)
				var membershipUser = Membership.GetUser(username);
				if (membershipUser != null && membershipUser.ProviderUserKey != null)
				{
					userId = (Guid)membershipUser.ProviderUserKey;
				}

				// Persist profile information
				if (userId != Guid.Empty)
				{
					using (var db = new DCReyla())
					{
						var profile = new Profile
						{
							FirstName             = firstname,
							RegistrationComplete  = true,
							UserId                = userId,
							UpdatedAtUtc          = DateTime.UtcNow,
							CreatedAtUtc          = DateTime.UtcNow
						};
						db.Profiles.InsertOnSubmit(profile);
						db.SubmitChanges();
					}
				}

				// Ensure the user is assigned to the Realtor role before signing in
				bool roleAssigned   = false;
				const string realtorRole = "Realtor";
				try
				{
					if (Roles.Enabled)
					{
						if (!Roles.RoleExists(realtorRole))
							Roles.CreateRole(realtorRole);

						if (!Roles.IsUserInRole(username, realtorRole))
							Roles.AddUserToRole(username, realtorRole);

						roleAssigned = Roles.IsUserInRole(username, realtorRole);
					}
					else
					{
						ValidationSummary1.HeaderText = "Registration succeeded but role service is not available. Contact support.";
						return;
					}
				}
				catch (Exception roleEx)
				{
					System.Diagnostics.Trace.TraceError(roleEx.ToString());
					ValidationSummary1.HeaderText = "Registration completed but role assignment failed. Contact support.";
					return;
				}

				if (!roleAssigned)
				{
					ValidationSummary1.HeaderText = "Registration succeeded but the new account could not be placed into the required role. Contact support.";
					return;
				}

				// Role assignment succeeded — sign in and redirect
				FormsAuthentication.SetAuthCookie(username, false);
				Response.Redirect("~/Secure/Index.aspx");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Trace.TraceError(ex.ToString());
				ValidationSummary1.HeaderText = "An error occurred: " + ex.Message;
			}
		}
	}
}
