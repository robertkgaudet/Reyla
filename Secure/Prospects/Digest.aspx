<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Digest.aspx.cs"
         Inherits="Reyla.Secure.Prospects.Digest" MasterPageFile="~/Reyla.Master" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
<% var d = _digest; if (d == null) { Response.Redirect("~/Login.aspx"); return; } %>
<% var win = d.WindowDays; %>

<style>
    .digest-wrap  { max-width:1200px; margin:0 auto; padding:20px 20px 60px; }

    /* Stat cards — Homer style */
    .stat-card    { background:#fff; border:1px solid #e5e7eb; border-radius:10px;
                    padding:20px 22px; height:100%; }
    .stat-label   { font-size:10px; font-weight:700; text-transform:uppercase;
                    letter-spacing:.06em; color:#9ca3af; margin-bottom:6px; }
    .stat-value   { font-size:32px; font-weight:800; color:#111827; line-height:1; margin-bottom:4px; }
    .stat-sub     { font-size:12px; color:#6b7280; }
    .stat-icon    { font-size:28px; opacity:.15; }

    /* Hero stat cards — coloured */
    .stat-card.hero-blue  { background:#1e40af; border-color:#1e40af; color:#fff; }
    .stat-card.hero-green { background:#16a34a; border-color:#16a34a; color:#fff; }
    .stat-card.hero-amber { background:#d97706; border-color:#d97706; color:#fff; }
    .stat-card.hero-blue  .stat-value,
    .stat-card.hero-green .stat-value,
    .stat-card.hero-amber .stat-value  { color:#fff; }
    .stat-card.hero-blue  .stat-label,
    .stat-card.hero-green .stat-label,
    .stat-card.hero-amber .stat-label  { color:rgba(255,255,255,.7); }
    .stat-card.hero-blue  .stat-sub,
    .stat-card.hero-green .stat-sub,
    .stat-card.hero-amber .stat-sub    { color:rgba(255,255,255,.8); }
    .stat-card.hero-blue  .stat-icon,
    .stat-card.hero-green .stat-icon,
    .stat-card.hero-amber .stat-icon   { opacity:.25; color:#fff; }

    /* Action rows inside cards */
    .digest-list-item { display:flex; align-items:flex-start; gap:10px;
                         padding:10px 0; border-bottom:1px solid #f3f4f6; }
    .digest-list-item:last-child { border-bottom:none; }
    .digest-list-icon { width:32px; height:32px; border-radius:8px; display:flex;
                         align-items:center; justify-content:center; flex-shrink:0; font-size:15px; }

    /* Progress bar for pipeline funnel */
    .funnel-row   { display:flex; align-items:center; gap:10px; margin-bottom:10px; }
    .funnel-label { font-size:12px; color:#374151; width:100px; flex-shrink:0; }
    .funnel-bar   { flex:1; background:#f3f4f6; border-radius:20px; height:8px; overflow:hidden; }
    .funnel-fill  { height:100%; border-radius:20px; transition:width .5s; }
    .funnel-count { font-size:12px; font-weight:700; color:#111827; width:24px; text-align:right; flex-shrink:0; }

    /* Activity timeline */
    .timeline-item { display:flex; gap:10px; padding:8px 0;
                     border-bottom:1px solid #f9fafb; }
    .timeline-dot  { width:8px; height:8px; border-radius:50%; background:#d1d5db;
                     flex-shrink:0; margin-top:5px; }
    .timeline-dot.blue   { background:#3b82f6; }
    .timeline-dot.green  { background:#22c55e; }
    .timeline-dot.amber  { background:#f59e0b; }
    .timeline-dot.red    { background:#ef4444; }

    /* Window selector pills */
    .window-pill  { display:inline-block; padding:4px 14px; border-radius:20px; font-size:12px;
                    font-weight:600; text-decoration:none; color:#6b7280;
                    border:1px solid #e5e7eb; background:#fff; margin-right:4px; }
    .window-pill.active { background:#1e40af; color:#fff; border-color:#1e40af; }

    /* Empty state */
    .digest-empty { text-align:center; padding:28px 16px; color:#9ca3af; font-size:13px; }
    .digest-empty i { font-size:28px; display:block; margin-bottom:8px; }
</style>

<div class="digest-wrap">

    <%-- ── Header ──────────────────────────────────────────────────────── --%>
    <div class="d-flex align-items-start justify-content-between flex-wrap gap-2 mb-4">
        <div>
            <%-- Org name --%>
            <% if (!string.IsNullOrEmpty(_orgName)) { %>
            <div class="d-flex align-items-center gap-2 mb-1">
                <span class="badge text-bg-primary fs-xs px-2 py-1">
                    <i class="ti ti-building me-1"></i><%= HttpUtility.HtmlEncode(_orgName) %>
                </span>
            </div>
            <% } %>
            <h4 class="fw-bold mb-0">
                <i class="ti ti-report-analytics me-2 text-primary"></i>Daily Digest
            </h4>
            <p class="text-muted fs-sm mb-0">
                Generated <%= d.GeneratedAt.ToLocalTime().ToString("MMM d, yyyy h:mm tt") %>
                &nbsp;&middot;&nbsp;
                Showing last
                <strong><%= win == 1 ? "24 hours" : win + " days" %></strong>
            </p>
        </div>
        <div class="d-flex align-items-start gap-3 flex-wrap">
            <%-- User info card --%>
            <div class="d-flex align-items-center gap-2 p-2 rounded"
                 style="background:#f8fafc;border:1px solid #e2e8f0;font-size:12px;">
                <div class="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0"
                     style="width:34px;height:34px;background:#dbeafe;">
                    <i class="ti ti-user text-primary fs-16"></i>
                </div>
                <div>
                    <div class="fw-semibold text-dark" style="font-size:13px;">
                        <%= HttpUtility.HtmlEncode(_userFullName) %>
                    </div>
                    <div class="text-muted" style="font-size:11px;">
                        <%= HttpUtility.HtmlEncode(_userEmail) %>
                    </div>
                </div>
            </div>
            <%-- Controls --%>
            <div class="d-flex flex-column align-items-end gap-2">
                <div>
                    <a href="?days=1"  class="window-pill <%= win==1  ? "active" : "" %>">Today</a>
                    <a href="?days=7"  class="window-pill <%= win==7  ? "active" : "" %>">7 Days</a>
                    <a href="?days=30" class="window-pill <%= win==30 ? "active" : "" %>">30 Days</a>
                </div>
                <div class="d-flex gap-2">
                    <a href="/Secure/Prospects/Prospects" class="btn btn-outline-secondary btn-sm">
                        <i class="ti ti-list me-1"></i>Prospects
                    </a>
                    <a href="/Secure/Prospects/Pipeline" class="btn btn-outline-primary btn-sm">
                        <i class="ti ti-layout-kanban me-1"></i>Pipeline
                    </a>
                </div>
            </div>
        </div>
    </div>

    <%-- ══════════════════════════════════════════════════════
         ROW 1 — Hero stat tiles (Homer-style coloured cards)
    ══════════════════════════════════════════════════════ --%>
    <div class="row g-3 mb-4">
        <div class="col-6 col-md-3">
            <div class="stat-card hero-blue h-100">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <div class="stat-label">Active Deals</div>
                        <div class="stat-value"><%= d.ActiveDeals %></div>
                        <div class="stat-sub"><%= d.DealsUnderContract %> under contract</div>
                    </div>
                    <i class="ti ti-briefcase stat-icon"></i>
                </div>
            </div>
        </div>
        <div class="col-6 col-md-3">
            <div class="stat-card hero-green h-100">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <div class="stat-label">Prospects</div>
                        <div class="stat-value"><%= d.TotalActiveProspects %></div>
                        <div class="stat-sub">+<%= d.NewProspects %> this period</div>
                    </div>
                    <i class="ti ti-building-estate stat-icon"></i>
                </div>
            </div>
        </div>
        <div class="col-6 col-md-3">
            <div class="stat-card hero-amber h-100">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <div class="stat-label">Contacts Made</div>
                        <div class="stat-value"><%= d.ContactsMade %></div>
                        <div class="stat-sub"><%= d.NotesAdded %> notes added</div>
                    </div>
                    <i class="ti ti-phone stat-icon"></i>
                </div>
            </div>
        </div>
        <div class="col-6 col-md-3">
            <div class="stat-card h-100">
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <div class="stat-label">Deals Moved</div>
                        <div class="stat-value"><%= d.DealsMoved %></div>
                        <div class="stat-sub"><%= d.DealsClosedPeriod %> closed this period</div>
                    </div>
                    <i class="ti ti-arrows-right-left stat-icon" style="opacity:.1;font-size:28px;color:#1e40af;"></i>
                </div>
            </div>
        </div>
    </div>

    <%-- ══════════════════════════════════════════════════════
         ROW 2 — Pipeline funnel + Upcoming closes
    ══════════════════════════════════════════════════════ --%>
    <div class="row g-3 mb-4">

        <%-- Pipeline funnel --%>
        <div class="col-12 col-md-5">
            <div class="card h-100">
                <div class="card-header d-flex align-items-center justify-content-between">
                    <h5 class="card-title mb-0">
                        <i class="ti ti-filter me-2 text-primary"></i>Pipeline Funnel
                    </h5>
                    <a href="/Secure/Prospects/Pipeline" class="btn btn-xs btn-outline-primary py-0 px-2" style="font-size:11px;">View Board</a>
                </div>
                <div class="card-body">
                    <% var stageData = new[] {
                           new { Label="Lead",           Count=d.DealsInLead,        Color="#93c5fd" },
                           new { Label="Qualified",      Count=d.ActiveDeals > 0 ? d.ActiveDeals - d.DealsInLead - d.DealsInLOI - d.DealsUnderContract : 0, Color="#60a5fa" },
                           new { Label="LOI",            Count=d.DealsInLOI,         Color="#3b82f6" },
                           new { Label="Under Contract", Count=d.DealsUnderContract, Color="#1d4ed8" },
                       };
                       int maxCount = stageData.Max(s => s.Count) + 1;
                    %>
                    <% foreach (var stage in stageData) { %>
                    <div class="funnel-row">
                        <div class="funnel-label"><%= stage.Label %></div>
                        <div class="funnel-bar">
                            <div class="funnel-fill" style="width:<%= maxCount > 0 ? (stage.Count * 100 / maxCount) : 0 %>%;background:<%= stage.Color %>"></div>
                        </div>
                        <div class="funnel-count"><%= stage.Count %></div>
                    </div>
                    <% } %>

                    <% if (d.StaleDeals.Any()) { %>
                    <div class="alert alert-warning py-2 mt-3 mb-0 d-flex align-items-center gap-2">
                        <i class="ti ti-clock-exclamation flex-shrink-0"></i>
                        <span class="fs-xs"><strong><%= d.StaleDeals.Count %> deal<%= d.StaleDeals.Count > 1 ? "s" : "" %></strong> stagnant 14+ days</span>
                    </div>
                    <% } %>
                </div>
            </div>
        </div>

        <%-- Upcoming closes --%>
        <div class="col-12 col-md-7">
            <div class="card h-100">
                <div class="card-header">
                    <h5 class="card-title mb-0">
                        <i class="ti ti-calendar-event me-2 text-success"></i>Closing Within 30 Days
                        <% if (d.UpcomingCloses.Any()) { %>
                        <span class="badge text-bg-success ms-1"><%= d.UpcomingCloses.Count %></span>
                        <% } %>
                    </h5>
                </div>
                <div class="card-body p-0">
                    <% if (!d.UpcomingCloses.Any()) { %>
                    <div class="digest-empty">
                        <i class="ti ti-calendar-off"></i>No closes scheduled in the next 30 days
                    </div>
                    <% } else { %>
                    <div class="table-responsive">
                        <table class="table table-sm table-hover align-middle mb-0">
                            <thead class="text-uppercase" style="font-size:10px;">
                                <tr>
                                    <th class="ps-3">Property</th>
                                    <th>Stage</th>
                                    <th class="text-end">Asking</th>
                                    <th class="text-end pe-3">Close Date</th>
                                </tr>
                            </thead>
                            <tbody>
                                <% foreach (var c in d.UpcomingCloses) { %>
                                <tr>
                                    <td class="ps-3">
                                        <% if (!string.IsNullOrEmpty(c.Clip)) { %>
                                        <a href="/Secure/Prospects/PropertyDetail.aspx?clip=<%= HttpUtility.UrlEncode(c.Clip) %>"
                                           class="text-dark fw-semibold fs-xs text-decoration-none">
                                            <%= HttpUtility.HtmlEncode(c.Title) %>
                                        </a>
                                        <% } else { %>
                                        <span class="fw-semibold fs-xs"><%= HttpUtility.HtmlEncode(c.Title) %></span>
                                        <% } %>
                                    </td>
                                    <td>
                                        <span class="badge text-bg-<%= StageBadgeColor(c.Stage) %>">
                                            <%= StageLabel(c.Stage) %>
                                        </span>
                                    </td>
                                    <td class="text-end fs-xs">
                                        <%= c.AskingPrice.HasValue ? "$" + c.AskingPrice.Value.ToString("N0") : "--" %>
                                    </td>
                                    <td class="text-end pe-3">
                                        <span class="fw-semibold fs-xs <%= c.DaysUntilClose <= 7 ? "text-danger" : c.DaysUntilClose <= 14 ? "text-warning" : "text-success" %>">
                                            <%= c.DaysUntilClose == 0 ? "Today" : "In " + c.DaysUntilClose + "d" %>
                                        </span>
                                        <div class="text-muted" style="font-size:10px;"><%= c.CloseDate.ToString("MMM d") %></div>
                                    </td>
                                </tr>
                                <% } %>
                            </tbody>
                        </table>
                    </div>
                    <% } %>
                </div>
            </div>
        </div>

    </div>

    <%-- ══════════════════════════════════════════════════════
         ROW 3 — Outreach gaps + Motivated seller signals
    ══════════════════════════════════════════════════════ --%>
    <div class="row g-3 mb-4">

        <%-- Outreach gap report --%>
        <div class="col-12 col-md-6">
            <div class="card h-100">
                <div class="card-header d-flex align-items-center justify-content-between">
                    <h5 class="card-title mb-0">
                        <i class="ti ti-phone-off me-2 text-danger"></i>Outreach Gaps
                        <% if (d.OutreachGaps.Any()) { %>
                        <span class="badge text-bg-danger ms-1"><%= d.OutreachGaps.Count %></span>
                        <% } %>
                    </h5>
                    <span class="text-muted fs-xs">No contact in 30+ days</span>
                </div>
                <div class="card-body p-0">
                    <% if (!d.OutreachGaps.Any()) { %>
                    <div class="digest-empty">
                        <i class="ti ti-circle-check text-success"></i>All prospects contacted recently
                    </div>
                    <% } else { %>
                    <ul class="list-group list-group-flush">
                        <% foreach (var g in d.OutreachGaps) { %>
                        <li class="list-group-item px-3 py-2">
                            <div class="d-flex align-items-center gap-2">
                                <div class="digest-list-icon <%= g.DaysSinceContact == -1 ? "bg-danger" : "bg-warning" %> bg-opacity-10">
                                    <i class="ti ti-phone-off <%= g.DaysSinceContact == -1 ? "text-danger" : "text-warning" %>"></i>
                                </div>
                                <div class="flex-grow-1 min-width-0">
                                    <% if (!string.IsNullOrEmpty(g.Clip)) { %>
                                    <a href="/Secure/Prospects/PropertyDetail.aspx?clip=<%= HttpUtility.UrlEncode(g.Clip) %>&tab=contact"
                                       class="fw-semibold text-dark text-decoration-none fs-xs d-block text-truncate">
                                        <%= HttpUtility.HtmlEncode(g.Address) %>
                                    </a>
                                    <% } else { %>
                                    <div class="fw-semibold fs-xs text-truncate"><%= HttpUtility.HtmlEncode(g.Address) %></div>
                                    <% } %>
                                    <div class="text-muted" style="font-size:10px;"><%= HttpUtility.HtmlEncode(g.CityLine) %></div>
                                </div>
                                <span class="badge <%= g.DaysSinceContact == -1 ? "text-bg-danger" : "text-bg-warning text-dark" %> flex-shrink-0" style="font-size:10px;">
                                    <%= Ago(g.DaysSinceContact) %>
                                </span>
                            </div>
                        </li>
                        <% } %>
                    </ul>
                    <% } %>
                </div>
            </div>
        </div>

        <%-- Motivated seller signals --%>
        <div class="col-12 col-md-6">
            <div class="card h-100">
                <div class="card-header d-flex align-items-center justify-content-between">
                    <h5 class="card-title mb-0">
                        <i class="ti ti-alert-triangle me-2 text-danger"></i>Motivated Seller Signals
                        <% if (d.SignalAlerts.Any()) { %>
                        <span class="badge text-bg-danger ms-1"><%= d.SignalAlerts.Count %></span>
                        <% } %>
                    </h5>
                </div>
                <div class="card-body p-0">
                    <% if (!d.SignalAlerts.Any()) { %>
                    <div class="digest-empty">
                        <i class="ti ti-shield-check text-success"></i>No distress signals on active prospects
                    </div>
                    <% } else { %>
                    <ul class="list-group list-group-flush">
                        <% foreach (var s in d.SignalAlerts) { %>
                        <li class="list-group-item px-3 py-2">
                            <div class="d-flex align-items-center gap-2">
                                <div class="digest-list-icon bg-<%= s.Color %> bg-opacity-10">
                                    <i class="ti <%= s.SignalType == "Lien" ? "ti-link" : "ti-receipt-tax" %> text-<%= s.Color %>"></i>
                                </div>
                                <div class="flex-grow-1 min-width-0">
                                    <% if (!string.IsNullOrEmpty(s.Clip)) { %>
                                    <a href="/Secure/Prospects/PropertyDetail.aspx?clip=<%= HttpUtility.UrlEncode(s.Clip) %>"
                                       class="fw-semibold text-dark text-decoration-none fs-xs d-block text-truncate">
                                        <%= HttpUtility.HtmlEncode(s.Address) %>
                                    </a>
                                    <% } else { %>
                                    <div class="fw-semibold fs-xs text-truncate"><%= HttpUtility.HtmlEncode(s.Address) %></div>
                                    <% } %>
                                    <div class="text-muted" style="font-size:10px;"><%= HttpUtility.HtmlEncode(s.Detail) %></div>
                                </div>
                                <span class="badge text-bg-<%= s.Color %> flex-shrink-0" style="font-size:10px;">
                                    <%= s.SignalType == "Lien" ? "Lien" : "Tax" %>
                                </span>
                            </div>
                        </li>
                        <% } %>
                    </ul>
                    <% } %>
                </div>
            </div>
        </div>

    </div>

    <%-- ══════════════════════════════════════════════════════
         ROW 4 — Stale deals + Activity feed
    ══════════════════════════════════════════════════════ --%>
    <div class="row g-3">

        <%-- Stale deals --%>
        <div class="col-12 col-md-5">
            <div class="card h-100">
                <div class="card-header d-flex align-items-center justify-content-between">
                    <h5 class="card-title mb-0">
                        <i class="ti ti-clock-pause me-2 text-warning"></i>Stale Deals
                        <% if (d.StaleDeals.Any()) { %>
                        <span class="badge text-bg-warning text-dark ms-1"><%= d.StaleDeals.Count %></span>
                        <% } %>
                    </h5>
                    <span class="text-muted fs-xs">No movement in 14+ days</span>
                </div>
                <div class="card-body p-0">
                    <% if (!d.StaleDeals.Any()) { %>
                    <div class="digest-empty">
                        <i class="ti ti-rocket text-success"></i>All deals are moving -- great momentum!
                    </div>
                    <% } else { %>
                    <ul class="list-group list-group-flush">
                        <% foreach (var s in d.StaleDeals) { %>
                        <li class="list-group-item px-3 py-2">
                            <div class="d-flex align-items-center gap-2">
                                <div class="digest-list-icon bg-warning bg-opacity-10">
                                    <i class="ti ti-clock text-warning"></i>
                                </div>
                                <div class="flex-grow-1 min-width-0">
                                    <% if (!string.IsNullOrEmpty(s.Clip)) { %>
                                    <a href="/Secure/Prospects/PropertyDetail.aspx?clip=<%= HttpUtility.UrlEncode(s.Clip) %>"
                                       class="fw-semibold text-dark text-decoration-none fs-xs d-block text-truncate">
                                        <%= HttpUtility.HtmlEncode(s.Title) %>
                                    </a>
                                    <% } else { %>
                                    <div class="fw-semibold fs-xs text-truncate"><%= HttpUtility.HtmlEncode(s.Title) %></div>
                                    <% } %>
                                    <div class="d-flex align-items-center gap-1 mt-1">
                                        <span class="badge text-bg-<%= StageBadgeColor(s.Stage) %>" style="font-size:9px;">
                                            <%= StageLabel(s.Stage) %>
                                        </span>
                                        <span class="text-muted" style="font-size:10px;">
                                            <%= HttpUtility.HtmlEncode(s.PropertyAddress ?? "") %>
                                        </span>
                                    </div>
                                </div>
                                <span class="badge text-bg-warning text-dark flex-shrink-0" style="font-size:10px;">
                                    <%= s.DaysInStage %>d
                                </span>
                            </div>
                        </li>
                        <% } %>
                    </ul>
                    <% } %>
                </div>
                <% if (d.StaleDeals.Any()) { %>
                <div class="card-footer text-center py-2">
                    <a href="/Secure/Prospects/Pipeline" class="fs-xs text-primary text-decoration-none">
                        <i class="ti ti-layout-kanban me-1"></i>Open Pipeline to move deals
                    </a>
                </div>
                <% } %>
            </div>
        </div>

        <%-- Recent activity feed --%>
        <div class="col-12 col-md-7">
            <div class="card h-100">
                <div class="card-header d-flex align-items-center justify-content-between">
                    <h5 class="card-title mb-0">
                        <i class="ti ti-activity me-2 text-primary"></i>Recent Activity
                    </h5>
                    <a href="/Secure/Prospects/Activity.aspx" class="btn btn-xs btn-outline-secondary py-0 px-2" style="font-size:11px;">
                        Full Feed
                    </a>
                </div>
                <div class="card-body py-2 px-3" style="max-height:380px;overflow-y:auto;">
                    <% if (!d.RecentActivity.Any()) { %>
                    <div class="digest-empty">
                        <i class="ti ti-mood-empty"></i>No activity in this period
                    </div>
                    <% } else { %>
                    <% foreach (var a in d.RecentActivity) {
                           string dotColor = "blue";
                           if (a.ActivityType == "ProspectContacted") dotColor = "green";
                           else if (a.ActivityType == "DealClosed")   dotColor = "green";
                           else if (a.ActivityType.StartsWith("Deal")) dotColor = "amber";
                           else if (a.ActivityType.Contains("Deleted")) dotColor = "red";
                    %>
                    <div class="timeline-item">
                        <div class="timeline-dot <%= dotColor %>"></div>
                        <div class="flex-grow-1">
                            <div class="fs-xs text-dark"><%= HttpUtility.HtmlEncode(a.Summary) %></div>
                            <div class="text-muted" style="font-size:10px;">
                                <%= a.CreatedAtUtc.ToLocalTime().ToString("MMM d, h:mm tt") %>
                                
                            </div>
                        </div>
                    </div>
                    <% } %>
                    <% } %>
                </div>
            </div>
        </div>

    </div>

</div>

</asp:Content>
