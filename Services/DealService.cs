using System;
using System.Collections.Generic;
using System.Linq;
using Reyla.Services;

namespace Reyla.Services
{
    /// <summary>
    /// Manages the Deal pipeline board.
    /// All writes fire ActivityLog entries.
    /// </summary>
    public class DealService
    {
        // ── Stage constants ───────────────────────────────────────────────
        public static class Stages
        {
            public const string Lead         = "Lead";
            public const string Qualified    = "Qualified";
            public const string LOI          = "LOI";
            public const string UnderContract = "UnderContract";
            public const string Closed       = "Closed";
            public const string Dead         = "Dead";

            public static readonly string[] Ordered = {
                Lead, Qualified, LOI, UnderContract, Closed, Dead
            };

            public static string DisplayName(string stage)
            {
                switch (stage)
                {
                    case Lead:          return "Lead";
                    case Qualified:     return "Qualified";
                    case LOI:           return "LOI";
                    case UnderContract: return "Under Contract";
                    case Closed:        return "Closed";
                    case Dead:          return "Dead";
                    default:            return stage ?? "Unknown";
                }
            }
        }

        // ── Result / DTO ──────────────────────────────────────────────────

        public class DealResult
        {
            public bool   Success      { get; set; }
            public string ErrorMessage { get; set; }
            public Guid   DealId       { get; set; }
            public Guid   NoteId       { get; set; }

            public static DealResult Fail(string msg) =>
                new DealResult { Success = false, ErrorMessage = msg };
        }

        public class DealRow
        {
            public Guid      DealId           { get; set; }
            public Guid?     ProspectId       { get; set; }
            public string    Stage            { get; set; }
            public int       StageOrder       { get; set; }
            public string    Title            { get; set; }
            public string    PropertyAddress  { get; set; }
            public decimal?  AskingPrice      { get; set; }
            public decimal?  OfferPrice       { get; set; }
            public DateTime? CloseDate        { get; set; }
            public string    AgentName        { get; set; }
            public DateTime  CreatedAtUtc     { get; set; }
            public string    Clip             { get; set; }
            // Reyla Valuation
            public decimal?  ReylaEstimate    { get; set; }
            public decimal?  ReylaRangeLow    { get; set; }
            public decimal?  ReylaRangeHigh   { get; set; }
            public string    ConfidenceLabel  { get; set; }
            public string    ConfidenceColor  { get; set; }
        }

        // ── GetBoard — all active deals for org, grouped by stage ─────────

        public Dictionary<string, List<DealRow>> GetBoard(Guid organizationId)
        {
            var board = new Dictionary<string, List<DealRow>>();
            foreach (var s in Stages.Ordered)
                board[s] = new List<DealRow>();

            var valSvc = new ReylaValuationService();

            using (var db = new DCReyla())
            {
                var deals = db.Deals
                    .Where(d => d.OrganizationId == organizationId && d.IsDeleted == false)
                    .OrderBy(d => d.StageOrder)
                    .ThenBy(d => d.CreatedAtUtc)
                    .ToList();

                foreach (var d in deals)
                {
                    string agentName = string.Empty;
                    string clip      = null;
                    try
                    {
                        var profile = db.Profiles.FirstOrDefault(p => p.UserId == d.UserId);
                        if (profile != null)
                            agentName = ((profile.FirstName ?? "") + " " + (profile.LastName ?? "")).Trim();

                        if (d.ProspectId.HasValue)
                        {
                            var prospect = db.Prospects.FirstOrDefault(p => p.ProspectId == d.ProspectId.Value);
                            if (prospect != null)
                            {
                                var prop = db.Properties.FirstOrDefault(p => p.PropertyId == prospect.PropertyId);
                                clip = prop?.Clip;
                            }
                        }
                    }
                    catch { /* non-fatal */ }

                    // Reyla Valuation — best-effort, non-fatal
                    decimal?  reylaEst   = null;
                    decimal?  reylaLow   = null;
                    decimal?  reylaHigh  = null;
                    string    confLabel  = null;
                    string    confColor  = null;
                    if (d.ProspectId.HasValue)
                    {
                        try
                        {
                            var val = valSvc.Calculate(d.ProspectId.Value, organizationId);
                            if (val.Success)
                            {
                                reylaEst  = val.PointEstimate;
                                reylaLow  = val.RangeLow;
                                reylaHigh = val.RangeHigh;
                                confLabel = val.ConfidenceLabel;
                                confColor = val.ConfidenceColor;
                            }
                        }
                        catch { /* non-fatal */ }
                    }

                    var stage = Stages.Ordered.Contains(d.Stage) ? d.Stage : Stages.Lead;
                    board[stage].Add(new DealRow
                    {
                        DealId          = d.DealId,
                        ProspectId      = d.ProspectId,
                        Stage           = d.Stage,
                        StageOrder      = d.StageOrder,
                        Title           = d.Title,
                        PropertyAddress = d.PropertyAddress,
                        AskingPrice     = d.AskingPrice,
                        OfferPrice      = d.OfferPrice,
                        CloseDate       = d.CloseDate,
                        AgentName       = agentName,
                        CreatedAtUtc    = d.CreatedAtUtc,
                        Clip            = clip,
                        ReylaEstimate   = reylaEst,
                        ReylaRangeLow   = reylaLow,
                        ReylaRangeHigh  = reylaHigh,
                        ConfidenceLabel = confLabel,
                        ConfidenceColor = confColor
                    });
                }
            }

            return board;
        }

