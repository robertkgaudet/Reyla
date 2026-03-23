using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Newtonsoft.Json;
using Reyla.Services;

namespace Reyla.Secure.Prospects
{
    /// <summary>
    /// Handles contact operations on PropertyDetail.aspx.
    /// Actions: getcontacts, linknew, linkexisting, unlink, setprimary, searchcontacts
    /// Auth pattern matches CompToggle/DealToggle exactly.
    /// </summary>
    public class PropertyContactHandler : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetNoStore();

            try
            {
                // ── Auth ─────────────────────────────────────────────────────
                var identity = context.User?.Identity;
                if (identity == null || !identity.IsAuthenticated || string.IsNullOrEmpty(identity.Name))
                {
                    context.Response.StatusCode = 401;
                    Write(context, Error("Not authenticated."));
                    return;
                }

                var memberUser = Membership.GetUser(identity.Name);
                if (memberUser == null)
                {
                    context.Response.StatusCode = 401;
                    Write(context, Error("User not found."));
                    return;
                }

                var userId = (Guid)memberUser.ProviderUserKey;
                Guid orgId;

                using (var db = new DCReyla())
                {
                    var profile = db.Profiles.FirstOrDefault(p => p.UserId == userId);
                    if (profile == null || !profile.OrganizationId.HasValue)
                    {
                        Write(context, Error("Profile not found or has no organization."));
                        return;
                    }
                    orgId = profile.OrganizationId.Value;
                }

                string action = Param(context, "action");

                switch (action)
                {
                    case "getcontacts":    HandleGetContacts(context, userId, orgId);    break;
                    case "linknew":        HandleLinkNew(context, userId, orgId);        break;
                    case "linkexisting":   HandleLinkExisting(context, userId, orgId);  break;
                    case "unlink":         HandleUnlink(context, userId, orgId);        break;
                    case "setprimary":     HandleSetPrimary(context, userId, orgId);    break;
                    case "setpreferred":   HandleSetPreferred(context, userId, orgId);  break;
                    case "searchcontacts": HandleSearchContacts(context, userId, orgId);break;
                    default:               Write(context, Error("Unknown action: " + action)); break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[PropertyContactHandler] {0}", ex);
                Write(context, Error("Server error: " + ex.Message
                    + " | Inner: " + (ex.InnerException?.Message ?? "none")));
            }
        }

        // ----------------------------------------------------------------
        // GET CONTACTS — load all contacts for a property grouped by type
        // ----------------------------------------------------------------
        private static void HandleGetContacts(HttpContext context, Guid userId, Guid orgId)
        {
            string propertyIdStr = Param(context, "propertyId");
            if (!Guid.TryParse(propertyIdStr, out Guid propertyId))
            {
                Write(context, Error("Invalid propertyId."));
                return;
            }

            var svc      = new ContactService();
            var contacts = svc.GetContactsForProperty(propertyId, orgId);
            var types    = svc.GetContactTypes();

            var contactDtos = contacts.Select(c => MapContactDto(c)).ToList();

            Write(context, JsonConvert.SerializeObject(new
            {
                success  = true,
                contacts = contactDtos,
                types    = types.Select(t => new { t.ContactTypeId, t.Name }).ToList()
            }));
        }

        // ----------------------------------------------------------------
        // LINK NEW CONTACT — create a new contact and link to property
        // ----------------------------------------------------------------
        private static void HandleLinkNew(HttpContext context, Guid userId, Guid orgId)
        {
            string propertyIdStr = Param(context, "propertyId");
            if (!Guid.TryParse(propertyIdStr, out Guid propertyId))
            {
                Write(context, Error("Invalid propertyId."));
                return;
            }

            string body    = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
            var    request = JsonConvert.DeserializeObject<ContactSaveRequest>(body);
            bool   isPrimary = string.Equals(Param(context, "isPrimary"), "true", StringComparison.OrdinalIgnoreCase);

            // Create the contact
            var contactSvc    = new ContactService();
            var saveResult    = contactSvc.SaveContact(request, orgId, userId);

            if (!saveResult.Success && !saveResult.WasDuplicate)
            {
                Write(context, Error(saveResult.Error ?? "Failed to save contact."));
                return;
            }

            var contactId = saveResult.ContactId.Value;

            // Link to property
            contactSvc.LinkContactToProperty(contactId, propertyId, orgId, userId, isPrimary);

            // Activity log
            TrackContactActivity("ContactLinkedToProperty", propertyId, userId, orgId, context, contactId);

            // Return updated contact list
            var contacts    = contactSvc.GetContactsForProperty(propertyId, orgId);
            var contactDtos = contacts.Select(c => MapContactDto(c)).ToList();

            Write(context, JsonConvert.SerializeObject(new
            {
                success      = true,
                wasDuplicate = saveResult.WasDuplicate,
                contacts     = contactDtos
            }));
        }

