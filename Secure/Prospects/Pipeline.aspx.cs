using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using Newtonsoft.Json;
using Reyla.Services;

namespace Reyla.Secure.Prospects
{
    public partial class Pipeline : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                BindBoard();
        }

        private void BindBoard()
        {
            try
            {
                var memberUser = Membership.GetUser();
                if (memberUser == null) return;

                var userId = (Guid)memberUser.ProviderUserKey;
                Guid orgId = Guid.Empty;

                using (var db = new DCReyla())
                {
                    var profile = db.Profiles.FirstOrDefault(p => p.UserId == userId);
                    if (profile?.OrganizationId != null)
                        orgId = profile.OrganizationId.Value;

                    // ── Board ─────────────────────────────────────────
                    var svc   = new DealService();
                    var board = svc.GetBoard(orgId);

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

                    hdnBoardJson.Value = JsonConvert.SerializeObject(boardDto);

                    // ── Prospect picker list ──────────────────────────
                    // All active prospects for this org, not already in a deal
                    var existingProspectIds = db.Deals
                        .Where(d => d.OrganizationId == orgId && d.IsDeleted == false && d.ProspectId != null)
                        .Select(d => d.ProspectId)
                        .ToList();

                    var prospects = (
                        from pr in db.Prospects
                        join prop in db.Properties on pr.PropertyId equals prop.PropertyId
                        where pr.OrganizationId == orgId
                           && pr.IsDeleted      == false
                        orderby prop.StreetAddress
                        select new
                        {
                            pr.ProspectId,
                            prop.Clip,
                            prop.StreetAddress,
                            prop.CityNameRaw,
                            prop.ZipCodeRaw
                        }).ToList();

                    var prospectDtos = prospects.Select(p => new
                    {
                        prospectId = p.ProspectId.ToString(),
                        clip       = p.Clip,
                        address    = (p.StreetAddress ?? "").ToUpper(),
                        cityLine   = string.Join(", ", new[] { p.CityNameRaw, p.ZipCodeRaw }
                                         .Where(s => !string.IsNullOrWhiteSpace(s))),
                        display    = (p.StreetAddress ?? "Unknown") +
                                     (!string.IsNullOrWhiteSpace(p.CityNameRaw) ? ", " + p.CityNameRaw : ""),
                        inDeal     = existingProspectIds.Contains(p.ProspectId)
                    }).ToList();

                    hdnProspectsJson.Value = JsonConvert.SerializeObject(prospectDtos);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[Pipeline.BindBoard] {0}", ex.Message);
                hdnBoardJson.Value     = "{}";
                hdnProspectsJson.Value = "[]";
            }
        }
    }
}