        // ── CreateDeal ────────────────────────────────────────────────────

        public DealResult CreateDeal(
            Guid    organizationId,
            Guid    userId,
            string  title,
            Guid?   prospectId      = null,
            string  propertyAddress = null,
            decimal? askingPrice    = null,
            decimal? offerPrice     = null,
            DateTime? closeDate     = null,
            string  stage           = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                return DealResult.Fail("Title is required.");

            stage = stage ?? Stages.Lead;
            if (!Stages.Ordered.Contains(stage))
                stage = Stages.Lead;

            try
            {
                Guid dealId;
                using (var db = new DCReyla())
                {
                    // Place at bottom of stage
                    int maxOrder = db.Deals
                        .Where(d => d.OrganizationId == organizationId &&
                                    d.Stage          == stage          &&
                                    d.IsDeleted      == false)
                        .Select(d => (int?)d.StageOrder)
                        .Max() ?? -1;

                    var deal = new Deal
                    {
                        DealId          = Guid.NewGuid(),
                        OrganizationId  = organizationId,
                        UserId          = userId,
                        ProspectId      = prospectId,
                        Stage           = stage,
                        StageOrder      = maxOrder + 1,
                        Title           = title.Trim().Substring(0, Math.Min(title.Trim().Length, 200)),
                        PropertyAddress = propertyAddress?.Trim(),
                        AskingPrice     = askingPrice,
                        OfferPrice      = offerPrice,
                        CloseDate       = closeDate,
                        IsDeleted       = false,
                        CreatedAtUtc    = DateTime.UtcNow,
                        CreatedBy       = userId
                    };
                    db.Deals.InsertOnSubmit(deal);
                    db.SubmitChanges();
                    dealId = deal.DealId;
                }

                new ActivityService().Track(
                    ActivityType.DealCreated,
                    ActivityEntityType.Deal,
                    dealId,
                    userId,
                    organizationId,
                    new { subjectAddress = propertyAddress ?? title, stage = stage });

                return new DealResult { Success = true, DealId = dealId };
            }
            catch (Exception ex)
            {
                return DealResult.Fail("Failed to create deal: " + ex.Message);
            }
        }

        // ── MoveStage — drag drop or manual stage change ──────────────────

        public DealResult MoveStage(
            Guid   dealId,
            Guid   userId,
            Guid   organizationId,
            string newStage,
            int    newOrder = 0)
        {
            if (!Stages.Ordered.Contains(newStage))
                return DealResult.Fail("Invalid stage.");

            try
            {
                string fromStage;
                string dealTitle;
                using (var db = new DCReyla())
                {
                    var deal = db.Deals.FirstOrDefault(d =>
                        d.DealId         == dealId         &&
                        d.OrganizationId == organizationId &&
                        d.IsDeleted      == false);

                    if (deal == null) return DealResult.Fail("Deal not found.");

                    fromStage = deal.Stage;
                    dealTitle = deal.Title;

                    deal.Stage       = newStage;
                    deal.StageOrder  = newOrder;
                    deal.UpdatedAtUtc = DateTime.UtcNow;
                    deal.UpdatedBy   = userId;
                    db.SubmitChanges();
                }

                var actType = newStage == Stages.Closed
                    ? ActivityType.DealClosed
                    : ActivityType.DealStageChanged;

                new ActivityService().Track(
                    actType,
                    ActivityEntityType.Deal,
                    dealId,
                    userId,
                    organizationId,
                    new { subjectAddress = dealTitle, fromStage = fromStage, toStage = newStage });

                return new DealResult { Success = true, DealId = dealId };
            }
            catch (Exception ex)
            {
                return DealResult.Fail("Failed to move deal: " + ex.Message);
            }
        }