        // ----------------------------------------------------------------
        // LINK EXISTING CONTACT — link an existing contact to property
        // ----------------------------------------------------------------
        private static void HandleLinkExisting(HttpContext context, Guid userId, Guid orgId)
        {
            string propertyIdStr = Param(context, "propertyId");
            string contactIdStr  = Param(context, "contactId");
            bool   isPrimary     = string.Equals(Param(context, "isPrimary"), "true", StringComparison.OrdinalIgnoreCase);

            if (!Guid.TryParse(propertyIdStr, out Guid propertyId) ||
                !Guid.TryParse(contactIdStr,  out Guid contactId))
            {
                Write(context, Error("Invalid propertyId or contactId."));
                return;
            }

            var svc = new ContactService();
            svc.LinkContactToProperty(contactId, propertyId, orgId, userId, isPrimary);

            TrackContactActivity("ContactLinkedToProperty", propertyId, userId, orgId, context, contactId);

            var contacts    = svc.GetContactsForProperty(propertyId, orgId);
            var contactDtos = contacts.Select(c => MapContactDto(c)).ToList();

            Write(context, JsonConvert.SerializeObject(new { success = true, contacts = contactDtos }));
        }

        // ----------------------------------------------------------------
        // UNLINK CONTACT — remove a contact from a property
        // ----------------------------------------------------------------
        private static void HandleUnlink(HttpContext context, Guid userId, Guid orgId)
        {
            string propertyContactIdStr = Param(context, "propertyContactId");
            string propertyIdStr        = Param(context, "propertyId");

            if (!Guid.TryParse(propertyContactIdStr, out Guid propertyContactId) ||
                !Guid.TryParse(propertyIdStr,        out Guid propertyId))
            {
                Write(context, Error("Invalid propertyContactId or propertyId."));
                return;
            }

            var svc = new ContactService();
            svc.UnlinkContactFromProperty(propertyContactId, orgId, userId);

            TrackContactActivity("ContactUnlinkedFromProperty", propertyId, userId, orgId, context, null);

            var contacts    = svc.GetContactsForProperty(propertyId, orgId);
            var contactDtos = contacts.Select(c => MapContactDto(c)).ToList();

            Write(context, JsonConvert.SerializeObject(new { success = true, contacts = contactDtos }));
        }

        // ----------------------------------------------------------------
        // SET PRIMARY — make a contact the primary for this property
        // ----------------------------------------------------------------
        private static void HandleSetPrimary(HttpContext context, Guid userId, Guid orgId)
        {
            string propertyContactIdStr = Param(context, "propertyContactId");
            string propertyIdStr        = Param(context, "propertyId");

            if (!Guid.TryParse(propertyContactIdStr, out Guid propertyContactId) ||
                !Guid.TryParse(propertyIdStr,        out Guid propertyId))
            {
                Write(context, Error("Invalid propertyContactId or propertyId."));
                return;
            }

            var svc = new ContactService();
            svc.SetPrimaryContact(propertyContactId, propertyId, orgId, userId);

            var contacts    = svc.GetContactsForProperty(propertyId, orgId);
            var contactDtos = contacts.Select(c => MapContactDto(c)).ToList();

            Write(context, JsonConvert.SerializeObject(new { success = true, contacts = contactDtos }));
        }

