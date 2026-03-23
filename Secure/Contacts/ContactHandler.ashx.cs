using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Newtonsoft.Json;
using Reyla.Services;

namespace Reyla.Secure.Contacts
{
    public class ContactHandler : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetNoStore();

            try
            {
                // ── Auth — matches CompToggle/DealToggle exactly ──────────
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

                // ── OrgId from Profile.OrganizationId — matches CompToggle/DealToggle ──
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
                    case "save":     HandleSave(context, userId, orgId);   break;
                    case "delete":   HandleDelete(context, userId, orgId); break;
                    case "gettypes": HandleGetTypes(context);              break;
                    default:         Write(context, Error("Unknown action: " + action)); break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[ContactHandler] {0}", ex);
                Write(context, Error("Server error: " + ex.Message
                    + " | Inner: " + (ex.InnerException?.Message ?? "none")));
            }
        }

        private static void HandleSave(HttpContext context, Guid userId, Guid orgId)
        {
            try
            {
                string body    = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
                var    request = JsonConvert.DeserializeObject<ContactSaveRequest>(body);
                bool   isNew   = !request.ContactId.HasValue;
                var    svc     = new ContactService();
                var    result  = svc.SaveContact(request, orgId, userId);

                if (result.Success && result.ContactId.HasValue)
                {
                    // Build display name for activity summary
                    string contactName = ((request.FirstName ?? "") + " " + (request.LastName ?? "")).Trim();
                    if (string.IsNullOrEmpty(contactName)) contactName = request.CompanyName ?? "contact";

                    var actSvc = new ActivityService();
                    actSvc.Track(
                        activityType:   isNew ? ActivityType.ContactCreated : ActivityType.ContactUpdated,
                        entityType:     ActivityEntityType.Contact,
                        entityId:       result.ContactId.Value,
                        userId:         userId,
                        organizationId: orgId,
                        metadata:       new { contactId = result.ContactId.Value.ToString(), contactName });
                }

                Write(context, JsonConvert.SerializeObject(new
                {
                    success      = result.Success,
                    error        = result.Error,
                    contactId    = result.ContactId.HasValue ? result.ContactId.Value.ToString() : null,
                    wasDuplicate = result.WasDuplicate
                }));
            }
            catch (Exception ex)
            {
                Write(context, Error("HandleSave error: " + ex.Message
                    + " | Inner: " + (ex.InnerException?.Message ?? "none")));
            }
        }

        private static void HandleDelete(HttpContext context, Guid userId, Guid orgId)
        {
            try
            {
                string contactIdStr = Param(context, "contactId");
                if (!Guid.TryParse(contactIdStr, out Guid contactId))
                {
                    Write(context, Error("Invalid contactId: " + contactIdStr));
                    return;
                }

                // Load contact name before deleting for activity summary
                string contactName = "contact";
                try
                {
                    using (var db = new DCReyla())
                    {
                        var c = db.Contacts.FirstOrDefault(x => x.ContactId == contactId);
                        if (c != null)
                            contactName = ((c.FirstName ?? "") + " " + (c.LastName ?? "")).Trim();
                        if (string.IsNullOrEmpty(contactName)) contactName = c?.CompanyName ?? "contact";
                    }
                }
                catch { /* non-fatal */ }

                var svc    = new ContactService();
                var result = svc.DeleteContact(contactId, orgId, userId);

                if (result)
                {
                    var actSvc = new ActivityService();
                    actSvc.Track(
                        activityType:   ActivityType.ContactDeleted,
                        entityType:     ActivityEntityType.Contact,
                        entityId:       contactId,
                        userId:         userId,
                        organizationId: orgId,
                        metadata:       new { contactId = contactId.ToString(), contactName });
                }

                Write(context, JsonConvert.SerializeObject(new { success = result }));
            }
            catch (Exception ex)
            {
                Write(context, Error("HandleDelete error: " + ex.Message));
            }
        }

        private static void HandleGetTypes(HttpContext context)
        {
            try
            {
                var svc   = new ContactService();
                var types = svc.GetContactTypes();
                Write(context, JsonConvert.SerializeObject(new
                {
                    success = true,
                    types   = types.Select(t => new { t.ContactTypeId, t.Name }).ToList()
                }));
            }
            catch (Exception ex)
            {
                Write(context, Error("HandleGetTypes error: " + ex.Message));
            }
        }

        private static void Write(HttpContext ctx, string json) => ctx.Response.Write(json);
        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new { success = false, error = msg });
        private static string Param(HttpContext ctx, string key) =>
            (ctx.Request.Form[key] ?? ctx.Request.QueryString[key])?.Trim();
    }
}
