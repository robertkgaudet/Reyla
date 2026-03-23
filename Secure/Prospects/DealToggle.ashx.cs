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
    /// JSON handler for Deal pipeline board operations.
    ///
    /// POST parameters:
    ///   action       — "createDeal" | "updateDeal" | "deleteDeal" | "moveStage" | "reorder"
    ///   dealId       — Guid (required for update/delete/move/reorder)
    ///   title        — string (createDeal, updateDeal)
    ///   stage        — string (createDeal, moveStage)
    ///   newOrder     — int (moveStage)
    ///   prospectId   — Guid (createDeal, optional)
    ///   propertyAddress — string (createDeal)
    ///   askingPrice  — decimal (createDeal, updateDeal)
    ///   offerPrice   — decimal (createDeal, updateDeal)
    ///   closeDate    — string yyyy-MM-dd (createDeal, updateDeal)
    ///   notes        — string (createDeal, updateDeal)
    ///   orderedIds   — JSON array of Guid strings (reorder)
    ///
    /// Returns: { success, board: { stage: [dealDto] }, errorMessage? }
    /// </summary>
    public class DealToggle : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetNoStore();

            try
            {
                // Auth
                var identity = context.User?.Identity;
                if (identity == null || !identity.IsAuthenticated)
                { context.Response.StatusCode = 401; Write(context, Error("Not authenticated.")); return; }

                var memberUser = Membership.GetUser(identity.Name);
                if (memberUser == null)
                { context.Response.StatusCode = 401; Write(context, Error("User not found.")); return; }

                var userId = (Guid)memberUser.ProviderUserKey;
                Guid orgId;

                using (var db = new DCReyla())
                {
                    var profile = db.Profiles.FirstOrDefault(p => p.UserId == userId);
                    if (profile == null || !profile.OrganizationId.HasValue)
                    { Write(context, Error("Profile not found.")); return; }
                    orgId = profile.OrganizationId.Value;
                }

                string action = Param(context, "action");
                var svc = new DealService();

                switch (action)
                {
                    case "createDeal":
                    {
                        var title    = Param(context, "title");
                        var stage    = Param(context, "stage");
                        Guid? prospectId = null;
                        string propAddr  = null;
                        decimal? asking = null, offer = null;
                        DateTime? closeDate = null;

                        if (Guid.TryParse(Param(context, "prospectId"), out Guid pid))
                            prospectId = pid;
                        if (decimal.TryParse(Param(context, "askingPrice"), out decimal ap))
                            asking = ap;
                        if (decimal.TryParse(Param(context, "offerPrice"), out decimal op))
                            offer = op;
                        if (DateTime.TryParse(Param(context, "closeDate"), out DateTime cd))
                            closeDate = cd;

                        // Resolve address and default title from prospect — never ask user to type address
                        if (prospectId.HasValue)
                        {
                            using (var db = new DCReyla())
                            {
                                var prospect = db.Prospects
                                    .FirstOrDefault(p => p.ProspectId == prospectId.Value && p.IsDeleted == false);
                                if (prospect == null)
                                { Write(context, Error("Prospect not found.")); return; }

                                var prop = db.Properties
                                    .FirstOrDefault(p => p.PropertyId == prospect.PropertyId);
                                if (prop != null)
                                {
                                    propAddr = (prop.StreetAddress ?? "") +
                                               (!string.IsNullOrWhiteSpace(prop.CityNameRaw)
                                                   ? ", " + prop.CityNameRaw : "");
                                    if (string.IsNullOrWhiteSpace(title))
                                        title = (prop.StreetAddress ?? "Unknown Address").ToUpper();
                                }
                            }
                        }

                        if (string.IsNullOrWhiteSpace(title))
                        { Write(context, Error("Please select a prospect.")); return; }

                        var result = svc.CreateDeal(orgId, userId, title, prospectId,
                            propAddr, asking, offer, closeDate, stage);
                        if (!result.Success) { Write(context, Error(result.ErrorMessage)); return; }
                        break;
                    }

                    case "updateDeal":
                    {
                        if (!Guid.TryParse(Param(context, "dealId"), out Guid dealId))
                        { Write(context, Error("Invalid dealId.")); return; }
                        var title  = Param(context, "title");
                        decimal? asking = null, offer = null;
                        DateTime? closeDate = null;

                        if (decimal.TryParse(Param(context, "askingPrice"), out decimal ap))
                            asking = ap;
                        if (decimal.TryParse(Param(context, "offerPrice"), out decimal op))
                            offer = op;
                        if (DateTime.TryParse(Param(context, "closeDate"), out DateTime cd))
                            closeDate = cd;

                        var result = svc.UpdateDeal(dealId, userId, orgId, title, asking, offer, closeDate);
                        if (!result.Success) { Write(context, Error(result.ErrorMessage)); return; }
                        break;
                    }

                    case "addDealNote":
                    {
                        if (!Guid.TryParse(Param(context, "dealId"), out Guid dealId))
                        { Write(context, Error("Invalid dealId.")); return; }
                        var noteText = Param(context, "noteText");
                        var result   = svc.AddDealNote(dealId, userId, orgId, noteText);
                        if (!result.Success) { Write(context, Error(result.ErrorMessage)); return; }
                        // Return notes for this deal alongside board
                        Write(context, BuildBoardWithNotesResponse(svc, orgId, dealId));
                        return;
                    }

                    case "editDealNote":
                    {
                        if (!Guid.TryParse(Param(context, "noteId"), out Guid noteId))
                        { Write(context, Error("Invalid noteId.")); return; }
                        var noteText = Param(context, "noteText");
                        var result   = svc.EditDealNote(noteId, userId, orgId, noteText);
                        if (!result.Success) { Write(context, Error(result.ErrorMessage)); return; }
                        Guid.TryParse(Param(context, "dealId"), out Guid dealIdForNotes);
                        Write(context, BuildBoardWithNotesResponse(svc, orgId, dealIdForNotes));
                        return;
                    }

                    case "deleteDealNote":
                    {
                        if (!Guid.TryParse(Param(context, "noteId"), out Guid noteId))
                        { Write(context, Error("Invalid noteId.")); return; }
                        var result = svc.DeleteDealNote(noteId, userId, orgId);
                        if (!result.Success) { Write(context, Error(result.ErrorMessage)); return; }
                        Guid.TryParse(Param(context, "dealId"), out Guid dealIdForNotes);
                        Write(context, BuildBoardWithNotesResponse(svc, orgId, dealIdForNotes));
                        return;
                    }

                    case "getDealNotes":
                    {
                        if (!Guid.TryParse(Param(context, "dealId"), out Guid dealId))
                        { Write(context, Error("Invalid dealId.")); return; }
                        Write(context, BuildBoardWithNotesResponse(svc, orgId, dealId));
                        return;
                    }

                    case "deleteDeal":
                    {
                        if (!Guid.TryParse(Param(context, "dealId"), out Guid dealId))
                        { Write(context, Error("Invalid dealId.")); return; }
                        var result = svc.DeleteDeal(dealId, userId, orgId);
                        if (!result.Success) { Write(context, Error(result.ErrorMessage)); return; }
                        break;
                    }

                    case "moveStage":
                    {
                        if (!Guid.TryParse(Param(context, "dealId"), out Guid dealId))
                        { Write(context, Error("Invalid dealId.")); return; }
                        var newStage = Param(context, "stage");
                        int.TryParse(Param(context, "newOrder"), out int newOrder);
                        var result = svc.MoveStage(dealId, userId, orgId, newStage, newOrder);
                        if (!result.Success) { Write(context, Error(result.ErrorMessage)); return; }
                        break;
                    }

                    case "reorder":
                    {
                        var stage      = Param(context, "stage");
                        var idsJson    = Param(context, "orderedIds");
                        var orderedIds = new List<Guid>();
                        try
                        {
                            var raw = JsonConvert.DeserializeObject<List<string>>(idsJson ?? "[]");
                            foreach (var s in raw)
                                if (Guid.TryParse(s, out Guid g)) orderedIds.Add(g);
                        }
                        catch { }
                        svc.ReorderStage(orgId, userId, stage, orderedIds);
                        break;
                    }

                    default:
                        Write(context, Error("Unknown action: " + action));
                        return;
                }

                // Return updated board
                Write(context, BuildBoardResponse(svc, orgId));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[DealToggle] {0}", ex);
                Write(context, Error("Server error: " + ex.Message));
            }
        }

        private string BuildBoardWithNotesResponse(DealService svc, Guid orgId, Guid dealId)
        {
            var notes = dealId != Guid.Empty ? svc.GetDealNotes(dealId) : new List<DealService.DealNoteRow>();
            var notesDtos = notes.Select(n => new
            {
                noteId       = n.DealNoteId.ToString(),
                dealId       = n.DealId.ToString(),
                noteText     = n.NoteText,
                authorName   = n.AuthorName,
                createdLocal = n.CreatedAtUtc.ToLocalTime().ToString("MMM d, yyyy h:mm tt"),
                updatedLocal = n.UpdatedAtUtc.HasValue
                               ? n.UpdatedAtUtc.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt")
                               : (string)null,
                isEdited     = n.UpdatedAtUtc.HasValue
            }).ToList();

            // Also include the full board so UI stays in sync
            var board = svc.GetBoard(orgId);
            var boardDto = BuildBoardDto(board);

            return JsonConvert.SerializeObject(new
            {
                success = true,
                board   = boardDto,
                notes   = notesDtos,
                dealId  = dealId.ToString()
            });
        }

        private Dictionary<string, object> BuildBoardDto(Dictionary<string, List<DealService.DealRow>> board)
        {
            var boardDto = new Dictionary<string, object>();
            foreach (var stage in DealService.Stages.Ordered)
            {
                boardDto[stage] = board[stage].Select(d => new
                {
                    dealId           = d.DealId.ToString(),
                    title            = d.Title,
                    propertyAddress  = d.PropertyAddress,
                    stage            = d.Stage,
                    stageOrder       = d.StageOrder,
                    askingPrice      = d.AskingPrice,
                    offerPrice       = d.OfferPrice,
                    closeDate        = d.CloseDate.HasValue ? d.CloseDate.Value.ToString("yyyy-MM-dd") : null,
                    closeDateDisplay = d.CloseDate.HasValue ? d.CloseDate.Value.ToString("MMM d, yyyy") : null,
                    agentName        = d.AgentName,
                    clip             = d.Clip,
                    prospectId       = d.ProspectId.HasValue ? d.ProspectId.Value.ToString() : null,
                    detailUrl        = d.Clip != null
                        ? "/Secure/Prospects/PropertyDetail.aspx?clip=" + HttpUtility.UrlEncode(d.Clip)
                        : null,
                    compsUrl         = d.ProspectId.HasValue
                        ? "/Secure/Prospects/Comps.aspx?prospectId=" + d.ProspectId.Value.ToString()
                        : null,
                    addedDate        = d.CreatedAtUtc.ToLocalTime().ToString("MMM d, yyyy"),
                    // Reyla Valuation
                    reylaEstimate    = d.ReylaEstimate,
                    reylaRangeLow    = d.ReylaRangeLow,
                    reylaRangeHigh   = d.ReylaRangeHigh,
                    confidenceLabel  = d.ConfidenceLabel,
                    confidenceColor  = d.ConfidenceColor
                }).ToList();
            }
            return boardDto;
        }

        private string BuildBoardResponse(DealService svc, Guid orgId)
        {
            var board = svc.GetBoard(orgId);
            return JsonConvert.SerializeObject(new { success = true, board = BuildBoardDto(board) });
        }

        private static void Write(HttpContext ctx, string json) => ctx.Response.Write(json);
        private static string Error(string msg) =>
            JsonConvert.SerializeObject(new { success = false, errorMessage = msg });
        private static string Param(HttpContext ctx, string key) =>
            (ctx.Request.Form[key] ?? ctx.Request.QueryString[key])?.Trim();
    }
}
