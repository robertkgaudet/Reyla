using System;
using System.Collections.Generic;
using System.Linq;
using Reyla.Services;

namespace Reyla.Services
{
    /// <summary>
    /// Manages outreach notes on a prospect (ProspectNote table).
    /// Also handles setting/clearing the ContactedDate on the Prospect record.
    /// All writes fire ActivityLog entries.
    /// </summary>
    public class ProspectNoteService
    {
        // ----------------------------------------------------------------
        // Result / DTO
        // ----------------------------------------------------------------

        public class NoteResult
        {
            public bool   Success      { get; set; }
            public string ErrorMessage { get; set; }
            public Guid   NoteId       { get; set; }

            public static NoteResult Fail(string msg) =>
                new NoteResult { Success = false, ErrorMessage = msg };
        }

        public class ProspectNoteRow
        {
            public Guid      ProspectNoteId { get; set; }
            public Guid      ProspectId     { get; set; }
            public Guid      UserId         { get; set; }
            public string    NoteText       { get; set; }
            public DateTime  CreatedAtUtc   { get; set; }
            public DateTime? UpdatedAtUtc   { get; set; }
            public string    AuthorName     { get; set; }  // display name
        }

        // ----------------------------------------------------------------
        // GetNotes — all active notes for a prospect, newest first
        // ----------------------------------------------------------------

        public List<ProspectNoteRow> GetNotes(Guid prospectId)
        {
            using (var db = new DCReyla())
            {
                var notes = db.ProspectNotes
                    .Where(n => n.ProspectId == prospectId && n.IsDeleted == false)
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .ToList();

                var rows = new List<ProspectNoteRow>();
                foreach (var n in notes)
                {
                    string authorName = string.Empty;
                    try
                    {
                        var profile = db.Profiles.FirstOrDefault(p => p.UserId == n.UserId);
                        if (profile != null)
                            authorName = (profile.FirstName + " " + profile.LastName).Trim();
                        if (string.IsNullOrWhiteSpace(authorName))
                        {
                            var mem = System.Web.Security.Membership.GetUser(n.UserId);
                            if (mem != null) authorName = mem.Email ?? string.Empty;
                        }
                    }
                    catch { /* non-fatal */ }

                    rows.Add(new ProspectNoteRow
                    {
                        ProspectNoteId = n.ProspectNoteId,
                        ProspectId     = n.ProspectId,
                        UserId         = n.UserId,
                        NoteText       = n.NoteText,
                        CreatedAtUtc   = n.CreatedAtUtc,
                        UpdatedAtUtc   = n.UpdatedAtUtc,
                        AuthorName     = authorName
                    });
                }
                return rows;
            }
        }

        // ----------------------------------------------------------------
        // AddNote
        // ----------------------------------------------------------------

        public NoteResult AddNote(
            Guid   prospectId,
            Guid   userId,
            Guid   organizationId,
            string noteText,
            string subjectAddress = null)
        {
            if (string.IsNullOrWhiteSpace(noteText))
                return NoteResult.Fail("Note text is required.");

            noteText = noteText.Trim();
            if (noteText.Length > 2000)
                noteText = noteText.Substring(0, 2000);

            try
            {
                Guid noteId;
                using (var db = new DCReyla())
                {
                    var note = new ProspectNote
                    {
                        ProspectNoteId = Guid.NewGuid(),
                        ProspectId     = prospectId,
                        UserId         = userId,
                        NoteText       = noteText,
                        IsDeleted      = false,
                        CreatedAtUtc   = DateTime.UtcNow
                    };
                    db.ProspectNotes.InsertOnSubmit(note);
                    db.SubmitChanges();
                    noteId = note.ProspectNoteId;
                }

                new ActivityService().Track(
                    ActivityType.NoteAdded,
                    ActivityEntityType.Prospect,
                    prospectId,
                    userId,
                    organizationId,
                    new { subjectAddress = subjectAddress, prospectId = prospectId.ToString(), clip = GetClipForProspect(prospectId) });

                return new NoteResult { Success = true, NoteId = noteId };
            }
            catch (Exception ex)
            {
                return NoteResult.Fail("Failed to save note: " + ex.Message);
            }
        }

        // ----------------------------------------------------------------
        // EditNote
        // ----------------------------------------------------------------

