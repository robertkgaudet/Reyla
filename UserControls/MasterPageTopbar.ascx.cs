using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using Reyla.Services;

namespace Reyla.UserControls
{
    public partial class MasterPageTopbar : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            bool isAuth = HttpContext.Current.User?.Identity?.IsAuthenticated == true;

            // Show/hide individual elements based on auth state
            divSearch.Visible    = isAuth;
            divNotif.Visible     = isAuth;
            divSettings.Visible  = isAuth;
            divDarkMode.Visible  = isAuth;
            divUserMenu.Visible  = isAuth;
            divAnonLinks.Visible = !isAuth;

            if (!IsPostBack && isAuth)
                BindNotifications();
        }

        // ── Notifications ─────────────────────────────────────────────────

        private void BindNotifications()
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
                }

                var svc     = new ActivityService();
                var entries = svc.GetUserActivity(userId, limit: 30);

                litNotifBadge.Text = entries.Count.ToString();
                litNotifCount.Text = entries.Count + " Alert" + (entries.Count != 1 ? "s" : "");
                litNotifItems.Text = BuildNotifHtml(entries);
            }
            catch
            {
                // Non-fatal — leave defaults
            }
        }

        private string BuildNotifHtml(List<ActivityLogEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return "<div class=\"dropdown-item text-muted py-3 text-center\">No recent activity.</div>";

            var sb  = new StringBuilder();
            int idx = 0;

            foreach (var entry in entries)
            {
                idx++;
                string itemId  = "notif-" + idx;
                string icon    = GetNotifIcon(entry.ActivityType);
                string summary = HttpUtility.HtmlEncode(entry.Summary ?? entry.ActivityType);
                string timeAgo = GetTimeAgo(entry.CreatedAtUtc);

                var links = new StringBuilder();
                if (!string.IsNullOrEmpty(entry.Clip))
                {
                    bool isContact = entry.ActivityType == ActivityType.NoteAdded         ||
                                     entry.ActivityType == ActivityType.NoteEdited        ||
                                     entry.ActivityType == ActivityType.NoteDeleted       ||
                                     entry.ActivityType == ActivityType.ProspectContacted ||
                                     entry.ActivityType == ActivityType.ContactInfoUpdated;

                    string tab = isContact ? "contact" : "overview";
                    links.AppendFormat(
                        " <a href=\"/Secure/Prospects/PropertyDetail.aspx?clip={0}&tab={1}\" " +
                        "class=\"text-primary fs-xs text-decoration-none\">" +
                        "<i class=\"ti ti-building-estate me-1\"></i>View Property</a>",
                        HttpUtility.HtmlAttributeEncode(entry.Clip), tab);
                }

                if (!string.IsNullOrEmpty(entry.ContactId))
                {
                    links.AppendFormat(
                        " <a href=\"/Secure/Contacts/Contact.aspx?contactId={0}\" " +
                        "class=\"text-primary fs-xs text-decoration-none ms-2\">" +
                        "<i class=\"ti ti-user me-1\"></i>View Contact</a>",
                        HttpUtility.HtmlAttributeEncode(entry.ContactId));
                }

                if (!string.IsNullOrEmpty(entry.LinkedProspectId))
                {
                    links.AppendFormat(
                        " <a href=\"/Secure/Prospects/Comps.aspx?prospectId={0}\" " +
                        "class=\"text-primary fs-xs text-decoration-none ms-2\">" +
                        "<i class=\"ti ti-search me-1\"></i>Comps</a>",
                        HttpUtility.HtmlAttributeEncode(entry.LinkedProspectId));
                }

                sb.AppendFormat(@"
                    <div class=""dropdown-item notification-item py-2 text-wrap"" id=""{0}"">
                        <span class=""d-flex gap-2"">
                            <span class=""avatar-md flex-shrink-0"">
                                <span class=""avatar-title bg-primary-subtle text-primary rounded-circle fs-22"">
                                    <i class=""{1} fs-xl""></i>
                                </span>
                            </span>
                            <span class=""flex-grow-1 text-muted"">
                                <span class=""fw-medium text-body"">{2}</span>
                                {3}
                                <br>
                                <span class=""fs-xs"">{4}</span>
                            </span>
                            <button type=""button"" class=""flex-shrink-0 text-muted btn btn-link p-0"" data-dismissible=""#{0}"">
                                <i data-lucide=""circle-x"" class=""fs-xxl""></i>
                            </button>
                        </span>
                    </div>",
                    itemId, icon, summary,
                    links.Length > 0 ? "<br>" + links.ToString() : "",
                    HttpUtility.HtmlEncode(timeAgo));
            }

            return sb.ToString();
        }

        private string GetNotifIcon(string activityType)
        {
            switch (activityType)
            {
                case ActivityType.ProspectCreated:    return "ti ti-building-estate";
                case ActivityType.ProspectContacted:  return "ti ti-phone-check";
                case ActivityType.ProspectArchived:   return "ti ti-archive";
                case ActivityType.CompAdded:          return "ti ti-circle-plus";
                case ActivityType.CompRemoved:        return "ti ti-circle-minus";
                case ActivityType.CompsCleared:       return "ti ti-trash";
                case ActivityType.NoteAdded:          return "ti ti-notes";
                case ActivityType.NoteEdited:         return "ti ti-pencil";
                case ActivityType.NoteDeleted:        return "ti ti-notes-off";
                case ActivityType.ContactInfoUpdated: return "ti ti-address-book";
                case ActivityType.ContactCreated:     return "ti ti-user-plus";
                case ActivityType.ContactUpdated:     return "ti ti-user-edit";
                case ActivityType.ContactDeleted:     return "ti ti-user-off";
                case ActivityType.ContactLinkedToProperty:     return "ti ti-link";
                case ActivityType.ContactUnlinkedFromProperty: return "ti ti-unlink";
                case ActivityType.DealCreated:        return "ti ti-briefcase";
                case ActivityType.DealStageChanged:   return "ti ti-arrows-right-left";
                case ActivityType.DealClosed:         return "ti ti-trophy";
                case ActivityType.ValuationViewed:    return "ti ti-calculator";
                case ActivityType.ValuationSaved:     return "ti ti-device-floppy";
                default:                              return "ti ti-activity";
            }
        }

        private string GetTimeAgo(DateTime utc)
        {
            var diff = DateTime.UtcNow - utc;
            if (diff.TotalMinutes < 1)  return "Just now";
            if (diff.TotalMinutes < 60) return (int)diff.TotalMinutes + " min ago";
            if (diff.TotalHours   < 24) return (int)diff.TotalHours   + " hr ago";
            if (diff.TotalDays    < 7)  return (int)diff.TotalDays + " day" + ((int)diff.TotalDays != 1 ? "s" : "") + " ago";
            return utc.ToLocalTime().ToString("MMM d");
        }

        protected void lnkLogout_ServerClick(object sender, EventArgs e)
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            Response.Redirect("~/Signin.aspx");
        }
    }
}
