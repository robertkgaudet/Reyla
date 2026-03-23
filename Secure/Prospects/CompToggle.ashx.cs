using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Newtonsoft.Json;
using Reyla.Services;
using Reyla.Services.Models;

namespace Reyla.Secure.Prospects
{
    /// <summary>
    /// Lightweight JSON handler for client-side comp add/remove/clear.
    ///
    /// POST parameters:
    ///   prospectId  — Guid
    ///   clip        — CLIP string, or "__CLEAR_ALL__"
    ///   action      — "add" | "remove" | "clear"
    ///
    /// Returns JSON:
    /// {
    ///   success: true,
    ///   selectedCount: 3,
    ///   selectedComps: [ { clip, address, city, dist, saleDate, price, sqft, ppsf, yrBuilt, lat, lng } ],
    ///   valuation: {
    ///     success: true,
    ///     pointEstimate: 420000,
    ///     rangeLow: 378000,
    ///     rangeHigh: 462000,
    ///     weightedMedianPpsf: 233,
    ///     subjectSqFt: 1800,
    ///     compsUsed: 3,
    ///     confidenceLabel: "Low",
    ///     confidenceColor: "danger",
    ///     errorMessage: null
    ///   }
    /// }
    /// </summary>
    public class CompToggle : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetNoStore();

            try
            {
                // ── Auth ────────────────────────────────────────────
                // Use HttpContext identity directly — more reliable in .ashx than
                // Membership.GetUser() which requires session to be fully initialized.
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
                    { Write(context, Error("Profile not found.")); return; }
                    orgId = profile.OrganizationId.Value;
                }

                // ── Parameters ──────────────────────────────────────
                string prospectIdStr = context.Request.Form["prospectId"] ?? context.Request.QueryString["prospectId"];
                string clip          = context.Request.Form["clip"]        ?? context.Request.QueryString["clip"];
                string action        = context.Request.Form["action"]      ?? context.Request.QueryString["action"];

                if (!Guid.TryParse(prospectIdStr, out Guid prospectId))
                {
                    Write(context, Error("Invalid prospectId.")); return;
                }

                if (string.IsNullOrEmpty(action))
                {
                    Write(context, Error("action is required.")); return;
                }

                // ── Execute toggle ──────────────────────────────────
                var svc = new ProspectService();

                if (action == "clear" || clip == "__CLEAR_ALL__")
                {
                    svc.ClearAllComps(prospectId, orgId);
                }
                else if (action == "recalc" || clip == "__RECALC_ONLY__")
                {
                    // No comp change — just recalculate valuation from current selection
                }
                else if (action == "add" || action == "remove")
                {
                    if (string.IsNullOrEmpty(clip)) { Write(context, Error("clip is required.")); return; }
                    svc.ToggleComp(prospectId, clip, action == "add", orgId);
                }
                else
                {
                    Write(context, Error("Unknown action: " + action)); return;
                }

                // ── Load updated selected comps ─────────────────────
                var selected = svc.GetSelectedComps(prospectId, orgId);
                var compDtos = selected.OrderBy(c => c.Distance).Select(c => new
                {
                    clip        = c.Clip,
                    address     = c.StreetAddress ?? "—",
                    city        = c.City ?? "",
                    dist        = c.Distance.HasValue     ? c.Distance.Value.ToString("F2") + " mi" : "",
                    saleDate    = FormatCompDate(c.SaleDate),
                    price       = c.SalePrice.HasValue    ? "$" + c.SalePrice.Value.ToString("N0") : "",
                    sqft        = c.BuildingSquareFeet.HasValue ? c.BuildingSquareFeet.Value.ToString("N0") + " sqft" : "",
                    ppsf        = c.SalePrice.HasValue && c.BuildingSquareFeet.HasValue && c.BuildingSquareFeet > 0
                                  ? "$" + Math.Round(c.SalePrice.Value / c.BuildingSquareFeet.Value).ToString("N0") + "/sqft"
                                  : "",
                    yrBuilt     = c.YearBuilt ?? "",
                    lat         = c.Latitude,
                    lng         = c.Longitude
                }).ToList();

                // ── Recalculate valuation ───────────────────────────
                var valSvc = new ReylaValuationService();
                var val    = valSvc.Calculate(prospectId, orgId);

                var valDto = new
                {
                    success            = val.Success,
                    errorMessage       = val.ErrorMessage,
                    pointEstimate      = val.PointEstimate,
                    rangeLow           = val.RangeLow,
                    rangeHigh          = val.RangeHigh,
                    weightedMedianPpsf = val.WeightedMedianPpsf,
                    subjectSqFt        = val.SubjectSqFt,
                    subjectYearBuilt   = val.SubjectYearBuilt,
                    compsUsed          = val.CompsUsed,
                    confidenceLabel    = val.ConfidenceLabel,
                    confidenceColor    = val.ConfidenceColor
                };

                var result = new
                {
                    success       = true,
                    selectedCount = selected.Count,
                    selectedComps = compDtos,
                    valuation     = valDto
                };

                Write(context, JsonConvert.SerializeObject(result));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[CompToggle] {0}", ex);
                Write(context, Error("Server error: " + ex.Message));
            }
        }

        private static void Write(HttpContext ctx, string json)
        {
            ctx.Response.Write(json);
        }

        private static string Error(string msg)
        {
            return JsonConvert.SerializeObject(new { success = false, errorMessage = msg });
        }

        private static string FormatCompDate(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string clean = s.Replace("-", "");
            if (clean.Length < 6) return s;
            if (!int.TryParse(clean.Substring(0, 4), out int y) ||
                !int.TryParse(clean.Substring(4, 2), out int m)) return s;
            var months = new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };
            if (m < 1 || m > 12) return y.ToString();
            return months[m - 1] + " " + y;
        }
    }
}
