using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace Reyla.Services
{
    /// <summary>
    /// Lightweight audit/timeline service for Reyla.
    ///
    /// Track() is the only write method. It is intentionally fire-and-forget:
    /// all exceptions are swallowed so a logging failure never disrupts a real
    /// user-facing operation.
    ///
    /// Read methods power three surfaces:
    ///   - Prospect timeline  (GetEntityActivity)
    ///   - User activity feed (GetUserActivity)
    ///   - Daily digest       (GetDigestActivity)
    ///
    /// Summarize() produces plain-English descriptions for any entry using
    /// self-contained metadata — no secondary DB lookups required.
    /// </summary>
    public class ActivityService
    {
        // ----------------------------------------------------------------
        // Track — write a single log entry. Never throws.
        // ----------------------------------------------------------------

        public void Track(
            string  activityType,
            string  entityType,
            Guid    entityId,
            Guid    userId,
            Guid?   organizationId,
            object  metadata = null)
        {
            try
            {
                string metaJson = null;
                if (metadata != null)
                {
                    var serializer = new JavaScriptSerializer();
                    metaJson = serializer.Serialize(metadata);
                }

                using (var db = new DCReyla())
                {
                    var entry = new ActivityLog
                    {
                        ActivityLogId  = Guid.NewGuid(),
                        UserId         = userId,
                        OrganizationId = organizationId,
                        EntityType     = entityType,
                        EntityId       = entityId,
                        ActivityType   = activityType,
                        Metadata       = metaJson,
                        CreatedAtUtc   = DateTime.UtcNow
                    };

                    db.ActivityLogs.InsertOnSubmit(entry);
                    db.SubmitChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "[ActivityService.Track] Failed to write {0}/{1}/{2}: {3}",
                    activityType, entityType, entityId, ex.Message);
            }
        }

        // ----------------------------------------------------------------
        // GetEntityActivity — timeline for a single entity (e.g. one prospect)
        // ----------------------------------------------------------------

        public List<ActivityLogEntry> GetEntityActivity(
            string entityType,
            Guid   entityId,
            int    limit = 20)
        {
            using (var db = new DCReyla())
            {
                var rows = db.ActivityLogs
                    .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .Take(limit)
                    .ToList();

                return rows.Select(r => Hydrate(r)).ToList();
            }
        }

        // ----------------------------------------------------------------
        // GetUserActivity — personal feed for dashboard / sidebar
        // ----------------------------------------------------------------

        public List<ActivityLogEntry> GetUserActivity(Guid userId, int limit = 50)
        {
            using (var db = new DCReyla())
            {
                var rows = db.ActivityLogs
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .Take(limit)
                    .ToList();

                return rows.Select(r => Hydrate(r)).ToList();
            }
        }

        // ----------------------------------------------------------------
        // GetDigestActivity — all org activity since a given UTC timestamp.
        // Used by Daily Digest (feature 4) and the org-wide feed.
        // ----------------------------------------------------------------

        public List<ActivityLogEntry> GetDigestActivity(Guid organizationId, DateTime sinceUtc)
        {
            using (var db = new DCReyla())
            {
                var rows = db.ActivityLogs
                    .Where(a =>
                        a.OrganizationId == organizationId &&
                        a.CreatedAtUtc   >= sinceUtc)
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .ToList();

                return rows.Select(r => Hydrate(r)).ToList();
            }
        }

        // ----------------------------------------------------------------
        // Summarize — plain-English description of a single entry.
        // Reads from Metadata JSON so no secondary DB lookups are needed.
        // ----------------------------------------------------------------

        public string Summarize(ActivityLogEntry entry)
        {
            if (entry == null) return string.Empty;

            // Parse metadata if present
            var meta = ParseMeta(entry.Metadata);

            string subject = GetMeta(meta, "subjectAddress") ?? "a property";
            string comp    = GetMeta(meta, "compAddress");
            string stage   = GetMeta(meta, "stage");
            string from    = GetMeta(meta, "fromStage");
            string to      = GetMeta(meta, "toStage");

            switch (entry.ActivityType)
            {
                // Prospect
                case ActivityType.ProspectCreated:
                    return string.Format("Added {0} as a prospect", subject);

                case ActivityType.ProspectContacted:
                    return string.Format("Marked {0} as contacted", subject);

                case ActivityType.ProspectArchived:
                    return string.Format("Archived {0}", subject);

                // Comps
                case ActivityType.CompAdded:
                    return comp != null
                        ? string.Format("Added {0} as a comp to {1}", comp, subject)
                        : string.Format("Added a comp to {0}", subject);

                case ActivityType.CompRemoved:
                    return comp != null
                        ? string.Format("Removed {0} as a comp from {1}", comp, subject)
                        : string.Format("Removed a comp from {0}", subject);

                case ActivityType.CompsCleared:
                    return string.Format("Cleared all comps from {0}", subject);

                // Notes
                case ActivityType.NoteAdded:
                    return string.Format("Added a note to {0}", subject);

                case ActivityType.NoteEdited:
                    return string.Format("Edited a note on {0}", subject);

                case ActivityType.NoteDeleted:
                    return string.Format("Deleted a note from {0}", subject);

                // Valuation
                case ActivityType.ValuationViewed:
                    return string.Format("Viewed valuation for {0}", subject);

                case ActivityType.ValuationSaved:
                    return string.Format("Saved valuation for {0}", subject);

                // Deal pipeline
                case ActivityType.DealCreated:
                    return string.Format("Created a deal for {0}", subject);

                case ActivityType.DealStageChanged:
                    if (from != null && to != null)
                        return string.Format("Moved {0} from {1} to {2}", subject, from, to);
                    if (to != null)
                        return string.Format("Moved {0} to {1}", subject, to);
                    return string.Format("Updated deal stage for {0}", subject);

                case ActivityType.DealClosed:
                    return string.Format("Closed deal for {0}", subject);

                // Owner contact (legacy)
                case ActivityType.ContactInfoUpdated:
                    return string.Format("Updated owner contact info for {0}", subject);

                // Contact management
                case ActivityType.ContactCreated:
                    return string.Format("Created contact {0}", GetMeta(meta, "contactName") ?? "a contact");

                case ActivityType.ContactUpdated:
                    return string.Format("Updated contact {0}", GetMeta(meta, "contactName") ?? "a contact");

                case ActivityType.ContactDeleted:
                    return string.Format("Deleted contact {0}", GetMeta(meta, "contactName") ?? "a contact");

                case ActivityType.ContactLinkedToProperty:
                    return string.Format("Linked {0} to a property", GetMeta(meta, "contactName") ?? "a contact");

                case ActivityType.ContactUnlinkedFromProperty:
                    return string.Format("Removed {0} from a property", GetMeta(meta, "contactName") ?? "a contact");

                default:
                    return string.Format("{0} on {1}", entry.ActivityType, subject);
            }
        }

        // ----------------------------------------------------------------
        // Private helpers
        // ----------------------------------------------------------------

        private ActivityLogEntry Hydrate(ActivityLog row)
        {
            var entry = new ActivityLogEntry
            {
                ActivityLogId  = row.ActivityLogId,
                UserId         = row.UserId,
                OrganizationId = row.OrganizationId,
                EntityType     = row.EntityType,
                EntityId       = row.EntityId,
                ActivityType   = row.ActivityType,
                Metadata       = row.Metadata,
                CreatedAtUtc   = row.CreatedAtUtc
            };

            entry.Summary          = Summarize(entry);

            // Extract link fields from metadata so the feed UI doesn't need to re-parse JSON
            var metaDict = ParseMeta(entry.Metadata);
            entry.Clip             = GetMeta(metaDict, "clip");
            entry.LinkedProspectId = GetMeta(metaDict, "prospectId");
            entry.ContactId        = GetMeta(metaDict, "contactId");

            return entry;
        }

        private System.Collections.Generic.Dictionary<string, object> ParseMeta(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var serializer = new JavaScriptSerializer();
                return serializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
            }
            catch
            {
                return null;
            }
        }

        private string GetMeta(System.Collections.Generic.Dictionary<string, object> meta, string key)
        {
            if (meta == null || !meta.ContainsKey(key)) return null;
            return meta[key]?.ToString();
        }
    }
}