        // ── UpdateDeal ────────────────────────────────────────────────────

        public DealResult UpdateDeal(
            Guid     dealId,
            Guid     userId,
            Guid     organizationId,
            string   title,
            decimal? askingPrice  = null,
            decimal? offerPrice   = null,
            DateTime? closeDate   = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                return DealResult.Fail("Title is required.");

            try
            {
                string dealTitle;
                using (var db = new DCReyla())
                {
                    var deal = db.Deals.FirstOrDefault(d =>
                        d.DealId         == dealId         &&
                        d.OrganizationId == organizationId &&
                        d.IsDeleted      == false);

                    if (deal == null) return DealResult.Fail("Deal not found.");

                    deal.Title        = title.Trim().Substring(0, Math.Min(title.Trim().Length, 200));
                    deal.AskingPrice  = askingPrice;
                    deal.OfferPrice   = offerPrice;
                    deal.CloseDate    = closeDate;
                    deal.UpdatedAtUtc = DateTime.UtcNow;
                    deal.UpdatedBy    = userId;
                    db.SubmitChanges();
                    dealTitle = deal.Title;
                }

                new ActivityService().Track(
                    ActivityType.DealUpdated,
                    ActivityEntityType.Deal,
                    dealId,
                    userId,
                    organizationId,
                    new { subjectAddress = dealTitle });

                return new DealResult { Success = true, DealId = dealId };
            }
            catch (Exception ex)
            {
                return DealResult.Fail("Failed to update deal: " + ex.Message);
            }
        }

        // ── Deal Notes — transactional ────────────────────────────────────

        public class DealNoteRow
        {
            public Guid      DealNoteId   { get; set; }
            public Guid      DealId       { get; set; }
            public Guid      UserId       { get; set; }
            public string    NoteText     { get; set; }
            public string    AuthorName   { get; set; }
            public DateTime  CreatedAtUtc { get; set; }
            public DateTime? UpdatedAtUtc { get; set; }
        }

        public List<DealNoteRow> GetDealNotes(Guid dealId)
        {
            using (var db = new DCReyla())
            {
                var notes = db.DealNotes
                    .Where(n => n.DealId == dealId && n.IsDeleted == false)
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .ToList();

                return notes.Select(n =>
                {
                    string author = string.Empty;
                    try
                    {
                        var p = db.Profiles.FirstOrDefault(x => x.UserId == n.UserId);
                        if (p != null) author = ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim();
                    }
                    catch { }

                    return new DealNoteRow
                    {
                        DealNoteId   = n.DealNoteId,
                        DealId       = n.DealId,
                        UserId       = n.UserId,
                        NoteText     = n.NoteText,
                        AuthorName   = author,
                        CreatedAtUtc = n.CreatedAtUtc,
                        UpdatedAtUtc = n.UpdatedAtUtc
                    };
                }).ToList();
            }
        }

