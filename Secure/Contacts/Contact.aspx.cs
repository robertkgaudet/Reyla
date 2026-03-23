using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Reyla.Services;

namespace Reyla.Secure.Contacts
{
    public partial class Contact : System.Web.UI.Page
    {
        // Exposed to ASPX inline expressions for seeding JS
        protected string PreferredContactValue = string.Empty;
        protected string Contact_FirstName     = string.Empty;
        protected string Contact_LastName      = string.Empty;
        protected string Contact_Company       = string.Empty;
        protected string Contact_Email         = string.Empty;
        protected string Contact_Phone         = string.Empty;
        protected string Contact_Mobile        = string.Empty;
        protected string Contact_LinkedIn      = string.Empty;
        protected string Contact_Notes         = string.Empty;
        protected string Contact_TypeIds       = string.Empty;

        // Export URLs — built in BindPage once contact is loaded
        protected string VCardUrl          = "#";
        protected string GoogleContactsUrl = "#";
        protected string OutlookUrl        = "#";

        private ContactRow _contact;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                BindPage();
        }

        private void BindPage()
        {
            var contactIdStr = Request.QueryString["contactId"];
            if (!Guid.TryParse(contactIdStr, out Guid contactId))
            {
                ShowNotFound();
                return;
            }

            var userId   = GetCurrentUserId();
            var orgId    = GetCurrentOrgId();
            var username = User.Identity.Name;

            if (userId == Guid.Empty || orgId == Guid.Empty)
            {
                Response.Redirect("~/Signin.aspx");
                return;
            }

            var svc = new ContactService();
            _contact = svc.GetContact(contactId, orgId, userId, username);

            if (_contact == null)
            {
                ShowNotFound();
                return;
            }

            // Seed hidden fields
            hdnContactId.Value  = _contact.ContactId.ToString();
            hdnHandlerUrl.Value = ResolveUrl("~/Secure/Contacts/ContactHandler.ashx");
            hdnCanEdit.Value    = "true";

            // Seed JS-accessible properties
            PreferredContactValue = _contact.PreferredContact ?? string.Empty;
            Contact_FirstName     = _contact.FirstName    ?? string.Empty;
            Contact_LastName      = _contact.LastName     ?? string.Empty;
            Contact_Company       = _contact.CompanyName  ?? string.Empty;
            Contact_Email         = _contact.Email        ?? string.Empty;
            Contact_Phone         = _contact.Phone        ?? string.Empty;
            Contact_Mobile        = _contact.MobilePhone  ?? string.Empty;
            Contact_LinkedIn      = _contact.LinkedInUrl  ?? string.Empty;
            Contact_Notes         = _contact.Notes        ?? string.Empty;
            Contact_TypeIds       = _contact.ContactTypes?.Count > 0
                ? string.Join(",", GetTypeIdsByNames(_contact.ContactTypes, svc))
                : string.Empty;

            BindHeader();
            BindContactInfo();
            BindTypes(svc);
            BindProperties();
            BindActivity();
            BuildExportUrls();
        }

        private void BindHeader()
        {
            // Avatar initials and color
            string initials = GetInitials(_contact.FirstName, _contact.LastName, _contact.CompanyName);
            string color    = GetAvatarColor(_contact.ContactId);
            litInitials.Text          = HttpUtility.HtmlEncode(initials);
            divAvatar.Style["background"] = color;

            // Name
            string fullName = _contact.FullName;
            divDisplayName.InnerText  = fullName;

            // Company subtitle
            if (!string.IsNullOrWhiteSpace(_contact.CompanyName) &&
                _contact.CompanyName != fullName)
                divCompanyLine.InnerHtml = "<i class='ti ti-building me-1'></i>" +
                                           HttpUtility.HtmlEncode(_contact.CompanyName);

            // Type badges
            if (_contact.ContactTypes?.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                var priority = new[] { "Buyer", "Seller", "Property Owner" };
                var sorted   = new List<string>(_contact.ContactTypes);
                sorted.Sort((a, b) => {
                    int ai = Array.IndexOf(priority, a);
                    int bi = Array.IndexOf(priority, b);
                    if (ai < 0 && bi < 0) return 0;
                    if (ai < 0) return 1;
                    if (bi < 0) return -1;
                    return ai.CompareTo(bi);
                });

                foreach (var t in sorted)
                {
                    bool isPri  = Array.IndexOf(priority, t) >= 0;
                    string cls  = GetTypeBadgeClass(t);
                    string size = isPri ? "font-size:0.8rem;font-weight:700;" : "font-size:0.75rem;";
                    sb.Append($"<span class='badge {cls} me-1' style='{size}'>{HttpUtility.HtmlEncode(t)}</span>");
                }
                phTypeBadges.Controls.Add(new LiteralControl(sb.ToString()));
            }

            // Buyer score
            if (_contact.BuyerScore.HasValue)
            {
                string cls   = _contact.BuyerScore >= 70 ? "text-bg-success" : _contact.BuyerScore >= 40 ? "text-bg-warning" : "text-bg-danger";
                string label = _contact.BuyerScore >= 70 ? "High" : _contact.BuyerScore >= 40 ? "Med" : "Low";
                phBuyerScore.Controls.Add(new LiteralControl(
                    $"<span class='badge {cls} fs-xs' title='Buyer Score'>Score: {_contact.BuyerScore} <span class='opacity-75'>{label}</span></span>"));
            }

            // Dates
            litCreatedDate.Text = _contact.CreatedAtUtc.ToLocalTime().ToString("MMM d, yyyy");
            if (_contact.UpdatedAtUtc.HasValue)
            {
                litUpdatedDate.Text    = _contact.UpdatedAtUtc.Value.ToLocalTime().ToString("MMM d, yyyy");
                pnlUpdatedDate.Visible = true;
            }
            else
            {
                pnlUpdatedDate.Visible = false;
            }
        }