        public NoteResult EditNote(
            Guid   noteId,
            Guid   userId,
            Guid   organizationId,
            string noteText,
            string subjectAddress = null)
        {
            if (string.IsNullOrWhiteSpace(noteText))
                return NoteResult.Fail("Note text is required.");

            noteText = noteText.Trim();
            if (noteText.Length > 2000)
                noteText = noteText.Substring(0, 2000);

            try
            {
                Guid prospectId;
                using (var db = new DCReyla())
                {
                    var note = db.ProspectNotes
                        .FirstOrDefault(n => n.ProspectNoteId == noteId && n.IsDeleted == false);

                    if (note == null)
                        return NoteResult.Fail("Note not found.");

                    prospectId       = note.ProspectId;
                    note.NoteText    = noteText;
                    note.UpdatedAtUtc = DateTime.UtcNow;
                    db.SubmitChanges();
                }

                new ActivityService().Track(
                    ActivityType.NoteEdited,
                    ActivityEntityType.Prospect,
                    prospectId,
                    userId,
                    organizationId,
                    new { subjectAddress = subjectAddress, prospectId = prospectId.ToString(), clip = GetClipForProspect(prospectId) });

                return new NoteResult { Success = true, NoteId = noteId };
            }
            catch (Exception ex)
            {
                return NoteResult.Fail("Failed to update note: " + ex.Message);
            }
        }

        // ----------------------------------------------------------------
        // DeleteNote — soft delete
        // ----------------------------------------------------------------

        public NoteResult DeleteNote(
            Guid   noteId,
            Guid   userId,
            Guid   organizationId,
            string subjectAddress = null)
        {
            try
            {
                Guid prospectId;
                using (var db = new DCReyla())
                {
                    var note = db.ProspectNotes
                        .FirstOrDefault(n => n.ProspectNoteId == noteId && n.IsDeleted == false);

                    if (note == null)
                        return NoteResult.Fail("Note not found.");

                    prospectId       = note.ProspectId;
                    note.IsDeleted   = true;
                    note.UpdatedAtUtc = DateTime.UtcNow;
                    db.SubmitChanges();
                }

                new ActivityService().Track(
                    ActivityType.NoteDeleted,
                    ActivityEntityType.Prospect,
                    prospectId,
                    userId,
                    organizationId,
                    new { subjectAddress = subjectAddress, prospectId = prospectId.ToString(), clip = GetClipForProspect(prospectId) });

                return new NoteResult { Success = true, NoteId = noteId };
            }
            catch (Exception ex)
            {
                return NoteResult.Fail("Failed to delete note: " + ex.Message);
            }
        }

        // ----------------------------------------------------------------
        // SetContactedDate — marks the prospect as contacted with a date
        // Pass null to clear the contacted date
        // ----------------------------------------------------------------

        public bool SetContactedDate(
            Guid      prospectId,
            Guid      organizationId,
            Guid      userId,
            DateTime? contactedDate,
            string    contactedNotes  = null,
            string    subjectAddress  = null)
        {
            try
            {
                using (var db = new DCReyla())
                {
                    var prospect = db.Prospects
                        .FirstOrDefault(p =>
                            p.ProspectId     == prospectId &&
                            p.OrganizationId == organizationId &&
                            p.IsDeleted      == false);

                    if (prospect == null) return false;

                    prospect.ContactedDate  = contactedDate.HasValue
                        ? (DateTime?)contactedDate.Value.Date
                        : null;
                    prospect.ContactedNotes = contactedNotes?.Trim();
                    prospect.UpdatedAtUtc   = DateTime.UtcNow;
                    prospect.UpdatedBy      = userId;
                    db.SubmitChanges();
                }

                if (contactedDate.HasValue)
                {
                    new ActivityService().Track(
                        ActivityType.ProspectContacted,
                        ActivityEntityType.Prospect,
                        prospectId,
                        userId,
                        organizationId,
                        new { subjectAddress = subjectAddress, prospectId = prospectId.ToString(), clip = GetClipForProspect(prospectId) });
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "[ProspectNoteService.SetContactedDate] {0}", ex.Message);
                return false;
            }
        }

        // ----------------------------------------------------------------
        // Private helper — look up CLIP for a prospect so activity feed
        // can build a direct link to PropertyDetail.aspx?clip=...
        // ----------------------------------------------------------------

        private string GetClipForProspect(Guid prospectId)
        {
            try
            {
                using (var db = new DCReyla())
                {
                    var prospect = db.Prospects
                        .FirstOrDefault(p => p.ProspectId == prospectId);
                    if (prospect == null) return null;

                    var property = db.Properties
                        .FirstOrDefault(p => p.PropertyId == prospect.PropertyId);
                    return property?.Clip;
                }
            }
            catch { return null; }
        }
    }
}