        // ----------------------------------------------------------------
        // SEARCH CONTACTS — find existing contacts by name/email for linking
        // ----------------------------------------------------------------
        private static void HandleSearchContacts(HttpContext context, Guid userId, Guid orgId)
        {
            string query = Param(context, "q") ?? string.Empty;
            query = query.Trim().ToLower();

            using (var db = new DCReyla())
            {
                var results = db.Contacts
                    .Where(c =>
                        c.OrganizationId == orgId &&
                        c.IsDeleted      == false &&
                        (c.FirstName.Contains(query) ||
                         c.LastName.Contains(query)  ||
                         c.Email.Contains(query)     ||
                         c.CompanyName.Contains(query)))
                    .OrderBy(c => c.LastName)
                    .Take(20)
                    .Select(c => new
                    {
                        contactId   = c.ContactId.ToString(),
                        firstName   = c.FirstName,
                        lastName    = c.LastName,
                        companyName = c.CompanyName,
                        email       = c.Email,
                        phone       = c.Phone,
                        displayName = (c.FirstName + " " + c.LastName).Trim()
                    })
                    .ToList();

                Write(context, JsonConvert.SerializeObject(new { success = true, results }));
            }
        }

        // ----------------------------------------------------------------
        // SET PREFERRED CONTACT METHOD — saves on the Contact record
        // ----------------------------------------------------------------
        private static void HandleSetPreferred(HttpContext context, Guid userId, Guid orgId)
        {
            string contactIdStr = Param(context, "contactId");
            string preferred    = Param(context, "preferred") ?? string.Empty;

            if (!Guid.TryParse(contactIdStr, out Guid contactId))
            {
                Write(context, Error("Invalid contactId."));
                return;
            }

            var allowed = new[] { "", "Phone", "Email", "Mail", "InPerson" };
            if (!System.Array.Exists(allowed, v => v == preferred))
            {
                Write(context, Error("Invalid preferred value: " + preferred));
                return;
            }

            try
            {
                using (var db = new DCReyla())
                {
                    var contact = db.Contacts
                        .FirstOrDefault(c => c.ContactId == contactId
                                          && c.OrganizationId == orgId
                                          && c.IsDeleted == false);

                    if (contact == null)
                    {
                        Write(context, Error("Contact not found."));
                        return;
                    }

                    contact.PreferredContact = string.IsNullOrEmpty(preferred) ? null : preferred;
                    contact.UpdatedAtUtc     = DateTime.UtcNow;
                    contact.UpdatedBy        = userId;
                    db.SubmitChanges();
                }

                Write(context, JsonConvert.SerializeObject(new { success = true }));
            }
            catch (Exception ex)
            {
                Write(context, Error("HandleSetPreferred error: " + ex.Message));
            }
        }

        // ----------------------------------------------------------------
        private static object MapContactDto(ContactRow c)
        {
            // Find IsPrimary from the PropertyContact link
            bool isPrimary = c.Properties?.Any(p => p.IsPrimary) == true;
            string propertyContactId = c.Properties?.FirstOrDefault()?.PropertyContactId.ToString();

            return new
            {
                contactId          = c.ContactId.ToString(),
                propertyContactId  = propertyContactId,
                firstName          = c.FirstName,
                lastName           = c.LastName,
                fullName           = c.FullName,
                companyName        = c.CompanyName,
                email              = c.Email,
                phone              = c.Phone,
                mobilePhone        = c.MobilePhone,
                linkedInUrl        = c.LinkedInUrl,
                notes              = c.Notes,
                isPrimary          = isPrimary,
                contactTypes       = c.ContactTypes,
                buyerScore         = c.BuyerScore,
                preferredContact   = c.PreferredContact
            };
        }

        private static void TrackContactActivity(
            string activityType,
            Guid   propertyId,
            Guid   userId,
            Guid   orgId,
            HttpContext context,
            Guid?  contactId)
        {
            try
            {
                var actSvc = new ActivityService();
                actSvc.Track(
                    activityType:  activityType,
                    entityType:    "Property",
                    entityId:      propertyId,
                    userId:        userId,
                    organizationId: orgId,
                    metadata:      contactId.HasValue ? new { contactId = contactId.Value.ToString() } : null);
            }
            catch { /* activity logging is fire-and-forget */ }
        }

        private static void Write(HttpContext ctx, string json) => ctx.Response.Write(json);
        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new { success = false, error = msg });
        private static string Param(HttpContext ctx, string key) =>
            (ctx.Request.Form[key] ?? ctx.Request.QueryString[key])?.Trim();
    }
}
