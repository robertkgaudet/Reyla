using System;
using System.Web.UI;

namespace Reyla
{
	/// <summary>
	/// Base class for site master pages. Loads CurrentUserProfile on Init.
	/// </summary>
	public abstract class ReylaMasterBase : MasterPage
	{
		private UserProfile _currentUserProfile;
		private bool _profileLoaded;

		/// <summary>
		/// Current aggregated profile for the signed-in user; null when anonymous.
		/// </summary>
		public UserProfile CurrentUserProfile
		{
			get
			{
				if (!_profileLoaded)
				{
					_profileLoaded = true;
					LoadCurrentUserProfile();
				}
				return _currentUserProfile;
			}
			protected set => _currentUserProfile = value;
		}

		/// <summary>
		/// True if a user is authenticated for this request.
		/// </summary>
		public bool IsUserAuthenticated => Page?.User?.Identity?.IsAuthenticated ?? false;

		/// <summary>
		/// Convenience: username of the current user or null.
		/// </summary>
		public string CurrentUserName => Page?.User?.Identity?.Name;

		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);

			if (IsUserAuthenticated)
			{
				_currentUserProfile = UserProfile.Load(CurrentUserName);
				_profileLoaded = true;
			}
		}

		private void LoadCurrentUserProfile()
		{
			if (!IsUserAuthenticated)
			{
				_currentUserProfile = null;
				return;
			}
			_currentUserProfile = UserProfile.Load(CurrentUserName);
		}

		/// <summary>
		/// Helper that answers whether the current user is in the given role.
		/// </summary>
		public bool CurrentUserIsInRole(string role)
		{
			if (string.IsNullOrEmpty(role)) return false;
			if (CurrentUserProfile?.RoleNames == null) return false;
			foreach (var r in CurrentUserProfile.RoleNames)
			{
				if (string.Equals(r, role, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}
	}
}