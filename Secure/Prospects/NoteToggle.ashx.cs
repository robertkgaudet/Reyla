using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Newtonsoft.Json;
using Reyla.Services;

namespace Reyla.Secure.Prospects
{
    /// <summary>
    /// Lightweight JSON handler for client-side note and contacted-date operations.
    ///
    /// POST parameters:
    ///   action       — "addNote" | "editNote" | "deleteNote" | "setContacted" | "clearContacted"
    ///   prospectId   — Guid (required for all actions)
    ///   noteId       — Guid (required for editNote, deleteNote)
    ///   noteText     — string (required for addNote, editNote)
    ///   contactedDate — string yyyy-MM-dd (required for setContacted)
    ///   contactedNotes — string (optional for setContacted)
    ///
    /// Returns JSON:
    /// {
    ///   success: true,
    ///   notes: [ { noteId, noteText, authorName, createdLocal, updatedLocal, isEdited } ],
    ///   contacted: { isContacted: true, dateDisplay: "Mar 15, 2026" } | { isContacted: false }
    /// }
    /// </summary>
    public class NoteToggle : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetNoStore();

            try
            {
                // ── Auth ─────────────────────────────────────────────────
                var identity = context.User?.Identity;
                if (identity == null || !identity.IsAuthenticated)
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
                string subjectAddress = null;

                using (var db = new DCReyla())
                {
                    var profile = db.Profiles.FirstOrDefault(p => p.UserId == userId);
                    if (profile == null || !profile.OrganizationId.HasValue)
                    { Write(context, Error("Profile not found.")); return; }
                    orgId = profile.OrganizationId.Value;
                }

                // ── Parameters ───────────────────────────────────────────
                string action      = Param(context, "action");
                string prospectStr = Param(context, "prospectId");
                string noteIdStr   = Param(context, "noteId");
                string noteText    = Param(context, "noteText");
                string contDateStr = Param(context, "contactedDate");
                string contNotes   = Param(context, "contactedNotes");

                if (!Guid.TryParse(prospectStr, out Guid prospectId))
                { Write(context, Error("Invalid prospectId.")); return; }

                // Resolve subject address for activity metadata
                try
                {
                    using (var db = new DCReyla())
                    {
                        var prospect = db.Prospects.FirstOrDefault(p => p.ProspectId == prospectId);
                        if (prospect != null)
                        {
                            var prop = db.Properties.FirstOrDefault(p => p.PropertyId == prospect.PropertyId);
                            if (prop != null)
                                subjectAddress = (prop.StreetAddress ?? "") +
                                    (!string.IsNullOrEmpty(prop.CityNameRaw) ? ", " + prop.CityNameRaw : "");
                        }
                    }
                }
                catch { /* non-fatal */ }

                var svc = new ProspectNoteService();

                // ── Dispatch ─────────────────────────────────────────────
                switch (action)
                {
                    case "addNote":
                        if (string.IsNullOrWhiteSpace(noteText))
                        { Write(context, Error("Note text is required.")); return; }
                        var addResult = svc.AddNote(prospectId, userId, orgId, noteText, subjectAddress);
                        if (!addResult.Success)
                        { Write(context, Error(addResult.ErrorMessage)); return; }
                        break;

                    case "editNote":
                        if (!Guid.TryParse(noteIdStr, out Guid editNoteId))
                        { Write(context, Error("Invalid noteId.")); return; }
                        if (string.IsNullOrWhiteSpace(noteText))
                        { Write(context, Error("Note text is required.")); return; }
                        var editResult = svc.EditNote(editNoteId, userId, orgId, noteText, subjectAddress);
                        if (!editResult.Success)
                        { Write(context, Error(editResult.ErrorMessage)); return; }
                        break;

                    case "deleteNote":
                        if (!Guid.TryParse(noteIdStr, out Guid delNoteId))
                        { Write(context, Error("Invalid noteId.")); return; }
                        var delResult = svc.DeleteNote(delNoteId, userId, orgId, subjectAddress);
                        if (!delResult.Success)
                        { Write(context, Error(delResult.ErrorMessage)); return; }
                        break;

                    case "setContacted":
                        if (!DateTime.TryParse(contDateStr, out DateTime contDate))
                        { Write(context, Error("Invalid contacted date.")); return; }
                        svc.SetContactedDate(prospectId, orgId, userId, contDate, contNotes, subjectAddress);
                        break;

                    case "clearContacted":
                        svc.SetContactedDate(prospectId, orgId, userId, null, null, subjectAddress);
                        break;

                    default:
                        Write(context, Error("Unknown action: " + action));
                        return;
                }

                // ── Build response — reload notes + contacted state ───────
                var notes = svc.GetNotes(prospectId);

                var noteDtos = notes.Select(n => new
                {
                    noteId       = n.ProspectNoteId.ToString(),
                    noteText     = n.NoteText,
                    authorName   = n.AuthorName,
                    createdLocal = n.CreatedAtUtc.ToLocalTime().ToString("MMM d, yyyy h:mm tt"),
                    updatedLocal = n.UpdatedAtUtc.HasValue
                                   ? n.UpdatedAtUtc.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt")
                                   : (string)null,
                    isEdited     = n.UpdatedAtUtc.HasValue
                }).ToList();

                // Reload contacted state
                object contactedDto;
                using (var db = new DCReyla())
                {
                    var p = db.Prospects.FirstOrDefault(x => x.ProspectId == prospectId);
                    if (p?.ContactedDate != null)
                        contactedDto = new { isContacted = true,  dateDisplay = p.ContactedDate.Value.ToString("MMM d, yyyy"), contactedNotes = p.ContactedNotes ?? string.Empty };
                    else
                        contactedDto = new { isContacted = false, dateDisplay = (string)null, contactedNotes = string.Empty };
                }

                Write(context, JsonConvert.SerializeObject(new
                {
                    success   = true,
                    notes     = noteDtos,
                    contacted = contactedDto
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[NoteToggle] {0}", ex);
                Write(context, Error("Server error: " + ex.Message));
            }
        }

        private static void Write(HttpContext ctx, string json) => ctx.Response.Write(json);

        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new { success = false, errorMessage = msg });

        private static string Param(HttpContext ctx, string key) =>
            (ctx.Request.Form[key] ?? ctx.Request.QueryString[key])?.Trim();
    }
}
