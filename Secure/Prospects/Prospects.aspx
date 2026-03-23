<%@ Page Title="Prospects" Language="C#" MasterPageFile="~/Reyla.Master" AutoEventWireup="true"
         CodeBehind="Prospects.aspx.cs" Inherits="Reyla.Secure.Prospects.ProspectsList" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

    <link href="/Theme/assets/plugins/datatables/buttons.bootstrap5.min.css" rel="stylesheet" />

    <style>
        .addr-street { font-weight:600; color:#1e293b; }
        .addr-city   { font-size:12px; color:#94a3b8; }

        /* Right-aligned numeric columns — on both th and td */
        .col-num { text-align:right !important; white-space:nowrap; }

        /* Comp count pill */
        .comps-count { display:inline-block; font-size:11px; font-weight:700;
                       background:#eff6ff; color:#1d4ed8; border-radius:20px;
                       padding:1px 8px; min-width:24px; text-align:center; }
        .comps-count.zero { background:#f1f5f9; color:#94a3b8; font-weight:400; }

        /* Action cell — buttons always on one line */
        .action-cell          { white-space:nowrap; text-align:center; }
        .action-cell .btn     { font-size:11px; padding:2px 8px; line-height:1.6; }

        /* Prevent card from clipping the DataTables button bar */
        #pnlTable .card { overflow: visible; }

        /* Override DataTables Bootstrap5 purple button theme completely */
        div.dt-buttons {
            padding: 8px 0 10px;
            margin-top: 8px;
            overflow: visible;
        }
        div.dt-buttons a.dt-button,
        div.dt-buttons button.dt-button,
        div.dt-buttons span.dt-button {
            font-size: 12px !important;
            font-weight: 500 !important;
            padding: 4px 12px !important;
            border-radius: 4px !important;
            background: transparent !important;
            background-image: none !important;
            border: 1px solid #dee2e6 !important;
            color: #6c757d !important;
            box-shadow: none !important;
            text-shadow: none !important;
            line-height: 1.5 !important;
        }
        div.dt-buttons a.dt-button:hover,
        div.dt-buttons button.dt-button:hover {
            background: #f8f9fa !important;
            border-color: #adb5bd !important;
            color: #343a40 !important;
        }
        div.dt-buttons a.dt-button:focus,
        div.dt-buttons button.dt-button:focus,
        div.dt-buttons a.dt-button:active,
        div.dt-buttons button.dt-button:active {
            outline: none !important;
            box-shadow: none !important;
            background: #f0f0f0 !important;
        }
    </style>

    <%-- Page header --%>
    <div class="page-title-head d-flex align-items-sm-center flex-sm-row flex-column gap-2 mb-3">
        <div class="flex-grow-1">
            <h4 class="fs-lg fw-bold mb-1">Prospects</h4>
            <p class="text-muted mb-0 fs-xs">
                <asp:Literal ID="litSubtitle" runat="server" />
            </p>
        </div>
        <div class="text-end">
            <ol class="breadcrumb m-0 py-0 fs-xs">
                <li class="breadcrumb-item"><a href="/Secure/Index">Home</a></li>
                <li class="breadcrumb-item active">Prospects</li>
            </ol>
        </div>
    </div>

    <%-- Summary stat widgets --%>
    <div class="row g-3 mb-3">
        <div class="col-6 col-sm-3">
            <div class="card text-center">
                <div class="card-body py-3">
                    <p class="text-muted fs-xs text-uppercase fw-semibold mb-1">Total</p>
                    <asp:Literal ID="litTotalCount" runat="server"><h4 class="mb-0">—</h4></asp:Literal>
                </div>
            </div>
        </div>
        <div class="col-6 col-sm-3">
            <div class="card text-center">
                <div class="card-body py-3">
                    <p class="text-muted fs-xs text-uppercase fw-semibold mb-1">New</p>
                    <asp:Literal ID="litNewCount" runat="server"><h4 class="mb-0">—</h4></asp:Literal>
                </div>
            </div>
        </div>
        <div class="col-6 col-sm-3">
            <div class="card text-center">
                <div class="card-body py-3">
                    <p class="text-muted fs-xs text-uppercase fw-semibold mb-1">Active</p>
                    <asp:Literal ID="litActiveCount" runat="server"><h4 class="mb-0">—</h4></asp:Literal>
                </div>
            </div>
        </div>
        <div class="col-6 col-sm-3">
            <div class="card text-center">
                <div class="card-body py-3">
                    <p class="text-muted fs-xs text-uppercase fw-semibold mb-1">Closed</p>
                    <asp:Literal ID="litClosedCount" runat="server"><h4 class="mb-0">—</h4></asp:Literal>
                </div>
            </div>
        </div>
    </div>

    <%-- Prospects table --%>
    <asp:Panel ID="pnlTable" runat="server">
        <div class="card">
            <div class="card-header d-flex align-items-center justify-content-between">
                <h5 class="card-title mb-0">Prospect Properties</h5>
                <a href="/Secure/Prospects/Search" class="btn btn-primary btn-sm">
                    <i class="ti ti-plus me-1"></i>Add New
                </a>
            </div>
            <div class="card-body pt-3">
                <div class="table-responsive">
                    <table id="tblProspects"
                           class="table table-striped table-hover align-middle mb-0"
                           style="width:100%">
                        <thead class="text-uppercase fs-xxs">
                            <tr>
                                <th>Property</th>
                                <th>Status</th>
                                <th>Added</th>
                                <th class="col-num">Sq Ft</th>
                                <th class="col-num">Yr Built</th>
                                <th class="col-num">Last Sold</th>
                                <th class="col-num">Sale Price</th>
                                <th class="col-num">PPSF</th>
                                <th class="col-num">Comps</th>
                                <th class="text-center">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptProspects" runat="server"
                                          OnItemDataBound="rptProspects_ItemDataBound">
                                <ItemTemplate>
                                    <tr>
                                        <td data-order="<%# HttpUtility.HtmlAttributeEncode((string)Eval("StreetAddress") ?? "") %>">
                                            <div class="addr-street"><%# HttpUtility.HtmlEncode((string)Eval("StreetAddress") ?? "—") %></div>
                                            <div class="addr-city"><%# HttpUtility.HtmlEncode((string)Eval("CityLine") ?? "") %></div>
                                        </td>
                                        <td data-order="<%# HttpUtility.HtmlAttributeEncode((string)Eval("Status") ?? "New") %>">
                                            <%# GetStatusBadge((string)Eval("Status")) %>
                                        </td>
                                        <td data-order="<%# ((DateTime)Eval("CreatedAtUtc")).ToString("yyyy-MM-dd") %>">
                                            <%# ((DateTime)Eval("CreatedAtUtc")).ToLocalTime().ToString("MMM d, yyyy") %>
                                        </td>
                                        <td class="col-num" data-order="<%# Eval("SqFt") ?? 0 %>"><%# FormatInt(Eval("SqFt")) %></td>
                                        <td class="col-num" data-order="<%# Eval("YearBuilt") ?? 0 %>"><%# FormatInt(Eval("YearBuilt")) %></td>
                                        <td class="col-num" data-order="<%# Eval("LastSaleDate") != null ? ((DateTime)Eval("LastSaleDate")).ToString("yyyy-MM-dd") : "0000-00-00" %>">
                                            <%# FormatDate(Eval("LastSaleDate")) %>
                                        </td>
                                        <td class="col-num" data-order="<%# Eval("LastSaleAmount") ?? 0 %>"><%# FormatMoney(Eval("LastSaleAmount")) %></td>
                                        <td class="col-num" data-order="<%# GetPpsfRaw(Eval("LastSaleAmount"), Eval("SqFt")) %>"><%# FormatPpsf(Eval("LastSaleAmount"), Eval("SqFt")) %></td>
                                        <td class="col-num" data-order="<%# Eval("CompCount") ?? -1 %>"><%# FormatCompCount(Eval("CompCount")) %></td>
                                        <td class="action-cell">
                                            <asp:HyperLink ID="lnkComps" runat="server" CssClass="btn btn-outline-primary btn-sm" />
                                            <a href='<%# ResolveUrl("~/Secure/Prospects/PropertyDetail.aspx?clip=" + Eval("Clip")) %>'
                                               class="btn btn-outline-secondary btn-sm ms-1">Detail</a>
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </asp:Panel>

    <%-- Empty state --%>
    <asp:Panel ID="pnlEmpty" runat="server" Visible="false">
        <div class="card">
            <div class="card-body text-center py-5">
                <i class="ti ti-building-estate fs-48 text-muted d-block mb-3"></i>
                <h5 class="fw-semibold">No prospects yet</h5>
                <p class="text-muted mb-3">Search for a property and add it as a prospect to start tracking it.</p>
                <a href="/Secure/Prospects/Search" class="btn btn-primary">
                    <i class="ti ti-plus me-1"></i>Add Your First Prospect
                </a>
            </div>
        </div>
    </asp:Panel>

    <%-- DataTables — loaded after vendors.min.js via window.load --%>
    <script>
        window.addEventListener('load', function () {
            var base = '/Theme/assets/plugins/datatables/';
            var scripts = [
                base + 'dataTables.min.js',
                base + 'dataTables.bootstrap5.min.js',
                base + 'dataTables.responsive.min.js',
                base + 'responsive.bootstrap5.min.js',
                base + 'dataTables.buttons.min.js',
                base + 'buttons.bootstrap5.min.js',
                base + 'jszip.min.js',
                base + 'pdfmake.min.js',
                base + 'vfs_fonts.js',
                base + 'buttons.html5.min.js',
                base + 'buttons.print.min.js'
            ];
            function loadNext(i) {
                if (i >= scripts.length) {
                    $('#tblProspects').DataTable({
                        // Buttons get their own row so they're never clipped
                        dom : "<'row mb-2'<'col-12'B>>" +
                              "<'row'<'col-sm-12 col-md-6'i><'col-sm-12 col-md-6'f>>" +
                              "<'row'<'col-12'tr>>" +
                              "<'row mt-2'<'col-sm-12 col-md-5'l><'col-sm-12 col-md-7'p>>",
                        pageLength : 25,
                        lengthMenu : [[10, 25, 50, -1], [10, 25, 50, 'All']],
                        order      : [[2, 'desc']],
                        responsive : true,
                        columnDefs : [
                            { targets: 0,                responsivePriority: 1 },
                            { targets: 9,                responsivePriority: 2, orderable: false, searchable: false },
                            { targets: [1,2,3,4,5,6,7,8], responsivePriority: 10 }
                        ],
                        buttons: [
                            { extend: 'copy',  className: 'btn btn-sm btn-outline-secondary' },
                            { extend: 'csv',   className: 'btn btn-sm btn-outline-secondary' },
                            { extend: 'excel', className: 'btn btn-sm btn-outline-secondary' },
                            { extend: 'print', className: 'btn btn-sm btn-outline-secondary' },
                            { extend: 'pdf',   className: 'btn btn-sm btn-outline-secondary' }
                        ]
                    });
                    return;
                }
                var s = document.createElement('script');
                s.src = scripts[i];
                s.onload = function () { loadNext(i + 1); };
                document.body.appendChild(s);
            }
            loadNext(0);
        });
    </script>

</asp:Content>
