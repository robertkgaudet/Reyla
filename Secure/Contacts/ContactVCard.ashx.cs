using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Reyla.Services;

namespace Reyla.Secure.Contacts
{
    /// <summary>
    /// Generates a vCard (.vcf) file download for a contact.
    /// Usage: /Secure/Contacts/ContactVCard.ashx?contactId=GUID
    /// </summary>
    public class ContactVCard : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            // ── Auth ─────────────────────────────────────────────────────
            var identity = context.User?.Identity;
            if (identity == null || !identity.IsAuthenticated)
            {
                context.Response.StatusCode = 401;
                context.Response.End();
                return;
            }

            var memberUser = Membership.GetUser(identity.Name);
            if (memberUser == null)
            {
                context.Response.StatusCode = 401;
                context.Response.End();
                return;
            }

            var userId = (Guid)memberUser.ProviderUserKey;
            Guid orgId;

            using (var db = new DCReyla())
            {
                var profile = db.Profiles.FirstOrDefault(p => p.UserId == userId);
                if (profile == null || !profile.OrganizationId.HasValue)
                {
                    context.Response.StatusCode = 403;
                    context.Response.End();
                    return;
                }
                orgId = profile.OrganizationId.Value;
            }

            // ── Load contact ─────────────────────────────────────────────
            var contactIdStr = context.Request.QueryString["contactId"];
            if (!Guid.TryParse(contactIdStr, out Guid contactId))
            {
                context.Response.StatusCode = 400;
                context.Response.End();
                return;
            }

            var svc     = new ContactService();
            var contact = svc.GetContact(contactId, orgId, userId, identity.Name);

            if (contact == null)
            {
                context.Response.StatusCode = 404;
                context.Response.End();
                return;
            }

            // ── Build vCard ───────────────────────────────────────────────
            var vcf = BuildVCard(contact);

            // ── Safe filename ─────────────────────────────────────────────
            var displayName = contact.FullName != "(No Name)"
                ? contact.FullName
                : contact.CompanyName ?? "contact";
            var filename = MakeSafeFilename(displayName) + ".vcf";

            context.Response.ContentType = "text/vcard";
            context.Response.AddHeader("Content-Disposition",
                "attachment; filename=\"" + filename + "\"");
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.Write(vcf);
            context.Response.End();
        }

        private static string BuildVCard(ContactRow c)
        {
            var sb = new StringBuilder();

            sb.AppendLine("BEGIN:VCARD");
            sb.AppendLine("VERSION:3.0");

            // Name — N field: Last;First;Middle;Prefix;Suffix
            var last  = Vcf(c.LastName  ?? string.Empty);
            var first = Vcf(c.FirstName ?? string.Empty);
            sb.AppendLine($"N:{last};{first};;;");

            // Display name
            var fullName = !string.IsNullOrWhiteSpace(c.FirstName) || !string.IsNullOrWhiteSpace(c.LastName)
                ? $"{c.FirstName} {c.LastName}".Trim()
                : c.CompanyName ?? string.Empty;
            sb.AppendLine($"FN:{Vcf(fullName)}");

            // Company
            if (!string.IsNullOrWhiteSpace(c.CompanyName))
                sb.AppendLine($"ORG:{Vcf(c.CompanyName)}");

            // Phone
            if (!string.IsNullOrWhiteSpace(c.Phone))
                sb.AppendLine($"TEL;TYPE=WORK,VOICE:{Vcf(c.Phone)}");

            // Mobile
            if (!string.IsNullOrWhiteSpace(c.MobilePhone))
                sb.AppendLine($"TEL;TYPE=CELL:{Vcf(c.MobilePhone)}");

            // Email
            if (!string.IsNullOrWhiteSpace(c.Email))
                sb.AppendLine($"EMAIL;TYPE=INTERNET:{Vcf(c.Email)}");

            // LinkedIn as URL
            if (!string.IsNullOrWhiteSpace(c.LinkedInUrl))
                sb.AppendLine($"URL:{Vcf(c.LinkedInUrl)}");

            // Notes
            if (!string.IsNullOrWhiteSpace(c.Notes))
                sb.AppendLine($"NOTE:{Vcf(c.Notes).Replace("\n", "\\n").Replace("\r", "")}");

            // Contact types as categories
            if (c.ContactTypes?.Count > 0)
                sb.AppendLine($"CATEGORIES:{string.Join(",", c.ContactTypes.Select(Vcf))}");

            // Source tag
            sb.AppendLine("PRODID:-//Reyla.ai//Contact Export//EN");
            sb.AppendLine($"REV:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");

            sb.AppendLine("END:VCARD");

            return sb.ToString();
        }

        // Escape special vCard characters
        private static string Vcf(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace(",",  "\\,")
                    .Replace(";",  "\\;");
        }

        private static string MakeSafeFilename(string name)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var c in name)
                sb.Append(invalid.Contains(c) ? '_' : c);
            return sb.ToString().Trim().Replace(' ', '_');
        }
    }
}
