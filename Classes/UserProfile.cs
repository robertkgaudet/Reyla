using System;
using System.Linq;
using System.Reflection;
using System.Web.Security;

namespace Reyla
{
	/// <summary>
	/// Aggregated user profile combining profile table, membership and roles.
	/// </summary>
	public class UserProfile
	{
		// Membership / user fields
		public Guid? UserId { get; private set; }
		public string UserName { get; private set; }
		public string Email { get; private set; }
		public bool IsApproved { get; private set; }
		public bool IsLockedOut { get; private set; }
		public DateTime? LastLoginDate { get; private set; }
		public DateTime? CreationDate { get; private set; }
		public DateTime? LastPasswordChangedDate { get; private set; }

		// Role names (renamed to avoid collision with System.Web.Security.Roles)
		public string[] RoleNames { get; private set; } = new string[0];

		// Profile table fields
		public string FirstName { get; private set; }
		public string LastName { get; private set; }
		public string FullName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
									? UserName
									: $"{FirstName} {LastName}".Trim();
		public string PhoneNumber { get; private set; }
		public string TimeZone { get; private set; }
		public bool RegistrationComplete { get; private set; }
		public DateTime? ProfileCreatedAtUtc { get; private set; }
		public DateTime? ProfileUpdatedAtUtc { get; private set; }

		public static UserProfile Load(string userName)
		{
			if (string.IsNullOrWhiteSpace(userName)) return null;

			var result = new UserProfile { UserName = userName };

			// Membership info
			var memUser = Membership.GetUser(userName);
			if (memUser != null)
			{
				result.Email = memUser.Email;
				result.IsApproved = memUser.IsApproved;
				result.IsLockedOut = memUser.IsLockedOut;
				result.LastLoginDate = memUser.LastLoginDate == DateTime.MinValue ? (DateTime?)null : memUser.LastLoginDate;
				result.CreationDate = memUser.CreationDate == DateTime.MinValue ? (DateTime?)null : memUser.CreationDate;
				result.LastPasswordChangedDate = memUser.LastPasswordChangedDate == DateTime.MinValue ? (DateTime?)null : memUser.LastPasswordChangedDate;

				// ProviderUserKey typically a GUID for SqlMembershipProvider
				if (memUser.ProviderUserKey != null)
				{
					if (memUser.ProviderUserKey is Guid g) result.UserId = g;
					else if (Guid.TryParse(memUser.ProviderUserKey.ToString(), out var id)) result.UserId = id;
				}
			}

			// Roles - fully qualify System.Web.Security.Roles to avoid name collision
			try
			{
				if (System.Web.Security.Roles.Enabled)
				{
					result.RoleNames = System.Web.Security.Roles.GetRolesForUser(userName) ?? new string[0];
				}
			}
			catch
			{
				result.RoleNames = new string[0];
			}

			// Profile table (DCReyla.Profiles assumed). Defensive mapping.
			try
			{
				if (result.UserId.HasValue)
				{
					using (var db = new DCReyla())
					{
						var profile = db.Profiles.FirstOrDefault(p => p.UserId == result.UserId.Value);
						if (profile != null)
						{
							TryMap(profile, "FirstName", v => result.FirstName = v as string);
							TryMap(profile, "LastName", v => result.LastName = v as string);
							TryMap(profile, "PhoneNumber", v => result.PhoneNumber = v as string);
							TryMap(profile, "TimeZone", v => result.TimeZone = v as string);

							TryMap(profile, "RegistrationComplete", v =>
							{
								if (v != null && bool.TryParse(v.ToString(), out bool b)) result.RegistrationComplete = b;
							});
							TryMap(profile, "CreatedAtUtc", v => { if (v != null && DateTime.TryParse(v.ToString(), out DateTime dt)) result.ProfileCreatedAtUtc = dt; });
							TryMap(profile, "UpdatedAtUtc", v => { if (v != null && DateTime.TryParse(v.ToString(), out DateTime dt)) result.ProfileUpdatedAtUtc = dt; });
						}
					}
				}
			}
			catch
			{
				// ignore DB mapping errors
			}

			return result;
		}

		private static void TryMap(object source, string propName, Action<object> setter)
		{
			if (source == null || setter == null || string.IsNullOrEmpty(propName)) return;
			try
			{
				var pi = source.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
				if (pi != null)
				{
					var val = pi.GetValue(source, null);
					setter(val);
				}
			}
			catch { }
		}
	}
}