        private void BindContactInfo()
        {
            litPhone.Text    = FormatPhone(_contact.Phone);
            litMobile.Text   = FormatPhone(_contact.MobilePhone);
            litEmail.Text    = FormatEmail(_contact.Email);
            litLinkedIn.Text = FormatLinkedIn(_contact.LinkedInUrl);

            pnlNotes.Visible = !string.IsNullOrWhiteSpace(_contact.Notes);
            litNotes.Text    = HttpUtility.HtmlEncode(_contact.Notes ?? string.Empty);
        }

        private void BindTypes(ContactService svc)
        {
            if (_contact.ContactTypes == null || _contact.ContactTypes.Count == 0)
            {
                pnlNoTypes.Visible   = true;
                pnlTypesList.Visible = false;
                return;
            }

            pnlNoTypes.Visible   = false;
            pnlTypesList.Visible = true;

            var sb = new System.Text.StringBuilder();
            foreach (var t in _contact.ContactTypes)
            {
                string cls = GetTypeBadgeClass(t);
                sb.Append($"<span class='badge {cls} me-2 mb-1' style='font-size:0.85rem;'>{HttpUtility.HtmlEncode(t)}</span>");
            }
            litTypesList.Text = sb.ToString();
        }

        private void BindProperties()
        {
            if (_contact.Properties == null || _contact.Properties.Count == 0)
            {
                pnlNoProperties.Visible   = true;
                pnlPropertiesList.Visible = false;
                return;
            }

            pnlNoProperties.Visible   = false;
            pnlPropertiesList.Visible = true;

            rptProperties.DataSource = _contact.Properties
                .OrderByDescending(p => p.IsPrimary)
                .ThenBy(p => p.StreetAddress)
                .ToList();
            rptProperties.DataBind();
        }

        private void BindActivity()
        {
            try
            {
                using (var db = new DCReyla())
                {
                    var logs = db.ActivityLogs
                        .Where(a => a.EntityId   == _contact.ContactId &&
                                    a.EntityType == "Contact")
                        .OrderByDescending(a => a.CreatedAtUtc)
                        .Take(20)
                        .ToList();

                    if (!logs.Any())
                    {
                        pnlNoActivity.Visible   = true;
                        pnlActivityList.Visible = false;
                        return;
                    }

                    pnlNoActivity.Visible   = false;
                    pnlActivityList.Visible = true;

                    var rows = logs.Select(l => new
                    {
                        Icon    = GetActivityIcon(l.ActivityType),
                        Summary = BuildActivitySummary(l.ActivityType),
                        When    = FormatTimeAgo(l.CreatedAtUtc)
                    }).ToList();

                    rptActivity.DataSource = rows;
                    rptActivity.DataBind();
                }
            }
            catch
            {
                pnlNoActivity.Visible   = true;
                pnlActivityList.Visible = false;
            }
        }

        private void BuildExportUrls()
        {
            // vCard download
            VCardUrl = ResolveUrl(
                $"~/Secure/Contacts/ContactVCard.ashx?contactId={_contact.ContactId}");

            // Google Contacts — pre-fill URL is unreliable in new UI,
            // direct to import page instead so user can import the vCard
            GoogleContactsUrl = "https://contacts.google.com/import";

            // Outlook Web new contact pre-fill URL
            // https://outlook.live.com/people/0/create?firstName=...&lastName=...&emailAddress=...&phone=...
            var oParams = new System.Collections.Specialized.NameValueCollection();
            if (!string.IsNullOrWhiteSpace(_contact.FirstName))  oParams["firstName"]     = _contact.FirstName;
            if (!string.IsNullOrWhiteSpace(_contact.LastName))   oParams["lastName"]      = _contact.LastName;
            if (!string.IsNullOrWhiteSpace(_contact.Email))      oParams["emailAddress"]  = _contact.Email;
            if (!string.IsNullOrWhiteSpace(_contact.Phone))      oParams["phone"]         = _contact.Phone;
            if (!string.IsNullOrWhiteSpace(_contact.CompanyName))oParams["companyName"]   = _contact.CompanyName;

            var oQuery = string.Join("&", oParams.AllKeys
                .Select(k => Uri.EscapeDataString(k) + "=" + Uri.EscapeDataString(oParams[k])));
            OutlookUrl = "https://outlook.live.com/people/0/create" +
                         (oQuery.Length > 0 ? "?" + oQuery : string.Empty);
        }

