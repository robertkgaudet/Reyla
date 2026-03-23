<%@ Page Title="Activity Feed" Language="C#" MasterPageFile="~/Reyla.Master" AutoEventWireup="true"
         CodeBehind="Activity.aspx.cs" Inherits="Reyla.Secure.ActivityFeed" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

    <style>
        /* ── Timeline time column — fixed width so dots stay aligned ─── */
        .timeline-time { min-width: 110px; font-size: 12px; }

        /* ── Activity-specific content styles ───────────────────────── */
        .activity-summary  { font-size: 14px; font-weight: 600; margin-bottom: 2px; }
        .activity-prospect { font-size: 12px; color: #3b82f6; }
        .activity-ts       { font-size: 11px; color: #94a3b8; }

        /* ── Filter bar ─────────────────────────────────────────────── */
        .filter-bar .form-select,
        .filter-bar .form-control { font-size: 13px; }

        /* ── Scope toggle ───────────────────────────────────────────── */
        .btn-group-scope .btn { font-size: 12px; }

        /* ── Date group header ──────────────────────────────────────── */
        .timeline-date-group {
            font-size: 11px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: .06em;
            color: #94a3b8;
            padding: 8px 0 4px;
        }

        /* ── Empty state ────────────────────────────────────────────── */
        .empty-state      { padding: 60px 20px; text-align: center; color: #94a3b8; }
        .empty-state i    { font-size: 48px; display: block; margin-bottom: 12px; }
        .empty-state p    { margin: 0; font-size: 14px; }
    </style>

    <%-- ── Page header ──────────────────────────────────────────────── --%>
    <div class="page-title-head d-flex align-items-sm-center flex-sm-row flex-column gap-2 mb-3">
        <div class="flex-grow-1">
            <h4 class="fs-lg fw-bold mb-1">Activity Feed</h4>
            <p class="text-muted mb-0 fs-xs">
                <asp:Literal ID="litSubtitle" runat="server" />
            </p>
        </div>
        <div class="text-end">
            <ol class="breadcrumb m-0 py-0 fs-xs">
                <li class="breadcrumb-item"><a href="/Secure/Index">Home</a></li>
                <li class="breadcrumb-item active">Activity Feed</li>
            </ol>
        </div>
    </div>

    <%-- ── Filter card ────────────────────────────────────────────────── --%>
    <div class="card mb-3">
        <div class="card-body py-2">
            <div class="row g-2 align-items-center filter-bar">

                <%-- Scope toggle: Mine / All Org --%>
                <div class="col-12 col-sm-auto">
                    <div class="btn-group btn-group-scope btn-group-sm" role="group">
                        <asp:LinkButton ID="btnScopeMine" runat="server"
                            CssClass="btn btn-primary"
                            OnClick="btnScope_Click" CommandArgument="mine">
                            <i class="ti ti-user me-1"></i>My Activity
                        </asp:LinkButton>
                        <asp:LinkButton ID="btnScopeOrg" runat="server"
                            CssClass="btn btn-outline-secondary"
                            OnClick="btnScope_Click" CommandArgument="org">
                            <i class="ti ti-building me-1"></i>All Org
                        </asp:LinkButton>
                    </div>
                </div>

                <%-- Activity type filter --%>
                <div class="col-12 col-sm-auto">
                    <asp:DropDownList ID="ddlType" runat="server" CssClass="form-select form-select-sm"
                        AutoPostBack="true" OnSelectedIndexChanged="Filter_Changed">
                        <asp:ListItem Value="" Text="All Types" />
                        <asp:ListItem Value="Prospect" Text="Prospects" />
                        <asp:ListItem Value="Comp"     Text="Comps" />
                        <asp:ListItem Value="Note"     Text="Notes" />
                        <asp:ListItem Value="Deal"     Text="Deals" />
                        <asp:ListItem Value="Contact"  Text="Contacts" />
                    </asp:DropDownList>
                </div>

                <%-- Date from --%>
                <div class="col-6 col-sm-auto">
                    <asp:TextBox ID="txtDateFrom" runat="server"
                        CssClass="form-control form-control-sm"
                        TextMode="Date"
                        placeholder="From"
                        AutoPostBack="true"
                        OnTextChanged="Filter_Changed" />
                </div>

                <%-- Date to --%>
                <div class="col-6 col-sm-auto">
                    <asp:TextBox ID="txtDateTo" runat="server"
                        CssClass="form-control form-control-sm"
                        TextMode="Date"
                        placeholder="To"
                        AutoPostBack="true"
                        OnTextChanged="Filter_Changed" />
                </div>

                <%-- Address search --%>
                <div class="col-12 col-sm">
                    <div class="input-group input-group-sm">
                        <span class="input-group-text"><i class="ti ti-search"></i></span>
                        <asp:TextBox ID="txtSearch" runat="server"
                            CssClass="form-control"
                            placeholder="Search by address…"
                            AutoPostBack="true"
                            OnTextChanged="Filter_Changed" />
                    </div>
                </div>

                <%-- Clear filters --%>
                <div class="col-12 col-sm-auto">
                    <asp:LinkButton ID="btnClear" runat="server" CssClass="btn btn-sm btn-outline-secondary"
                        OnClick="btnClear_Click">
                        <i class="ti ti-x me-1"></i>Clear
                    </asp:LinkButton>
                </div>

            </div>
        </div>
    </div>

    <%-- ── Timeline card ───────────────────────────────────────────────── --%>
    <div class="card">
        <div class="card-header d-flex align-items-center justify-content-between">
            <h5 class="card-title mb-0">
                <i class="ti ti-timeline me-1 text-primary"></i>
                <asp:Literal ID="litFeedTitle" runat="server" Text="My Activity" />
            </h5>
            <span class="badge text-bg-secondary">
                <asp:Literal ID="litCount" runat="server" Text="0" /> events
            </span>
        </div>
        <div class="card-body">

            <%-- Empty state --%>
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
                <div class="empty-state">
                    <i class="ti ti-clock-off text-muted"></i>
                    <p>No activity found.<br />
                       <span class="text-muted">Try adjusting your filters or add a prospect to get started.</span>
                    </p>
                </div>
            </asp:Panel>

            <%-- Timeline --%>
            <asp:Panel ID="pnlTimeline" runat="server">
                <div class="timeline timeline-icon-based">
                    <asp:Repeater ID="rptActivity" runat="server"
                                  OnItemDataBound="rptActivity_ItemDataBound">
                        <ItemTemplate>

                            <%-- Date group header — rendered by code-behind via a Literal --%>
                            <asp:Literal ID="litDateGroup" runat="server" />

                            <div class="timeline-item d-flex align-items-stretch">

                                <%-- Time --%>
                                <div class="timeline-time pe-3 text-muted">
                                    <%# FormatTimeAgo((DateTime)Eval("CreatedAtUtc")) %>
                                </div>

                                <%-- Icon dot — colour + icon driven by ActivityType --%>
                                <div class="<%# GetDotClass((string)Eval("ActivityType")) %>">
                                    <i class="<%# GetIconClass((string)Eval("ActivityType")) %> fs-xl"></i>
                                </div>

                                <%-- Content --%>
                                <div class="timeline-content ps-3 pb-4">
                                    <div class="activity-summary">
                                        <%# HttpUtility.HtmlEncode((string)Eval("Summary")) %>
                                    </div>
                                    <asp:Literal ID="litProspectLink" runat="server" />
                                    <div class="activity-ts">
                                        <%# ((DateTime)Eval("CreatedAtUtc")).ToLocalTime().ToString("MMM d, yyyy h:mm tt") %>
                                        <%# GetEntityTypeBadge((string)Eval("EntityType")) %>
                                    </div>
                                </div>

                            </div>

                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </asp:Panel>

        </div>
    </div>

</asp:Content>
