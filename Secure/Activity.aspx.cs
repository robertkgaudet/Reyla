using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using Reyla.Services;

namespace Reyla.Secure
{
    public partial class ActivityFeed : System.Web.UI.Page
    {
        // ── Auth ──────────────────────────────────────────────────────────

        private Guid CurrentUserId
        {
            get
            {
                var user = Membership.GetUser();
                return user != null ? (Guid)user.ProviderUserKey : Guid.Empty;
            }
        }

        private Guid CurrentOrganizationId
        {
            get
            {
                using (var db = new DCReyla())
                {
                    var uid     = CurrentUserId;
                    var profile = db.Profiles.FirstOrDefault(p => p.UserId == uid);
                    return profile?.OrganizationId ?? Guid.Empty;
                }
            }
        }

        // ── Scope state — persisted in ViewState ──────────────────────────

        private string Scope
        {
            get { return (ViewState["Scope"] as string) ?? "mine"; }
            set { ViewState["Scope"] = value; }
        }

        // ── Page lifecycle ────────────────────────────────────────────────

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Default date range: last 30 days
                txtDateTo.Text   = DateTime.Today.ToString("yyyy-MM-dd");
                txtDateFrom.Text = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");

                BindFeed();
            }

            UpdateScopeButtons();
        }

        // ── Event handlers ────────────────────────────────────────────────

        protected void btnScope_Click(object sender, EventArgs e)
        {
            var btn = (LinkButton)sender;
            Scope = btn.CommandArgument;   // "mine" or "org"
            BindFeed();
            UpdateScopeButtons();
        }

        protected void Filter_Changed(object sender, EventArgs e)
        {
            BindFeed();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            ddlType.SelectedIndex = 0;
            txtDateFrom.Text      = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
            txtDateTo.Text        = DateTime.Today.ToString("yyyy-MM-dd");
            txtSearch.Text        = string.Empty;
            BindFeed();
        }

        // ── Data load ─────────────────────────────────────────────────────

        private void BindFeed()
        {
            var userId = CurrentUserId;
            var orgId  = CurrentOrganizationId;

            if (userId == Guid.Empty || orgId == Guid.Empty)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            var svc = new ActivityService();

            // Pull base set based on scope
            List<ActivityLogEntry> entries;
            if (Scope == "org")
            {
                // Use a generous lookback window; we'll filter by date below
                var since = DateTime.UtcNow.AddDays(-365);
                entries = svc.GetDigestActivity(orgId, since);
                litFeedTitle.Text = "All Org Activity";
            }
            else
            {
                entries = svc.GetUserActivity(userId, limit: 500);
                litFeedTitle.Text = "My Activity";
            }

            // ── Apply filters ──────────────────────────────────────────

            // Type filter
            // Prospect/Deal/Property filter by EntityType.
            // Note/Comp filter by ActivityType prefix since they're stored as EntityType=Prospect.
            string typeFilter = ddlType.SelectedValue;
            if (!string.IsNullOrEmpty(typeFilter))
            {
                switch (typeFilter)
                {
                    case "Prospect":
                        entries = entries.Where(e =>
                            e.ActivityType == ActivityType.ProspectCreated   ||
                            e.ActivityType == ActivityType.ProspectContacted ||
                            e.ActivityType == ActivityType.ProspectArchived).ToList();
                        break;

                    case "Note":
                        entries = entries.Where(e =>
                            e.ActivityType == ActivityType.NoteAdded   ||
                            e.ActivityType == ActivityType.NoteEdited  ||
                            e.ActivityType == ActivityType.NoteDeleted).ToList();
                        break;

                    case "Comp":
                        entries = entries.Where(e =>
                            e.ActivityType == ActivityType.CompAdded   ||
                            e.ActivityType == ActivityType.CompRemoved  ||
                            e.ActivityType == ActivityType.CompsCleared).ToList();
                        break;

                    case "Property":
                        entries = entries.Where(e =>
                            e.EntityType   == ActivityEntityType.Property ||
                            e.ActivityType == ActivityType.ContactInfoUpdated).ToList();
                        break;

                    case "Deal":
                        entries = entries.Where(e =>
                            e.ActivityType == ActivityType.DealCreated      ||
                            e.ActivityType == ActivityType.DealStageChanged ||
                            e.ActivityType == ActivityType.DealClosed       ||
                            e.ActivityType == ActivityType.DealUpdated      ||
                            e.ActivityType == ActivityType.DealNoteAdded    ||
                            e.ActivityType == ActivityType.DealNoteEdited   ||
                            e.ActivityType == ActivityType.DealNoteDeleted).ToList();
                        break;

                    case "Contact":
                        entries = entries.Where(e =>
                            e.EntityType   == ActivityEntityType.Contact    ||
                            e.ActivityType == ActivityType.ContactCreated   ||
                            e.ActivityType == ActivityType.ContactUpdated   ||
                            e.ActivityType == ActivityType.ContactDeleted   ||
                            e.ActivityType == ActivityType.ContactLinkedToProperty   ||
                            e.ActivityType == ActivityType.ContactUnlinkedFromProperty).ToList();
                        break;

                    default:
                        entries = entries.Where(e => e.EntityType == typeFilter).ToList();
                        break;
                }
            }

            // Date from — convert UTC to local before comparing so today's entries always show
            if (DateTime.TryParse(txtDateFrom.Text, out DateTime dateFrom))
                entries = entries.Where(e => e.CreatedAtUtc.ToLocalTime().Date >= dateFrom.Date).ToList();

            // Date to — add one day buffer so entries saved late UTC still appear
            if (DateTime.TryParse(txtDateTo.Text, out DateTime dateTo))
                entries = entries.Where(e => e.CreatedAtUtc.ToLocalTime().Date <= dateTo.Date).ToList();

            // Address search — matches against Summary which contains the address
            string search = (txtSearch.Text ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(search))
                entries = entries
                    .Where(e => e.Summary != null &&
                                e.Summary.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

            // ── Bind ───────────────────────────────────────────────────

            int count = entries.Count;
            litCount.Text = count.ToString("N0");

            string scopeDesc = Scope == "org" ? "organization's" : "your";
            litSubtitle.Text = count == 0
                ? string.Format("No activity found for {0} selected filters", scopeDesc)
                : string.Format("{0:N0} event{1} recorded for {2} account",
                                count, count == 1 ? "" : "s", scopeDesc);

            if (count == 0)
            {
                pnlEmpty.Visible    = true;
                pnlTimeline.Visible = false;
            }
            else
            {
                pnlEmpty.Visible    = false;
                pnlTimeline.Visible = true;

                rptActivity.DataSource = entries;
                rptActivity.DataBind();
            }
        }

        // ── Repeater item bound ───────────────────────────────────────────

        // Track last rendered date group so we only insert a header on change
        private string _lastDateGroup = null;

        protected void rptActivity_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item &&
                e.Item.ItemType != ListItemType.AlternatingItem) return;

            var entry         = (ActivityLogEntry)e.Item.DataItem;
            var litDateGroup  = (Literal)e.Item.FindControl("litDateGroup");
            var litProspect   = (Literal)e.Item.FindControl("litProspectLink");

            // ── Date group header ──────────────────────────────────────
            string groupLabel = GetDateGroupLabel(entry.CreatedAtUtc);
            if (groupLabel != _lastDateGroup)
            {
                litDateGroup.Text = string.Format(
                    "<div class=\"timeline-date-group\">{0}</div>",
                    HttpUtility.HtmlEncode(groupLabel));
                _lastDateGroup = groupLabel;
            }

            // ── Property detail link ────────────────────────────────────────
            // Build links using clip (→ PropertyDetail) and prospectId (→ Comps)
            // Note-related activities link directly to the ownership tab.
            var links = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(entry.ContactId))
            {
                links.AppendFormat(
                    "<a href=\"/Secure/Contacts/Contact.aspx?contactId={0}\" class=\"text-primary text-decoration-none me-3\">" +
                    "<i class=\"ti ti-user me-1\"></i>View Contact</a>",
                    HttpUtility.HtmlAttributeEncode(entry.ContactId));
            }

            if (!string.IsNullOrEmpty(entry.Clip))
            {
                // Note/contact activities deep-link to the Ownership & History tab
                bool isNoteOrContact = entry.ActivityType == ActivityType.NoteAdded     ||
                                       entry.ActivityType == ActivityType.NoteEdited    ||
                                       entry.ActivityType == ActivityType.NoteDeleted   ||
                                       entry.ActivityType == ActivityType.ProspectContacted ||
                                       entry.ActivityType == ActivityType.ContactInfoUpdated;

                string detailUrl = isNoteOrContact
                    ? string.Format("/Secure/Prospects/PropertyDetail.aspx?clip={0}&tab=contact",
                                    HttpUtility.HtmlAttributeEncode(entry.Clip))
                    : string.Format("/Secure/Prospects/PropertyDetail.aspx?clip={0}",
                                    HttpUtility.HtmlAttributeEncode(entry.Clip));

                links.AppendFormat(
                    "<a href=\"{0}\" class=\"text-primary text-decoration-none me-3\">" +
                    "<i class=\"ti ti-building-estate me-1\"></i>Property Detail</a>",
                    detailUrl);
            }

            if (!string.IsNullOrEmpty(entry.LinkedProspectId))
            {
                links.AppendFormat(
                    "<a href=\"/Secure/Prospects/Comps.aspx?prospectId={0}\" class=\"text-primary text-decoration-none\">" +
                    "<i class=\"ti ti-search me-1\"></i>View Comps</a>",
                    HttpUtility.HtmlAttributeEncode(entry.LinkedProspectId));
            }
            else if (entry.EntityType == ActivityEntityType.Prospect && entry.EntityId != Guid.Empty)
            {
                // Fallback for older entries that don't have prospectId in metadata
                links.AppendFormat(
                    "<a href=\"/Secure/Prospects/Comps.aspx?prospectId={0}\" class=\"text-primary text-decoration-none\">" +
                    "<i class=\"ti ti-search me-1\"></i>View Comps</a>",
                    entry.EntityId);
            }

            if (links.Length > 0)
            {
                litProspect.Text = string.Format(
                    "<div class=\"activity-prospect mt-1\">{0}</div>", links);
            }
        }

        // ── Scope button active state ─────────────────────────────────────

        private void UpdateScopeButtons()
        {
            if (Scope == "org")
            {
                btnScopeMine.CssClass = "btn btn-outline-secondary btn-sm";
                btnScopeOrg.CssClass  = "btn btn-primary btn-sm";
            }
            else
            {
                btnScopeMine.CssClass = "btn btn-primary btn-sm";
                btnScopeOrg.CssClass  = "btn btn-outline-secondary btn-sm";
            }
        }

        // ── Format helpers ────────────────────────────────────────────────

        protected string FormatTimeAgo(DateTime utc)
        {
            var local   = utc.ToLocalTime();
            var elapsed = DateTime.Now - local;

            if (elapsed.TotalMinutes < 1)  return "Just now";
            if (elapsed.TotalMinutes < 60) return (int)elapsed.TotalMinutes + "m ago";
            if (elapsed.TotalHours   < 24) return local.ToString("h:mm tt");
            if (elapsed.TotalDays    < 2)  return "Yesterday " + local.ToString("h:mm tt");
            return local.ToString("MMM d");
        }

        private string GetDateGroupLabel(DateTime utc)
        {
            var local   = utc.ToLocalTime().Date;
            var today   = DateTime.Today;
            var elapsed = today - local;

            if (elapsed.Days == 0) return "Today";
            if (elapsed.Days == 1) return "Yesterday";
            if (elapsed.Days < 7)  return local.ToString("dddd");  // "Monday" etc.
            return local.ToString("MMMM d, yyyy");
        }

        protected string GetDotClass(string activityType)
        {
            switch (activityType)
            {
                case ActivityType.ProspectCreated:
                case ActivityType.ProspectContacted:
                case ActivityType.ProspectArchived:
                    return "timeline-dot text-bg-primary";

                case ActivityType.CompAdded:
                    return "timeline-dot text-bg-success";

                case ActivityType.CompRemoved:
                    return "timeline-dot bg-danger-subtle";

                case ActivityType.CompsCleared:
                    return "timeline-dot bg-warning-subtle";

                case ActivityType.NoteAdded:
                case ActivityType.NoteEdited:
                case ActivityType.NoteDeleted:
                    return "timeline-dot text-bg-info";

                case ActivityType.DealCreated:
                case ActivityType.DealStageChanged:
                case ActivityType.DealClosed:
                case ActivityType.DealUpdated:
                case ActivityType.DealNoteAdded:
                case ActivityType.DealNoteEdited:
                case ActivityType.DealNoteDeleted:
                    return "timeline-dot text-bg-warning";

                case ActivityType.ValuationViewed:
                case ActivityType.ValuationSaved:
                    return "timeline-dot bg-primary-subtle";

                case ActivityType.ContactInfoUpdated:
                    return "timeline-dot text-bg-info";

                case ActivityType.ContactCreated:
                case ActivityType.ContactLinkedToProperty:
                    return "timeline-dot text-bg-success";

                case ActivityType.ContactUpdated:
                    return "timeline-dot text-bg-info";

                case ActivityType.ContactDeleted:
                case ActivityType.ContactUnlinkedFromProperty:
                    return "timeline-dot bg-danger-subtle";

                default:
                    return "timeline-dot bg-secondary-subtle";
            }
        }

        protected string GetIconClass(string activityType)
        {
            switch (activityType)
            {
                case ActivityType.ProspectCreated:   return "ti ti-building-estate";
                case ActivityType.ProspectContacted: return "ti ti-phone-call";
                case ActivityType.ProspectArchived:  return "ti ti-archive";

                case ActivityType.CompAdded:         return "ti ti-circle-plus";
                case ActivityType.CompRemoved:       return "ti ti-circle-minus text-danger";
                case ActivityType.CompsCleared:      return "ti ti-trash text-warning";

                case ActivityType.NoteAdded:         return "ti ti-notes";
                case ActivityType.NoteEdited:        return "ti ti-pencil";
                case ActivityType.NoteDeleted:       return "ti ti-notes-off";

                case ActivityType.DealCreated:       return "ti ti-briefcase";
                case ActivityType.DealStageChanged:  return "ti ti-arrows-right-left";
                case ActivityType.DealClosed:        return "ti ti-trophy";
                case ActivityType.DealUpdated:       return "ti ti-pencil text-warning";
                case ActivityType.DealNoteAdded:     return "ti ti-notes text-warning";
                case ActivityType.DealNoteEdited:    return "ti ti-pencil text-warning";
                case ActivityType.DealNoteDeleted:   return "ti ti-notes-off text-warning";

                case ActivityType.ValuationViewed:   return "ti ti-calculator text-primary";
                case ActivityType.ValuationSaved:    return "ti ti-device-floppy text-primary";

                case ActivityType.ContactInfoUpdated:  return "ti ti-address-book text-info";

                case ActivityType.ContactCreated:      return "ti ti-user-plus text-success";
                case ActivityType.ContactUpdated:      return "ti ti-user-edit text-info";
                case ActivityType.ContactDeleted:      return "ti ti-user-off text-danger";
                case ActivityType.ContactLinkedToProperty:   return "ti ti-link text-success";
                case ActivityType.ContactUnlinkedFromProperty: return "ti ti-unlink text-danger";

                default:                             return "ti ti-activity";
            }
        }

        protected string GetEntityTypeBadge(string entityType)
        {
            switch (entityType)
            {
                case ActivityEntityType.Prospect: return "&nbsp;<span class='badge text-bg-primary' style='font-size:10px'>Prospect</span>";
                case ActivityEntityType.Comp:     return "&nbsp;<span class='badge text-bg-success' style='font-size:10px'>Comp</span>";
                case ActivityEntityType.Note:     return "&nbsp;<span class='badge text-bg-info'    style='font-size:10px'>Note</span>";
                case ActivityEntityType.Deal:     return "&nbsp;<span class='badge text-bg-warning' style='font-size:10px'>Deal</span>";
                case ActivityEntityType.Property: return "&nbsp;<span class='badge text-bg-secondary' style='font-size:10px'>Contact</span>";
                case ActivityEntityType.Contact:  return "&nbsp;<span class='badge text-bg-success'   style='font-size:10px'>Contact</span>";
                default:                          return string.Empty;
            }
        }
    }
}