        private void ShowNotFound()
        {
            pnlNotFound.Visible = true;
            pnlMain.Visible     = false;
        }

        // ── Format helpers ────────────────────────────────────────────────

        private static string FormatPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "<span class='text-muted'>—</span>";
            return $"<a href='tel:{HttpUtility.HtmlAttributeEncode(phone)}' class='text-decoration-none'>" +
                   $"<i class='ti ti-phone me-1'></i>{HttpUtility.HtmlEncode(phone)}</a>";
        }

        private static string FormatEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "<span class='text-muted'>—</span>";
            return $"<a href='mailto:{HttpUtility.HtmlAttributeEncode(email)}' class='text-decoration-none'>" +
                   $"<i class='ti ti-mail me-1'></i>{HttpUtility.HtmlEncode(email)}</a>";
        }

        private static string FormatLinkedIn(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "<span class='text-muted'>—</span>";
            return $"<a href='{HttpUtility.HtmlAttributeEncode(url)}' target='_blank' class='text-decoration-none text-primary'>" +
                   $"<i class='ti ti-brand-linkedin me-1'></i>LinkedIn Profile</a>";
        }

        private static string GetInitials(string first, string last, string company)
        {
            if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last))
                return (first[0].ToString() + last[0].ToString()).ToUpper();
            if (!string.IsNullOrWhiteSpace(first))  return first[0].ToString().ToUpper();
            if (!string.IsNullOrWhiteSpace(last))   return last[0].ToString().ToUpper();
            if (!string.IsNullOrWhiteSpace(company)) return company[0].ToString().ToUpper();
            return "?";
        }

        private static string GetAvatarColor(Guid id)
        {
            var colors = new[] {
                "#3b82f6","#10b981","#f59e0b","#ef4444",
                "#8b5cf6","#06b6d4","#ec4899","#84cc16"
            };
            int sum = 0;
            var bytes = id.ToByteArray();
            for (int i = 0; i < Math.Min(bytes.Length, 8); i++) sum += bytes[i];
            return colors[sum % colors.Length];
        }

        private static string GetTypeBadgeClass(string type)
        {
            switch (type)
            {
                case "Property Owner":             return "text-bg-primary";
                case "Buyer":                      return "text-bg-success";
                case "Seller":                     return "text-bg-warning";
                case "Decision Maker":             return "text-bg-info";
                case "LLC / Trust Representative": return "text-bg-dark";
                default:                           return "text-bg-secondary";
            }
        }

        private static string GetActivityIcon(string activityType)
        {
            switch (activityType)
            {
                case "ContactCreated":              return "ti ti-user-plus";
                case "ContactUpdated":              return "ti ti-pencil";
                case "ContactLinkedToProperty":     return "ti ti-link";
                case "ContactUnlinkedFromProperty": return "ti ti-unlink";
                default:                            return "ti ti-activity";
            }
        }

        private static string BuildActivitySummary(string activityType)
        {
            switch (activityType)
            {
                case "ContactCreated":              return "Contact was created.";
                case "ContactUpdated":              return "Contact details were updated.";
                case "ContactLinkedToProperty":     return "Linked to a property.";
                case "ContactUnlinkedFromProperty": return "Removed from a property.";
                default:                            return activityType;
            }
        }

        private static string FormatTimeAgo(DateTime utc)
        {
            var diff = DateTime.UtcNow - utc;
            if (diff.TotalMinutes < 1)   return "just now";
            if (diff.TotalHours   < 1)   return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalDays    < 1)   return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays    < 30)  return $"{(int)diff.TotalDays}d ago";
            return utc.ToLocalTime().ToString("MMM d, yyyy");
        }

        // Gets ContactType GUIDs by name for seeding the edit modal checkboxes
        private List<string> GetTypeIdsByNames(List<string> names, ContactService svc)
        {
            var all = svc.GetContactTypes();
            return all
                .Where(t => names.Contains(t.Name))
                .Select(t => t.ContactTypeId.ToString())
                .ToList();
        }

        // JS-safe string escaping for inline script expressions
        protected string EscJs(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace("'",  "\\'")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n");
        }

        // ── Auth helpers ──────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var user = Membership.GetUser();
            return user != null ? (Guid)user.ProviderUserKey : Guid.Empty;
        }

        private Guid GetCurrentOrgId()
        {
            using (var db = new DCReyla())
            {
                var userId  = GetCurrentUserId();
                var profile = db.Profiles.FirstOrDefault(p => p.UserId == userId);
                return profile?.OrganizationId ?? Guid.Empty;
            }
        }
    }
}