        public DealResult AddDealNote(Guid dealId, Guid userId, Guid organizationId, string noteText)
        {
            if (string.IsNullOrWhiteSpace(noteText))
                return DealResult.Fail("Note text is required.");

            noteText = noteText.Trim();
            if (noteText.Length > 2000) noteText = noteText.Substring(0, 2000);

            try
            {
                string dealTitle = null;
                Guid noteId;
                using (var db = new DCReyla())
                {
                    var deal = db.Deals.FirstOrDefault(d => d.DealId == dealId && d.IsDeleted == false);
                    dealTitle = deal?.Title;

                    var note = new DealNote
                    {
                        DealNoteId   = Guid.NewGuid(),
                        DealId       = dealId,
                        UserId       = userId,
                        NoteText     = noteText,
                        IsDeleted    = false,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    db.DealNotes.InsertOnSubmit(note);
                    db.SubmitChanges();
                    noteId = note.DealNoteId;
                }

                new ActivityService().Track(
                    ActivityType.DealNoteAdded,
                    ActivityEntityType.Deal,
                    dealId,
                    userId,
                    organizationId,
                    new { subjectAddress = dealTitle });

                return new DealResult { Success = true, NoteId = noteId };
            }
            catch (Exception ex) { return DealResult.Fail("Failed to add note: " + ex.Message); }
        }

        public DealResult EditDealNote(Guid noteId, Guid userId, Guid organizationId, string noteText)
        {
            if (string.IsNullOrWhiteSpace(noteText))
                return DealResult.Fail("Note text is required.");

            noteText = noteText.Trim();
            if (noteText.Length > 2000) noteText = noteText.Substring(0, 2000);

            try
            {
                Guid dealId;
                string dealTitle = null;
                using (var db = new DCReyla())
                {
                    var note = db.DealNotes.FirstOrDefault(n => n.DealNoteId == noteId && n.IsDeleted == false);
                    if (note == null) return DealResult.Fail("Note not found.");
                    dealId       = note.DealId;
                    note.NoteText     = noteText;
                    note.UpdatedAtUtc = DateTime.UtcNow;
                    db.SubmitChanges();

                    var deal = db.Deals.FirstOrDefault(d => d.DealId == dealId);
                    dealTitle = deal?.Title;
                }

                new ActivityService().Track(
                    ActivityType.DealNoteEdited,
                    ActivityEntityType.Deal,
                    dealId,
                    userId,
                    organizationId,
                    new { subjectAddress = dealTitle });

                return new DealResult { Success = true, NoteId = noteId };
            }
            catch (Exception ex) { return DealResult.Fail("Failed to edit note: " + ex.Message); }
        }

        public DealResult DeleteDealNote(Guid noteId, Guid userId, Guid organizationId)
        {
            try
            {
                Guid dealId;
                string dealTitle = null;
                using (var db = new DCReyla())
                {
                    var note = db.DealNotes.FirstOrDefault(n => n.DealNoteId == noteId && n.IsDeleted == false);
                    if (note == null) return DealResult.Fail("Note not found.");
                    dealId         = note.DealId;
                    note.IsDeleted   = true;
                    note.UpdatedAtUtc = DateTime.UtcNow;
                    db.SubmitChanges();

                    var deal = db.Deals.FirstOrDefault(d => d.DealId == dealId);
                    dealTitle = deal?.Title;
                }

                new ActivityService().Track(
                    ActivityType.DealNoteDeleted,
                    ActivityEntityType.Deal,
                    dealId,
                    userId,
                    organizationId,
                    new { subjectAddress = dealTitle });

                return new DealResult { Success = true };
            }
            catch (Exception ex) { return DealResult.Fail("Failed to delete note: " + ex.Message); }
        }

        // ── DeleteDeal — soft delete ──────────────────────────────────────

        public DealResult DeleteDeal(Guid dealId, Guid userId, Guid organizationId)
        {
            try
            {
                using (var db = new DCReyla())
                {
                    var deal = db.Deals.FirstOrDefault(d =>
                        d.DealId         == dealId         &&
                        d.OrganizationId == organizationId &&
                        d.IsDeleted      == false);

                    if (deal == null) return DealResult.Fail("Deal not found.");

                    deal.IsDeleted    = true;
                    deal.UpdatedAtUtc = DateTime.UtcNow;
                    deal.UpdatedBy    = userId;
                    db.SubmitChanges();
                }

                return new DealResult { Success = true, DealId = dealId };
            }
            catch (Exception ex)
            {
                return DealResult.Fail("Failed to delete deal: " + ex.Message);
            }
        }

        // ── ReorderStage — persist card order after drag ──────────────────

        public bool ReorderStage(Guid organizationId, Guid userId, string stage, List<Guid> orderedIds)
        {
            try
            {
                using (var db = new DCReyla())
                {
                    var deals = db.Deals
                        .Where(d => d.OrganizationId == organizationId &&
                                    d.Stage          == stage          &&
                                    d.IsDeleted      == false)
                        .ToList();

                    for (int i = 0; i < orderedIds.Count; i++)
                    {
                        var deal = deals.FirstOrDefault(d => d.DealId == orderedIds[i]);
                        if (deal != null)
                        {
                            deal.StageOrder  = i;
                            deal.UpdatedAtUtc = DateTime.UtcNow;
                        }
                    }
                    db.SubmitChanges();
                }
                return true;
            }
            catch { return false; }
        }
    }
}
