using System;

namespace Reyla.Services
{
    // ----------------------------------------------------------------
    // ActivityType — string constants.
    // Stored as-is in the DB column so rows are human-readable without
    // a lookup table join. Add new constants here as features expand.
    // ----------------------------------------------------------------

    public static class ActivityType
    {
        // Prospect lifecycle
        public const string ProspectCreated   = "ProspectCreated";
        public const string ProspectContacted = "ProspectContacted";
        public const string ProspectArchived  = "ProspectArchived";
		public const string ContactLinkedToProperty   = "ContactLinkedToProperty";
		public const string ContactUnlinkedFromProperty = "ContactUnlinkedFromProperty";

        // Comp management
        public const string CompAdded   = "CompAdded";
        public const string CompRemoved = "CompRemoved";
        public const string CompsCleared = "CompsCleared";

        // Notes (feature 2 — Owner Outreach Notes)
        public const string NoteAdded   = "NoteAdded";
        public const string NoteEdited  = "NoteEdited";
        public const string NoteDeleted = "NoteDeleted";

        // Valuation
        public const string ValuationViewed = "ValuationViewed";
        public const string ValuationSaved  = "ValuationSaved";

        // Deal pipeline (feature 3)
        public const string DealCreated      = "DealCreated";
        public const string DealStageChanged = "DealStageChanged";
        public const string DealClosed       = "DealClosed";
        public const string DealUpdated      = "DealUpdated";
        public const string DealNoteAdded    = "DealNoteAdded";
        public const string DealNoteEdited   = "DealNoteEdited";
        public const string DealNoteDeleted  = "DealNoteDeleted";

        // Owner contact
        public const string ContactInfoUpdated = "ContactInfoUpdated";

        // Contact management
        public const string ContactCreated              = "ContactCreated";
        public const string ContactUpdated              = "ContactUpdated";
        public const string ContactDeleted              = "ContactDeleted";
    }

    // ----------------------------------------------------------------
    // ActivityEntityType — EntityType column values
    // ----------------------------------------------------------------

    public static class ActivityEntityType
    {
        public const string Prospect = "Prospect";
        public const string Comp     = "Comp";
        public const string Note     = "Note";
        public const string Deal     = "Deal";
        public const string Property = "Property";
        public const string Contact  = "Contact";
    }

    // ----------------------------------------------------------------
    // ActivityLogEntry — DTO returned by ActivityService read methods.
    // Summary is populated by ActivityService.Summarize().
    // ----------------------------------------------------------------

    public class ActivityLogEntry
    {
        public Guid     ActivityLogId     { get; set; }
        public Guid     UserId            { get; set; }
        public Guid?    OrganizationId    { get; set; }
        public string   EntityType        { get; set; }
        public Guid     EntityId          { get; set; }
        public string   ActivityType      { get; set; }
        public string   Metadata          { get; set; }   // raw JSON
        public DateTime CreatedAtUtc      { get; set; }

        // Populated on read — not stored in DB
        public string   Summary           { get; set; }

        // Extracted from Metadata for direct use in the activity feed UI
        public string   Clip              { get; set; }   // CLIP for PropertyDetail link
        public string   LinkedProspectId  { get; set; }   // ProspectId string for Comps link
        public string   ContactId         { get; set; }   // ContactId for Contact.aspx link
    }
}
