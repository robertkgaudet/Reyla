<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PropertyDetail.aspx.cs" Inherits="Reyla.Secure.Prospects.PropertyDetail" MasterPageFile="~/Reyla.Master" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

    <style>
        /* ── Layout ──────────────────────────────────────── */
        .detail-wrap        { max-width: 1100px; margin: 0 auto; padding: 24px 20px 60px; }

        /* ── Header card ─────────────────────────────────── */
        .prop-header        { display: flex; gap: 20px; align-items: flex-start;
                              background: #fff; border: 1px solid #e5e7eb;
                              border-radius: 10px; padding: 20px 24px; margin-bottom: 24px; }
        .prop-header-photo  { flex-shrink: 0; width: 220px; height: 140px;
                              object-fit: cover; border-radius: 7px; background: #f1f5f9; }
        .prop-header-photo-placeholder { flex-shrink: 0; width: 220px; height: 140px;
                              background: #f1f5f9; border-radius: 7px; display: flex;
                              align-items: center; justify-content: center;
                              color: #9ca3af; font-size: 13px; }
        .prop-header-info   { flex: 1; min-width: 0; }
        .prop-address-line  { font-size: 22px; font-weight: 700; color: #111827; line-height: 1.2; }
        .prop-city-line     { font-size: 15px; color: #6b7280; margin-top: 2px; margin-bottom: 10px; }
        .prop-signals       { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 12px; }
        .signal-badge       { display: inline-flex; align-items: center; gap: 5px;
                              font-size: 12px; font-weight: 600; padding: 4px 10px;
                              border-radius: 20px; border: 1px solid; }
        .signal-red         { background: #fef2f2; color: #991b1b; border-color: #fecaca; }
        .signal-amber       { background: #fffbeb; color: #92400e; border-color: #fde68a; }
        .signal-green       { background: #f0fdf4; color: #166534; border-color: #bbf7d0; }
        .signal-blue        { background: #eff6ff; color: #1e40af; border-color: #bfdbfe; }
        .signal-grey        { background: #f9fafb; color: #374151; border-color: #e5e7eb; }
        .prop-header-actions { display: flex; gap: 8px; flex-wrap: wrap; margin-top: 4px; }

        /* ── Tabs ────────────────────────────────────────── */
        .tab-bar            { display: flex; gap: 0; border-bottom: 2px solid #e5e7eb;
                              margin-bottom: 24px; }
        .tab-btn            { padding: 10px 20px; font-size: 14px; font-weight: 500;
                              color: #6b7280; background: none; border: none;
                              border-bottom: 2px solid transparent; margin-bottom: -2px;
                              cursor: pointer; transition: all 0.15s; white-space: nowrap; }
        .tab-btn:hover      { color: #111827; }
        .tab-btn.active     { color: #1e40af; border-bottom-color: #1e40af; }
        .tab-panel          { display: none; }
        .tab-panel.active   { display: block; }

        /* ── Section cards ───────────────────────────────── */
        .section-card       { background: #fff; border: 1px solid #e5e7eb; border-radius: 10px;
                              padding: 20px 24px; margin-bottom: 18px; }
        .section-title      { font-size: 13px; font-weight: 700; text-transform: uppercase;
                              letter-spacing: 0.05em; color: #6b7280; margin-bottom: 14px; }

        /* ── Fact grid ───────────────────────────────────── */
        .fact-grid          { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
                              gap: 14px 20px; }
        .fact-item label    { display: block; font-size: 11px; font-weight: 600; color: #9ca3af;
                              text-transform: uppercase; letter-spacing: 0.04em; margin-bottom: 2px; }
        .fact-item span     { display: block; font-size: 15px; color: #111827; font-weight: 500; }
        .fact-item span.na  { color: #d1d5db; font-style: italic; font-weight: 400; }

        /* ── Tables ──────────────────────────────────────── */
        .data-table         { width: 100%; border-collapse: collapse; font-size: 13px; }
        .data-table th      { text-align: left; padding: 8px 12px; font-size: 11px; font-weight: 700;
                              text-transform: uppercase; letter-spacing: 0.04em; color: #9ca3af;
                              background: #f9fafb; border-bottom: 1px solid #e5e7eb; }
        .data-table td      { padding: 10px 12px; border-bottom: 1px solid #f3f4f6; color: #374151; vertical-align: top; }
        .data-table tr:last-child td { border-bottom: none; }
        .data-table tr:hover td { background: #fafafa; }
        .empty-state        { text-align: center; padding: 32px; color: #9ca3af; font-size: 14px; }

        /* ── Document tab ────────────────────────────────── */
        .doc-row            { display: flex; align-items: center; justify-content: space-between;
                              padding: 12px 0; border-bottom: 1px solid #f3f4f6; gap: 12px; }
        .doc-row:last-child { border-bottom: none; }
        .doc-info           { flex: 1; min-width: 0; }
        .doc-type           { font-size: 14px; font-weight: 600; color: #111827; }
        .doc-meta           { font-size: 12px; color: #6b7280; margin-top: 2px; }
        .doc-btn            { flex-shrink: 0; }
        .doc-viewer         { margin-top: 12px; border: 1px solid #e5e7eb; border-radius: 6px;
                              overflow: hidden; }
        .doc-viewer iframe  { width: 100%; height: 640px; border: none; display: block; }
        .doc-loading        { padding: 24px; text-align: center; color: #6b7280; font-size: 14px; }
        .doc-error          { padding: 16px; background: #fef2f2; color: #991b1b;
                              border-radius: 6px; font-size: 13px; margin-top: 8px; }

        /* ── Misc ────────────────────────────────────────── */
        .lien-amount        { font-weight: 600; color: #991b1b; }
        .status-open        { color: #d97706; font-weight: 600; }
        .status-closed      { color: #6b7280; }
        .gmaps-link         { font-size: 12px; color: #1e40af; text-decoration: none; }
        .gmaps-link:hover   { text-decoration: underline; }
        .back-link          { display: inline-flex; align-items: center; gap: 5px;
                              color: #6b7280; font-size: 13px; text-decoration: none;
                              margin-bottom: 16px; }
        .back-link:hover    { color: #111827; }
        .alert-info         { background: #eff6ff; border: 1px solid #bfdbfe; color: #1e40af;
                              border-radius: 7px; padding: 12px 16px; font-size: 13px;
                              margin-bottom: 16px; }

        /* ── Prospect card ───────────────────────────────── */
        .prospect-card          { border-radius: 10px; padding: 20px 24px;
                                  margin-bottom: 20px; border: 2px solid; }
        .prospect-card.active   { background: #f0fdf4; border-color: #86efac; }
        .prospect-card.inactive { background: #f9fafb; border-color: #e5e7eb; }
        .prospect-card-header   { display: flex; align-items: center;
                                  justify-content: space-between; margin-bottom: 14px; gap: 12px; }
        .prospect-card-title    { display: flex; align-items: center; gap: 10px; }
        .prospect-card-title h3 { font-size: 15px; font-weight: 700; margin: 0;
                                  color: #111827; }
        .prospect-pill          { font-size: 11px; font-weight: 700; padding: 3px 10px;
                                  border-radius: 20px; text-transform: uppercase;
                                  letter-spacing: 0.05em; }
        .prospect-pill.active   { background: #dcfce7; color: #166534; }
        .prospect-pill.inactive { background: #f3f4f6; color: #6b7280; }
        .prospect-card-actions  { display: flex; align-items: center; gap: 8px; }
        .prospect-facts         { display: grid;
                                  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
                                  gap: 12px 20px; }
        .prospect-fact label    { display: block; font-size: 11px; font-weight: 600;
                                  color: #9ca3af; text-transform: uppercase;
                                  letter-spacing: 0.04em; margin-bottom: 2px; }
        .prospect-fact span     { display: block; font-size: 14px; color: #111827;
                                  font-weight: 500; }
        .prospect-fact span.na  { color: #d1d5db; font-style: italic; font-weight: 400; }
        .prospect-status-select { font-size: 13px; padding: 4px 8px; border-radius: 6px;
                                  border: 1px solid #d1d5db; color: #374151;
                                  background: #fff; cursor: pointer; }

        /* ── People tab — checkbox contrast fix ─────────────── */
        #modalPeopleContact .form-check-input {
            border: 2px solid #6b7280;
            background-color: #fff;
        }
        #modalPeopleContact .form-check-input:checked {
            background-color: #1e40af;
            border-color: #1e40af;
        }
        #modalPeopleContact .form-check-label {
            color: #111827;
            font-weight: 500;
        }
    </style>

<asp:HiddenField ID="hdnClip"         runat="server" />
    <asp:HiddenField ID="hdnFipsCode"     runat="server" />
    <asp:HiddenField ID="hdnLat"          runat="server" />
    <asp:HiddenField ID="hdnLng"          runat="server" />
    <asp:HiddenField ID="hdnStreetViewKey" runat="server" />
    <asp:HiddenField ID="hdnDocResult"    runat="server" />

    <div class="detail-wrap">

        <a href="javascript:history.back()" class="btn btn-outline-secondary btn-sm mb-3">
            <i class="ti ti-arrow-left me-1"></i>Back
        </a>

        <%-- Cleave.js for phone mask — loaded from cdnjs --%>
        <script src="https://cdnjs.cloudflare.com/ajax/libs/cleave.js/1.6.0/cleave.min.js"></script>

        <!-- ── Property header ──────────────────────────── -->
        <div class="prop-header" style="align-items:flex-start; flex-wrap:wrap; gap:16px;">

            <%-- Left: street view photo --%>
            <asp:Image ID="imgStreetView" runat="server" CssClass="prop-header-photo"
                       AlternateText="" style="display:none;" />
            <div id="divPhotoPlaceholder" runat="server" class="prop-header-photo-placeholder">
                No street view
            </div>

            <%-- Centre: address + signals + action buttons --%>
            <div class="prop-header-info" style="min-width:200px;">
                <div class="prop-address-line">
                    <asp:Literal ID="litAddress" runat="server" /></div>
                <div class="prop-city-line">
                    <asp:Literal ID="litCityStateZip" runat="server" /></div>
                <div class="prop-signals">
                    <asp:PlaceHolder ID="phSignals" runat="server" />
                </div>
                <div class="prop-header-actions">
                    <asp:HyperLink ID="lnkGoogleMaps" runat="server" CssClass="btn btn-sm btn-outline-secondary"
                                   Target="_blank">
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 13v6a2 2 0 01-2 2H5a2 2 0 01-2-2V8a2 2 0 012-2h6"/><polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/></svg>
                        Street View
                    </asp:HyperLink>
                    <asp:HyperLink ID="lnkBackToComps" runat="server" CssClass="btn btn-sm btn-outline-primary">
                        &#8592; Back to Comps
                    </asp:HyperLink>
                    <asp:LinkButton ID="btnToggleProspect" runat="server" CssClass="btn btn-sm btn-success"
                                    OnClick="btnToggleProspect_Click" />
                    <asp:LinkButton ID="btnRemoveProspect" runat="server" CssClass="btn btn-sm btn-outline-danger"
                                    OnClick="btnToggleProspect_Click" style="display:none;"
                                    OnClientClick="return confirm('Remove this property as a prospect? This cannot be undone.');" />
                    <asp:LinkButton ID="btnRefreshData" runat="server" CssClass="btn btn-sm btn-outline-warning"
                                    OnClick="btnRefreshData_Click"
                                    OnClientClick="return confirm('Re-fetch all data from CoreLogic? This may take a moment.');">
                        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/><path d="M3.51 9a9 9 0 0114.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0020.49 15"/></svg>
                        Refresh Data
                    </asp:LinkButton>
                </div>
            </div>

            <%-- Right: Primary Contact — read-only display from People tab --%>
            <div class="owner-contact-header flex-shrink-0" style="min-width:260px; flex:1; max-width:360px;
                         border-left:1px solid #e5e7eb; padding-left:16px;">

                <asp:HiddenField ID="hdnContactSaved"      runat="server" Value="" />
                <asp:HiddenField ID="hdnOwnershipPostback" runat="server" Value="" />

                <%-- Hidden server-side controls kept for codebehind compatibility --%>
                <asp:LinkButton ID="btnEditContact"   runat="server" style="display:none" OnClick="btnEditContact_Click" />
                <asp:LinkButton ID="btnCancelContact" runat="server" style="display:none" OnClick="btnCancelContact_Click" />
                <asp:LinkButton ID="btnCancelContact2" runat="server" style="display:none" OnClick="btnCancelContact_Click" />
                <asp:LinkButton ID="btnSaveContact"   runat="server" style="display:none" OnClick="btnSaveContact_Click" />
                <asp:TextBox    ID="txtContactName"    runat="server" style="display:none" />
                <asp:TextBox    ID="txtContactPhone"   runat="server" style="display:none" />
                <asp:TextBox    ID="txtContactEmail"   runat="server" style="display:none" />
                <asp:TextBox    ID="txtContactMailing" runat="server" style="display:none" />
                <asp:TextBox    ID="txtContactNotes"   runat="server" style="display:none" />
                <asp:DropDownList ID="ddlPreferredContact" runat="server" style="display:none">
                    <asp:ListItem Value="" /><asp:ListItem Value="Phone" /><asp:ListItem Value="Email" />
                    <asp:ListItem Value="Mail" /><asp:ListItem Value="InPerson" />
                </asp:DropDownList>

                <%-- Header label --%>
                <div class="d-flex align-items-center justify-content-between mb-2">
                    <span class="fs-xs fw-bold text-uppercase text-muted" style="letter-spacing:.05em;">
                        <i class="ti ti-address-book me-1 text-primary"></i>Primary Contact
                    </span>
                    <a href="#" onclick="showTab('people', document.getElementById('tabBtnPeople')); return false;"
                       class="btn btn-xs btn-outline-secondary py-0 px-2" style="font-size:11px;">
                        <i class="ti ti-users me-1"></i>Manage
                    </a>
                </div>

                <%-- No primary contact set --%>
                <asp:Panel ID="pnlContactEmpty" runat="server" Visible="false">
                    <p class="text-muted fst-italic fs-xs mb-1">No primary contact set.</p>
                    <a href="#" onclick="showTab('people', document.getElementById('tabBtnPeople')); return false;"
                       class="btn btn-sm btn-outline-primary btn-xs" style="font-size:11px;">
                        <i class="ti ti-user-plus me-1"></i>Add in People tab
                    </a>
                </asp:Panel>

                <%-- Primary contact data --%>
                <asp:Panel ID="pnlContactData" runat="server">
                    <div style="font-size:13px; line-height:1.7;">
                        <div class="fw-semibold"><asp:Literal ID="litContactName"    runat="server" /></div>
                        <div><asp:Literal ID="litContactPhone"   runat="server" /></div>
                        <div><asp:Literal ID="litContactEmail"   runat="server" /></div>
                        <div class="text-muted fs-xs mt-1"><asp:Literal ID="litContactMailing" runat="server" /></div>
                    </div>

                    <%-- Preferred contact method — saved via Ajax on change --%>
                    <asp:Panel ID="pnlContactNotes" runat="server" CssClass="mt-2">
                        <div class="fs-xs text-muted fw-semibold text-uppercase mb-1" style="letter-spacing:.04em;">Preferred Contact</div>
                        <div class="d-flex align-items-center gap-2">
                            <select id="selPreferredContact" class="form-select form-select-sm flex-fill"
                                    onchange="savePreferredContact(this)">
                                <option value="">-- Select --</option>
                                <option value="Phone">Phone</option>
                                <option value="Email">Email</option>
                                <option value="Mail">Mail</option>
                                <option value="InPerson">In Person</option>
                            </select>
                            <span id="spnPreferredSaved" class="text-success fs-xs" style="display:none;">
                                <i class="ti ti-check"></i>
                            </span>
                        </div>
                        <div class="mt-1"><asp:Literal ID="litPreferredContact" runat="server" /><asp:Literal ID="litContactNotes" runat="server" /></div>
                    </asp:Panel>
                </asp:Panel>

                <%-- Hidden edit panel — kept empty for codebehind compatibility --%>
                <asp:Panel ID="pnlContactRead"      runat="server" Style="display:none" />
                <asp:Panel ID="pnlContactEdit"      runat="server" Style="display:none" />
                <asp:Panel ID="pnlContactSaveAlert" runat="server" Style="display:none">
                    <asp:Literal ID="litContactSaveAlert" runat="server" />
                </asp:Panel>

            </div><%-- /owner-contact-header --%>

        </div><%-- /prop-header --%>

        <!-- ── API diagnostics (shown after fresh pull only) ── -->
        <asp:PlaceHolder ID="phDiagnostics" runat="server" />

        <!-- ══════════════════════════════════════════════
             PROSPECT CARD — above tabs, always visible
        ═══════════════════════════════════════════════ -->
        <asp:PlaceHolder ID="phProspectCard" runat="server" />

        <!-- ── Tabs ─────────────────────────────────────── -->
        <ul class="nav nav-tabs mb-4" id="detailTabs">
            <li class="nav-item">
                <button type="button" class="nav-link active" onclick="showTab('overview',this)">
                    <i class="ti ti-home me-1"></i>Overview
                </button>
            </li>
            <li class="nav-item">
                <button type="button" class="nav-link" id="tabBtnPeople" onclick="showTab('people',this)">
                    <i class="ti ti-users me-1"></i>People
                </button>
            </li>
            <li class="nav-item">
                <button type="button" class="nav-link" id="tabBtnContact" onclick="showTab('contact',this)">
                    <i class="ti ti-notes me-1"></i>Contact History, Deal &amp; Notes
                </button>
            </li>
            <li class="nav-item">
                <button type="button" class="nav-link" onclick="showTab('valuation',this)">
                    <i class="ti ti-calculator me-1"></i>Reyla Valuation
                </button>
            </li>
            <li class="nav-item">
                <button type="button" class="nav-link" onclick="showTab('ownership',this)">
                    <i class="ti ti-users me-1"></i>Ownership &amp; History
                </button>
            </li>
            <li class="nav-item">
                <button type="button" class="nav-link" onclick="showTab('documents',this)">
                    <i class="ti ti-file-text me-1"></i>Documents
                </button>
            </li>
        </ul>

        <!-- Comp toggle postback fields -->
        <asp:HiddenField ID="hdnToggleClip"       runat="server" />
        <asp:HiddenField ID="hdnToggleAction"     runat="server" />
        <asp:HiddenField ID="hdnToggleProspectId" runat="server" />

        <!-- ══════════════════════════════════════════════
             TAB 1 — OVERVIEW
        ═══════════════════════════════════════════════ -->
        <div id="tab-overview" class="tab-panel active">

            <!-- Property facts -->
            <div class="section-card">
                <div class="section-title">Property</div>
                <div class="fact-grid">
                    <div class="fact-item">
                        <label>APN</label>
                        <asp:Literal ID="litApn" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>CLIP</label>
                        <asp:Literal ID="litClip" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Property Use</label>
                        <asp:Literal ID="litPropertyUse" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Zoning</label>
                        <asp:Literal ID="litZoning" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Lot Size</label>
                        <asp:Literal ID="litLotSize" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>County</label>
                        <asp:Literal ID="litCounty" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>FIPS Code</label>
                        <asp:Literal ID="litFips" runat="server" />
                    </div>
                </div>
            </div>

            <!-- Building -->
            <div class="section-card">
                <div class="section-title">Building</div>
                <div class="fact-grid">
                    <div class="fact-item">
                        <label>Building Sq Ft</label>
                        <asp:Literal ID="litBldgSqFt" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Year Built</label>
                        <asp:Literal ID="litYearBuilt" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Effective Year Built</label>
                        <asp:Literal ID="litEffYearBuilt" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Stories</label>
                        <asp:Literal ID="litStories" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Construction</label>
                        <asp:Literal ID="litConstruction" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Roof Type</label>
                        <asp:Literal ID="litRoofType" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Condition</label>
                        <asp:Literal ID="litCondition" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Bedrooms</label>
                        <asp:Literal ID="litBedrooms" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Bathrooms</label>
                        <asp:Literal ID="litBathrooms" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Total Rooms</label>
                        <asp:Literal ID="litTotalRooms" runat="server" />
                    </div>
                </div>
            </div>

            <!-- Valuation -->
            <div class="section-card">
                <div class="section-title">Valuation &amp; Tax</div>
                <%-- CoreLogic required disclaimer — do not remove (MSA SOW III.B.1.a) --%>
                <div class="fact-grid">
                    <div class="fact-item">
                        <label>AVM Estimate</label>
                        <asp:Literal ID="litAvmValue" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>AVM Range</label>
                        <asp:Literal ID="litAvmRange" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>AVM Confidence</label>
                        <asp:Literal ID="litAvmConfidence" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Rent Estimate</label>
                        <asp:Literal ID="litRentEstimate" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Cap Rate</label>
                        <asp:Literal ID="litCapRate" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Estimated Equity</label>
                        <asp:Literal ID="litEquity" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Assessed Value (<asp:Literal ID="litTaxYear" runat="server" />)</label>
                        <asp:Literal ID="litAssessedValue" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Land Value</label>
                        <asp:Literal ID="litLandValue" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Improvement Value</label>
                        <asp:Literal ID="litImprovementValue" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Annual Tax</label>
                        <asp:Literal ID="litTaxAmount" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Tax Delinquent Year</label>
                        <asp:Literal ID="litTaxDelinquent" runat="server" />
                    </div>
                </div>
                <%-- Required CoreLogic valuation disclaimer (MSA SOW Section III.B.1.a) --%>
                <div class="alert alert-secondary py-2 mt-3 mb-0 d-flex align-items-start gap-2" style="font-size:11px;line-height:1.4;">
                    <i class="ti ti-info-circle flex-shrink-0 mt-1" style="font-size:14px;"></i>
                    <span><strong>Disclaimer:</strong> The data and valuations are provided as is without warranty or guarantee of any kind, either express or implied, including without limitation, any warranties of merchantability or fitness for a particular purpose. The existence of the subject property and the accuracy of the valuations are estimated based on available data and do not constitute an appraisal of the subject property and should not be relied upon in lieu of underwriting or an appraisal.</span>
                </div>
            </div>

            <!-- Propensity & signals -->
            <div class="section-card">
                <div class="section-title">Motivated Seller Signals</div>
                <div class="fact-grid">
                    <div class="fact-item">
                        <label>Sale Propensity Score</label>
                        <asp:Literal ID="litPropensityScore" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Propensity Tier</label>
                        <asp:Literal ID="litPropensityTier" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Absentee Owner</label>
                        <asp:Literal ID="litAbsentee" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Involuntary Liens</label>
                        <asp:Literal ID="litInvoluntaryLiens" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Tax Delinquency</label>
                        <asp:Literal ID="litTaxDelinquencySignal" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Open/Expired Permits</label>
                        <asp:Literal ID="litPermitSignal" runat="server" />
                    </div>
                </div>
            </div>

            <!-- HOA -->
            <asp:Panel ID="pnlHoa" runat="server" Visible="false">
                <div class="section-card">
                    <div class="section-title">HOA</div>
                    <div class="fact-grid">
                        <div class="fact-item">
                            <label>HOA Name</label>
                            <asp:Literal ID="litHoaName" runat="server" />
                        </div>
                        <div class="fact-item">
                            <label>Fee</label>
                            <asp:Literal ID="litHoaFee" runat="server" />
                        </div>
                        <div class="fact-item">
                            <label>Frequency</label>
                            <asp:Literal ID="litHoaFrequency" runat="server" />
                        </div>
                        <div class="fact-item">
                            <label>Phone</label>
                            <asp:Literal ID="litHoaPhone" runat="server" />
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <!-- Climate risk -->
            <div class="section-card">
                <div class="section-title">Climate Risk</div>
                <div class="fact-grid">
                    <div class="fact-item">
                        <label>Flood Risk</label>
                        <asp:Literal ID="litFloodRisk" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Fire Risk</label>
                        <asp:Literal ID="litFireRisk" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Wind Risk</label>
                        <asp:Literal ID="litWindRisk" runat="server" />
                    </div>
                    <div class="fact-item">
                        <label>Heat Risk</label>
                        <asp:Literal ID="litHeatRisk" runat="server" />
                    </div>
                </div>
            </div>

            <!-- Building permits -->
            <div class="section-card">
                <div class="section-title">Building Permits</div>
                <asp:PlaceHolder ID="phPermits" runat="server">
                    <table class="data-table">
                        <thead>
                            <tr>
                                <th>Permit #</th>
                                <th>Type</th>
                                <th>Status</th>
                                <th>Issued</th>
                                <th>Completed</th>
                                <th>Value</th>
                                <th>Description</th>
                            </tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptPermits" runat="server">
                                <ItemTemplate>
                                    <tr>
                                        <td><%# Eval("PermitNumber") %></td>
                                        <td><%# Eval("PermitType") %></td>
                                        <td class='<%# IsOpenPermit(Eval("Status")?.ToString()) ? "status-open" : "status-closed" %>'>
                                            <%# Eval("Status") %>
                                        </td>
                                        <td><%# FormatDate(Eval("IssueDate")?.ToString()) %></td>
                                        <td><%# FormatDate(Eval("CompletionDate")?.ToString()) %></td>
                                        <td><%# FormatMoney(Eval("JobValue")) %></td>
                                        <td><%# Eval("Description") %></td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </asp:PlaceHolder>
                <asp:Panel ID="pnlNoPermits" runat="server" Visible="false">
                    <div class="empty-state">No building permits on record</div>
                </asp:Panel>
            </div>

        </div><!-- /tab-overview -->

        <!-- ══════════════════════════════════════════════
             TAB 2 — PEOPLE
        ═══════════════════════════════════════════════ -->
        <div id="tab-people" class="tab-panel">

            <%-- Hidden fields used by JS --%>
            <asp:HiddenField ID="hdnPropertyId"       runat="server" />
            <asp:HiddenField ID="hdnPeopleHandlerUrl" runat="server" />

            <%-- Add / Link Contact Modal --%>
            <div class="modal fade" id="modalPeopleContact" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title fw-bold" id="modalPeopleContactLabel">Add Contact</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">

                            <!-- Search existing contacts first -->
                            <div class="mb-3">
                                <label class="form-label fw-semibold fs-sm">Search Existing Contacts</label>
                                <div class="input-group input-group-sm">
                                    <input type="text" id="txtPeopleSearch" class="form-control"
                                           placeholder="Search by name or email..."
                                           oninput="searchExistingContacts()" />
                                    <span class="input-group-text"><i class="ti ti-search"></i></span>
                                </div>
                                <div id="divSearchResults" class="mt-2" style="display:none;">
                                    <div id="listSearchResults"></div>
                                </div>
                            </div>

                            <div class="d-flex align-items-center gap-2 mb-3">
                                <hr class="flex-fill" />
                                <span class="text-muted fs-xs">or create new</span>
                                <hr class="flex-fill" />
                            </div>

                            <!-- Validation errors -->
                            <div id="divPeopleValidation" class="alert alert-danger d-none mb-3">
                                <ul id="ulPeopleValidation" class="mb-0 ps-3"></ul>
                            </div>

                            <div class="row g-3">
                                <div class="col-12 col-md-6">
                                    <label class="form-label fw-semibold fs-sm">First Name <span class="text-muted fw-normal">(or Last Name required)</span></label>
                                    <input type="text" id="pplFirstName" class="form-control form-control-sm" placeholder="First name" />
                                </div>
                                <div class="col-12 col-md-6">
                                    <label class="form-label fw-semibold fs-sm">Last Name</label>
                                    <input type="text" id="pplLastName" class="form-control form-control-sm" placeholder="Last name" />
                                </div>
                                <div class="col-12">
                                    <label class="form-label fw-semibold fs-sm">Company</label>
                                    <input type="text" id="pplCompany" class="form-control form-control-sm" placeholder="Company name" />
                                </div>
                                <div class="col-12 col-md-6">
                                    <label class="form-label fw-semibold fs-sm">Email</label>
                                    <input type="email" id="pplEmail" class="form-control form-control-sm" placeholder="email@example.com" />
                                </div>
                                <div class="col-12 col-md-6">
                                    <label class="form-label fw-semibold fs-sm">Phone</label>
                                    <input type="text" id="pplPhone" class="form-control form-control-sm" placeholder="(555) 000-0000" />
                                </div>
                                <div class="col-12 col-md-6">
                                    <label class="form-label fw-semibold fs-sm">Mobile</label>
                                    <input type="text" id="pplMobile" class="form-control form-control-sm" placeholder="(555) 000-0000" />
                                </div>
                                <div class="col-12 col-md-6">
                                    <label class="form-label fw-semibold fs-sm">LinkedIn</label>
                                    <input type="url" id="pplLinkedIn" class="form-control form-control-sm" placeholder="https://linkedin.com/in/..." />
                                </div>
                                <div class="col-12">
                                    <label class="form-label fw-semibold fs-sm">Contact Types <span class="text-muted fw-normal">(select all that apply)</span></label>
                                    <div id="divPeopleTypes" class="d-flex flex-wrap gap-2 mt-1"></div>
                                </div>
                                <div class="col-12">
                                    <div class="form-check">
                                        <input class="form-check-input" type="checkbox" id="chkPeopleIsPrimary" />
                                        <label class="form-check-label fs-sm" for="chkPeopleIsPrimary">
                                            Set as primary contact for this property
                                        </label>
                                    </div>
                                </div>
                                <div class="col-12">
                                    <label class="form-label fw-semibold fs-sm">Notes</label>
                                    <textarea id="pplNotes" class="form-control form-control-sm" rows="2" placeholder="Notes about this contact..."></textarea>
                                </div>
                            </div>

                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-primary btn-sm" id="btnSavePeopleContact" onclick="savePeopleContact()">
                                <i class="ti ti-user-plus me-1"></i>Add to Property
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            <%-- People content --%>
            <div id="divPeopleContent">

                <%-- Header row --%>
                <div class="d-flex align-items-center justify-content-between mb-3">
                    <div>
                        <h6 class="mb-0 fw-bold">Property Contacts</h6>
                        <p class="text-muted fs-xs mb-0">Buyers, sellers, owners, and parties involved with this property.</p>
                    </div>
                    <button type="button" class="btn btn-primary btn-sm" onclick="openAddPeopleModal()">
                        <i class="ti ti-user-plus me-1"></i>Add Contact
                    </button>
                </div>

                <%-- Loading --%>
                <div id="divPeopleLoading" class="text-center py-4">
                    <div class="spinner-border text-primary spinner-border-sm"></div>
                    <p class="text-muted fs-xs mt-2 mb-0">Loading contacts...</p>
                </div>

                <%-- Empty state --%>
                <div id="divPeopleEmpty" class="card" style="display:none;">
                    <div class="card-body text-center py-5">
                        <i class="ti ti-users text-muted" style="font-size:2.5rem;"></i>
                        <h6 class="mt-3 mb-1">No Contacts Yet</h6>
                        <p class="text-muted fs-sm mb-3">Add buyers, sellers, owners, or third parties connected to this property.</p>
                        <button type="button" class="btn btn-primary btn-sm" onclick="openAddPeopleModal()">
                            <i class="ti ti-user-plus me-1"></i>Add First Contact
                        </button>
                    </div>
                </div>

                <%-- Contact groups --%>
                <div id="divPeopleGroups" style="display:none;"></div>

            </div>

        </div><%-- /tab-people --%>

        <!-- ══════════════════════════════════════════════
             TAB 2 — REYLA VALUATION
        ═══════════════════════════════════════════════ -->
        <div id="tab-valuation" class="tab-panel">
            <asp:PlaceHolder ID="phCompMgmt" runat="server" />
            <%-- Required CoreLogic valuation disclaimer (MSA SOW Section III.B.1.a) --%>
            <div class="alert alert-secondary py-2 mt-3 d-flex align-items-start gap-2" style="font-size:11px;line-height:1.4;">
                <i class="ti ti-info-circle flex-shrink-0 mt-1" style="font-size:14px;"></i>
                <span><strong>Disclaimer:</strong> The data and valuations are provided as is without warranty or guarantee of any kind, either express or implied, including without limitation, any warranties of merchantability or fitness for a particular purpose. The existence of the subject property and the accuracy of the valuations are estimated based on available data and do not constitute an appraisal of the subject property and should not be relied upon in lieu of underwriting or an appraisal.</span>
            </div>
        </div><!-- /tab-valuation -->

        <!-- ══════════════════════════════════════════════
             TAB 2 — CONTACT HISTORY & NOTES
        ═══════════════════════════════════════════════ -->
        <div id="tab-contact" class="tab-panel">

            <%-- Hidden field carrying prospectId for the JS engine --%>
            <asp:HiddenField ID="hdnNotesProspectId" runat="server" Value="" />

            <%-- Shown only when property is a prospect --%>
            <div id="contactNotesContent" style="display:none">

                <%-- ── Contacted status card ─────────────────────────────── --%>
                <div class="card mb-3">
                    <div class="card-header d-flex align-items-center justify-content-between">
                        <h5 class="card-title mb-0">
                            <i class="ti ti-phone-check me-1 text-success"></i>Contacted Status
                        </h5>
                        <div id="contactedBadgeArea"></div>
                    </div>
                    <div class="card-body">
                        <%-- Contacted form (hidden until Mark as Contacted clicked) --%>
                        <div id="contactedFormPanel" style="display:none" class="mb-3">
                            <div class="row g-2 align-items-end">
                                <div class="col-12 col-sm-4">
                                    <label class="form-label fw-semibold mb-1">Date Contacted</label>
                                    <input type="date" id="inpContactedDate" class="form-control form-control-sm" />
                                </div>
                                <div class="col-12 col-sm-6">
                                    <label class="form-label fw-semibold mb-1">Notes (optional)</label>
                                    <input type="text" id="inpContactedNotes" class="form-control form-control-sm"
                                           placeholder="e.g. Left voicemail, sent letter" maxlength="500" />
                                </div>
                                <div class="col-12 col-sm-2 d-flex gap-2">
                                    <button type="button" class="btn btn-success btn-sm" onclick="saveContacted()">
                                        <i class="ti ti-check me-1"></i>Save
                                    </button>
                                    <button type="button" class="btn btn-outline-secondary btn-sm"
                                            onclick="toggleContactedForm(false)">
                                        Cancel
                                    </button>
                                </div>
                            </div>
                            <div id="contactedAlert" class="mt-2" style="display:none"></div>
                        </div>
                        <%-- Status body — rendered by renderContacted() --%>
                        <div id="contactedBody"></div>
                    </div>
                </div>

                <%-- ── Outreach Notes card ────────────────────────────────── --%>
                <div class="card mb-3">
                    <div class="card-header">
                        <h5 class="card-title mb-0">
                            <i class="ti ti-notes me-1 text-primary"></i>Outreach Notes
                        </h5>
                    </div>
                    <div class="card-body">

                        <%-- Add note --%>
                        <div class="mb-3">
                            <div class="input-group">
                                <textarea id="txtNewNote" class="form-control" rows="2"
                                    placeholder="Add an outreach note… e.g. Called owner, left message"
                                    maxlength="2000"></textarea>
                                <button type="button" class="btn btn-primary" onclick="addNote()">
                                    <i class="ti ti-plus me-1"></i>Add
                                </button>
                            </div>
                            <div class="form-text">Each entry is a new note. Notes are private to your organization.</div>
                            <div id="noteAlert" class="mt-2" style="display:none"></div>
                        </div>

                        <%-- Notes list — rebuilt by JS after every operation --%>
                        <div id="notesList"></div>

                    </div>
                </div>

                <%-- ── Deal Notes card — only shown when a deal exists ────── --%>
                <div class="card mb-3" id="dealNotesCard" style="display:none">
                    <div class="card-header d-flex align-items-center justify-content-between">
                        <h5 class="card-title mb-0">
                            <i class="ti ti-briefcase me-1 text-warning"></i>Deal Notes
                        </h5>
                        <span class="badge text-bg-warning text-dark fs-xs" id="dealStageInNotes"></span>
                    </div>
                    <div class="card-body">
                        <div class="mb-3">
                            <div class="input-group">
                                <textarea id="txtNewDealNote" class="form-control" rows="2"
                                    placeholder="Add a deal note… e.g. Seller countered at $850K"
                                    maxlength="2000"></textarea>
                                <button type="button" class="btn btn-warning" onclick="addDealNoteFromDetail()">
                                    <i class="ti ti-plus me-1"></i>Add
                                </button>
                            </div>
                            <div class="form-text">Deal notes are visible to your whole organization.</div>
                            <div id="dealNoteAlert" class="mt-2" style="display:none"></div>
                        </div>
                        <div id="dealNotesList"></div>
                    </div>
                </div>

            </div><%-- /contactNotesContent --%>

            <%-- Shown when property is NOT a prospect --%>
            <div id="contactNotesEmpty" style="display:none">
                <div class="card">
                    <div class="card-body text-center py-5">
                        <i class="ti ti-notes text-muted" style="font-size:2.5rem"></i>
                        <h5 class="mt-3 mb-1">Not a Prospect Yet</h5>
                        <p class="text-muted fs-sm mb-0">
                            Add this property as a prospect to start tracking outreach notes.
                        </p>
                    </div>
                </div>
            </div>

        </div><%-- /tab-contact --%>

        <!-- ══════════════════════════════════════════════
             TAB 3 — OWNERSHIP & HISTORY
        ═══════════════════════════════════════════════ -->
        <div id="tab-ownership" class="tab-panel">

            <%-- Notes section removed — now lives in Contact History & Notes tab --%>

            <%-- ── Owner of Record (CoreLogic) ─────────────────────────────── --%>
            <div class="card mb-3">
                <div class="card-header">
                    <h5 class="card-title mb-0">
                        <i class="ti ti-user me-1 text-muted"></i>Owner of Record
                        <span class="badge text-bg-secondary ms-2" style="font-size:10px">CoreLogic</span>
                    </h5>
                </div>
                <div class="card-body">
                    <div class="row g-3">
                        <div class="col-12 col-md-4">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">Owner Name</div>
                            <asp:Literal ID="litOwnerName" runat="server" />
                        </div>
                        <div class="col-12 col-md-4">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">Owner 1</div>
                            <asp:Literal ID="litOwner1" runat="server" />
                        </div>
                        <div class="col-12 col-md-4">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">Owner 2</div>
                            <asp:Literal ID="litOwner2" runat="server" />
                        </div>
                        <div class="col-12 col-md-4">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">Ownership Type</div>
                            <asp:Literal ID="litOwnershipType" runat="server" />
                        </div>
                        <div class="col-12 col-md-4">
                            <div class="text-muted fs-xs text-uppercase fw-semibold mb-1">Absentee Owner</div>
                            <asp:Literal ID="litAbsenteeOwner" runat="server" />
                        </div>
                    </div>
                    <div class="mt-3">
                        <div class="text-muted fs-xs text-uppercase fw-semibold mb-2">Mailing Address (Record)</div>
                        <div class="row g-2">
                            <div class="col-12 col-md-6">
                                <asp:Literal ID="litMailStreet" runat="server" />
                            </div>
                            <div class="col-12 col-md-6">
                                <asp:Literal ID="litMailCityStateZip" runat="server" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <%-- ── Ownership transfers ──────────────────────────────────────── --%>
            <div class="card mb-3">
                <div class="card-header">
                    <h5 class="card-title mb-0"><i class="ti ti-arrows-exchange me-1 text-muted"></i>Ownership Transfers</h5>
                </div>
                <div class="card-body p-0">
                    <asp:PlaceHolder ID="phTransfers" runat="server">
                        <div class="table-responsive">
                            <table class="table table-sm table-hover align-middle mb-0">
                                <thead class="text-uppercase fs-xxs">
                                    <tr>
                                        <th>Date</th><th>Buyer</th><th>Seller</th>
                                        <th class="text-end">Sale Amount</th>
                                        <th>Deed Type</th><th>Doc #</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptTransfers" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td><%# FormatDate(Eval("SaleDate")?.ToString()) %></td>
                                                <td><%# Eval("BuyerName") %></td>
                                                <td><%# Eval("SellerName") %></td>
                                                <td class="text-end"><%# FormatMoney(Eval("SaleAmount")) %></td>
                                                <td><%# Eval("DeedType") %></td>
                                                <td><%# Eval("DocumentNumber") %></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </asp:PlaceHolder>
                    <asp:Panel ID="pnlNoTransfers" runat="server" Visible="false">
                        <div class="text-muted fst-italic fs-sm p-3">No transfer history on record.</div>
                    </asp:Panel>
                </div>
            </div>

            <%-- ── Mortgage history ────────────────────────────────────────── --%>
            <div class="card mb-3">
                <div class="card-header">
                    <h5 class="card-title mb-0"><i class="ti ti-building-bank me-1 text-muted"></i>Mortgage History</h5>
                </div>
                <div class="card-body p-0">
                    <asp:PlaceHolder ID="phMortgages" runat="server">
                        <div class="table-responsive">
                            <table class="table table-sm table-hover align-middle mb-0">
                                <thead class="text-uppercase fs-xxs">
                                    <tr>
                                        <th>Origination</th><th>Lender</th>
                                        <th class="text-end">Amount</th>
                                        <th>Type</th><th>Rate</th><th>Position</th><th>Maturity</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptMortgages" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td><%# FormatDate(Eval("OriginationDate")?.ToString()) %></td>
                                                <td><%# Eval("LenderName") %></td>
                                                <td class="text-end"><%# FormatMoney(Eval("LoanAmount")) %></td>
                                                <td><%# Eval("LoanType") %></td>
                                                <td><%# FormatRate(Eval("InterestRate")) %></td>
                                                <td><%# Eval("LoanPosition") %></td>
                                                <td><%# FormatDate(Eval("MaturityDate")?.ToString()) %></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </asp:PlaceHolder>
                    <asp:Panel ID="pnlNoMortgages" runat="server" Visible="false">
                        <div class="text-muted fst-italic fs-sm p-3">No mortgage history on record.</div>
                    </asp:Panel>
                </div>
            </div>

            <%-- ── Voluntary liens ─────────────────────────────────────────── --%>
            <div class="card mb-3">
                <div class="card-header">
                    <h5 class="card-title mb-0"><i class="ti ti-link me-1 text-muted"></i>Voluntary Liens</h5>
                </div>
                <div class="card-body p-0">
                    <asp:PlaceHolder ID="phVolLiens" runat="server">
                        <div class="table-responsive">
                            <table class="table table-sm table-hover align-middle mb-0">
                                <thead class="text-uppercase fs-xxs">
                                    <tr>
                                        <th>Recording Date</th><th>Lender</th>
                                        <th class="text-end">Amount</th>
                                        <th>Type</th><th>Doc #</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptVolLiens" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td><%# FormatDate(Eval("RecordingDate")?.ToString()) %></td>
                                                <td><%# Eval("LenderName") %></td>
                                                <td class="text-end"><%# FormatMoney(Eval("LienAmount")) %></td>
                                                <td><%# Eval("LienType") %></td>
                                                <td><%# Eval("DocumentNumber") %></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </asp:PlaceHolder>
                    <asp:Panel ID="pnlNoVolLiens" runat="server" Visible="false">
                        <div class="text-muted fst-italic fs-sm p-3">No voluntary liens on record.</div>
                    </asp:Panel>
                </div>
            </div>

            <%-- ── Involuntary liens ───────────────────────────────────────── --%>
            <div class="card mb-3">
                <div class="card-header">
                    <h5 class="card-title mb-0"><i class="ti ti-alert-triangle me-1 text-danger"></i>Involuntary Liens</h5>
                </div>
                <div class="card-body p-0">
                    <asp:PlaceHolder ID="phInvLiens" runat="server">
                        <div class="table-responsive">
                            <table class="table table-sm table-hover align-middle mb-0">
                                <thead class="text-uppercase fs-xxs">
                                    <tr>
                                        <th>Recording Date</th><th>Creditor</th>
                                        <th class="text-end">Amount</th>
                                        <th>Type</th><th>Release Date</th><th>Doc #</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptInvLiens" runat="server">
                                        <ItemTemplate>
                                            <tr>
                                                <td><%# FormatDate(Eval("RecordingDate")?.ToString()) %></td>
                                                <td><%# Eval("CreditorName") %></td>
                                                <td class="text-end"><%# FormatMoney(Eval("LienAmount")) %></td>
                                                <td><%# Eval("LienType") %></td>
                                                <td><%# FormatDate(Eval("ReleaseDate")?.ToString()) %></td>
                                                <td><%# Eval("DocumentNumber") %></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </asp:PlaceHolder>
                    <asp:Panel ID="pnlNoInvLiens" runat="server" Visible="false">
                        <div class="text-muted fst-italic fs-sm p-3">No involuntary liens on record.</div>
                    </asp:Panel>
                </div>
            </div>

        </div><%-- /tab-ownership --%>


        <!-- ══════════════════════════════════════════════
             TAB 3 — DOCUMENTS
        ═══════════════════════════════════════════════ -->
        <div id="tab-documents" class="tab-panel">

            <div class="alert-info">
                Documents are fetched on demand from CoreLogic's recorded document image archive.
                Click <strong>Fetch PDF</strong> on any row to retrieve it. Coverage varies by county.
            </div>

            <div class="section-card">
                <div class="section-title">Recorded Documents</div>
                <asp:Panel ID="pnlDocs" runat="server">
                    <asp:Repeater ID="rptDocs" runat="server" OnItemCommand="rptDocs_ItemCommand">
                        <ItemTemplate>
                            <div class="doc-row" id='doc-row-<%# Eval("RowKey") %>'>
                                <div class="doc-info">
                                    <div class="doc-type"><%# Eval("DocumentTypeDescription") ?? Eval("DocumentType") %></div>
                                    <div class="doc-meta">
                                        <%# FormatDate(Eval("RecordingDate")?.ToString()) %>
                                        &nbsp;&middot;&nbsp; Doc # <%# Eval("DocumentNumber") %>
                                        <%# !string.IsNullOrEmpty(Eval("BuyerName")?.ToString())
                                              ? " &nbsp;&middot;&nbsp; Buyer: " + Eval("BuyerName") : "" %>
                                        <%# !string.IsNullOrEmpty(Eval("SellerName")?.ToString())
                                              ? " &nbsp;&middot;&nbsp; Seller: " + Eval("SellerName") : "" %>
                                        <%# !string.IsNullOrEmpty(Eval("LenderName")?.ToString())
                                              ? " &nbsp;&middot;&nbsp; Lender: " + Eval("LenderName") : "" %>
                                    </div>
                                </div>
                                <div class="doc-btn">
                                    <asp:LinkButton runat="server" CssClass="btn btn-sm btn-outline-primary"
                                                    CommandName="FetchDoc"
                                                    CommandArgument='<%# Eval("RowKey") %>'>
                                        Fetch PDF
                                    </asp:LinkButton>
                                </div>
                            </div>
                            <!-- Document viewer injected here by code-behind on postback -->
                        </ItemTemplate>
                    </asp:Repeater>
                </asp:Panel>
                <asp:Panel ID="pnlNoDocs" runat="server" Visible="false">
                    <div class="empty-state">No recorded documents found for this property</div>
                </asp:Panel>
            </div>

            <!-- Fetched doc viewer (populated on postback) -->
            <asp:Panel ID="pnlDocViewer" runat="server" Visible="false">
                <div class="section-card">
                    <div class="section-title">
                        Document: <asp:Literal ID="litDocTitle" runat="server" />
                    </div>
                    <asp:Panel ID="pnlDocError" runat="server" Visible="false">
                        <div class="doc-error">
                            <asp:Literal ID="litDocError" runat="server" />
                        </div>
                    </asp:Panel>
                    <asp:Panel ID="pnlDocPages" runat="server" Visible="false">
                        <asp:Repeater ID="rptDocPages" runat="server">
                            <ItemTemplate>
                                <div class="doc-viewer">
                                    <iframe src='<%# "data:application/pdf;base64," + Container.DataItem %>'
                                            title="Document page"></iframe>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                    </asp:Panel>
                </div>
            </asp:Panel>

        </div><!-- /tab-documents -->

    </div><!-- /detail-wrap -->

    <script>
		function showTab(name, btn) {
			document.querySelectorAll('.tab-panel').forEach(function (p) {
				p.classList.remove('active');
			});
			document.querySelectorAll('#detailTabs .nav-link').forEach(function (b) {
				b.classList.remove('active');
			});
			document.getElementById('tab-' + name).classList.add('active');
			btn.classList.add('active');

			// Lazy-load People tab on first open
			if (name === 'people' && typeof initPeopleTab === 'function') initPeopleTab();
		}

        // ── Notes engine — all fetch, no postback ─────────────────────────

        var NOTES_PROSPECT_ID = document.getElementById('<%= hdnNotesProspectId.ClientID %>') ?
            document.getElementById('<%= hdnNotesProspectId.ClientID %>').value : '';

        var DEAL_ID = null; // set by BindNotes() startup script

        // ── Deal notes from PropertyDetail (uses DealToggle.ashx) ────────

        function renderDealNotesFromServer(notes) {
            var card  = document.getElementById('dealNotesCard');
            var label = document.getElementById('dealStageInNotes');
            if (!DEAL_ID) { if (card) card.style.display = 'none'; return; }
            if (card) card.style.display = '';
            renderDealNotesList(notes);
        }

        function renderDealNotesList(notes) {
            var list = document.getElementById('dealNotesList');
            if (!list) return;
            if (!notes || notes.length === 0) {
                list.innerHTML = '<p class="text-muted fst-italic fs-sm mb-0">No deal notes yet.</p>';
                return;
            }
            var h = '';
            notes.forEach(function(n) {
                var edited = n.isEdited ? '<span class="ms-1 text-muted">(edited)</span>' : '';
                h += '<div class="border rounded p-3 mb-2" id="dealNote_' + n.noteId + '">';
                h += '<div class="deal-note-read">';
                h += '<div class="d-flex align-items-start justify-content-between gap-2">';
                h += '<div class="flex-grow-1">';
                h += '<div class="fs-sm" style="white-space:pre-wrap">' + escHtml(n.noteText) + '</div>';
                h += '<div class="text-muted fs-xs mt-1">';
                h += '<i class="ti ti-user me-1"></i>' + escHtml(n.authorName || 'Unknown');
                h += ' &nbsp;·&nbsp; <i class="ti ti-clock me-1"></i>' + escHtml(n.createdLocal);
                h += edited + '</div></div>';
                h += '<div class="d-flex gap-1 flex-shrink-0">';
                h += '<button type="button" class="btn btn-sm btn-outline-secondary py-0 px-2" onclick="toggleDealNoteEdit(\'' + n.noteId + '\')">';
                h += '<i class="ti ti-pencil"></i></button>';
                h += '<button type="button" class="btn btn-sm btn-outline-danger py-0 px-2" onclick="deleteDealNoteFromDetail(\'' + n.noteId + '\')">';
                h += '<i class="ti ti-trash"></i></button>';
                h += '</div></div></div>';
                h += '<div class="deal-note-edit mt-2" style="display:none">';
                h += '<textarea id="dealEditTxt_' + n.noteId + '" class="form-control form-control-sm mb-2" rows="3" maxlength="2000">' + escHtml(n.noteText) + '</textarea>';
                h += '<div class="d-flex gap-2">';
                h += '<button type="button" class="btn btn-sm btn-primary" onclick="saveDealNoteFromDetail(\'' + n.noteId + '\')">';
                h += '<i class="ti ti-device-floppy me-1"></i>Save</button>';
                h += '<button type="button" class="btn btn-sm btn-outline-secondary" onclick="toggleDealNoteEdit(\'' + n.noteId + '\')">Cancel</button>';
                h += '</div></div>';
                h += '</div>';
            });
            list.innerHTML = h;
        }

        function dealNoteFetch(action, params, callback) {
            if (!DEAL_ID) return;
            var fd = new FormData();
            fd.append('action',  action);
            fd.append('dealId',  DEAL_ID);
            Object.keys(params).forEach(function(k) { if (params[k]) fd.append(k, params[k]); });
            fetch('/Secure/Prospects/DealToggle.ashx', { method: 'POST', body: fd, credentials: 'same-origin' })
                .then(function(r) { return r.json(); })
                .then(function(data) {
                    if (!data.success) { showDealNoteAlert(data.errorMessage, 'danger'); return; }
                    if (data.notes) renderDealNotesList(data.notes);
                    if (callback) callback(data);
                })
                .catch(function(e) { showDealNoteAlert('Request failed: ' + e.message, 'danger'); });
        }

        function addDealNoteFromDetail() {
            var ta  = document.getElementById('txtNewDealNote');
            var val = ta ? ta.value.trim() : '';
            if (!val) { showDealNoteAlert('Please enter a note.', 'warning'); return; }
            dealNoteFetch('addDealNote', { noteText: val }, function() {
                ta.value = '';
                hideDealNoteAlert();
            });
        }

        function saveDealNoteFromDetail(noteId) {
            var ta  = document.getElementById('dealEditTxt_' + noteId);
            var val = ta ? ta.value.trim() : '';
            if (!val) return;
            dealNoteFetch('editDealNote', { noteId: noteId, noteText: val }, function() {
                toggleDealNoteEdit(noteId);
            });
        }

        function deleteDealNoteFromDetail(noteId) {
            if (!confirm('Delete this deal note?')) return;
            dealNoteFetch('deleteDealNote', { noteId: noteId });
        }

        function toggleDealNoteEdit(noteId) {
            var item = document.getElementById('dealNote_' + noteId);
            if (!item) return;
            var rv = item.querySelector('.deal-note-read');
            var ev = item.querySelector('.deal-note-edit');
            var editing = ev.style.display !== 'none';
            rv.style.display = editing ? '' : 'none';
            ev.style.display = editing ? 'none' : '';
        }

        function showDealNoteAlert(msg, type) {
            var el = document.getElementById('dealNoteAlert');
            if (!el) return;
            el.innerHTML = '<div class="alert alert-' + type + ' py-2">' + escHtml(msg) + '</div>';
            el.style.display = '';
        }
        function hideDealNoteAlert() {
            var el = document.getElementById('dealNoteAlert');
            if (el) { el.style.display = 'none'; el.innerHTML = ''; }
        }

        function notesFetch(action, extra, callback) {
            var fd = new FormData();
            fd.append('prospectId', NOTES_PROSPECT_ID);
            fd.append('action', action);
            if (extra) Object.keys(extra).forEach(function(k) { fd.append(k, extra[k]); });
            fetch('/Secure/Prospects/NoteToggle.ashx', { method: 'POST', body: fd, credentials: 'same-origin' })
                .then(function(r) { return r.json(); })
                .then(function(data) {
                    if (!data.success) { showNoteAlert(data.errorMessage, 'danger'); return; }
                    renderNotes(data.notes);
                    renderContacted(data.contacted);
                    if (callback) callback(data);
                })
                .catch(function(e) { showNoteAlert('Request failed: ' + e.message, 'danger'); });
        }

        function addNote() {
            var txt = document.getElementById('txtNewNote');
            var val = txt ? txt.value.trim() : '';
            if (!val) { showNoteAlert('Please enter a note.', 'warning'); return; }
            notesFetch('addNote', { noteText: val }, function() {
                txt.value = '';
                hideNoteAlert();
            });
        }

        function editNote(noteId) {
            var ta = document.getElementById('editTxt_' + noteId);
            var val = ta ? ta.value.trim() : '';
            if (!val) return;
            notesFetch('editNote', { noteId: noteId, noteText: val });
        }

        function deleteNote(noteId) {
            if (!confirm('Delete this note?')) return;
            notesFetch('deleteNote', { noteId: noteId });
        }

        function toggleContactedForm(show) {
            var panel = document.getElementById('contactedFormPanel');
            if (!panel) return;
            var visible = show !== undefined ? show : panel.style.display === 'none';
            panel.style.display = visible ? '' : 'none';
            if (visible) {
                var inp = document.getElementById('inpContactedDate');
                var inpNotes = document.getElementById('inpContactedNotes');
                // If already contacted, pre-fill from current badge data
                var badge = document.getElementById('contactedBadgeArea');
                if (inp && !inp.value) {
                    var today = new Date();
                    inp.value = today.toISOString().substring(0, 10);
                }
                // Pre-fill notes from current contactedBody if editing
                if (inpNotes && !inpNotes.value && window._lastContactedNotes) {
                    inpNotes.value = window._lastContactedNotes;
                }
            }
        }

        function saveContacted() {
            var d = document.getElementById('inpContactedDate').value;
            var n = document.getElementById('inpContactedNotes').value;
            if (!d) { showContactedAlert('Please select a date.', 'warning'); return; }
            notesFetch('setContacted', { contactedDate: d, contactedNotes: n }, function() {
                toggleContactedForm(false);
                hideContactedAlert();
            });
        }

        function clearContacted() {
            if (!confirm('Clear contacted date?')) return;
            notesFetch('clearContacted', {});
        }

        // ── Render helpers ────────────────────────────────────────────────

        function renderContacted(c) {
            var area = document.getElementById('contactedBadgeArea');
            var body = document.getElementById('contactedBody');
            if (!area) return;
            window._lastContactedNotes = (c && c.contactedNotes) ? c.contactedNotes : '';

            if (c && c.isContacted) {
                // Header badge + clear button
                area.innerHTML =
                    '<span class="badge text-bg-success me-2">' +
                    '<i class="ti ti-phone-check me-1"></i>Contacted ' + escHtml(c.dateDisplay) + '</span>' +
                    '<button type="button" class="btn btn-sm btn-outline-secondary me-1" onclick="toggleContactedForm(true)">' +
                    '<i class="ti ti-pencil me-1"></i>Edit</button>' +
                    '<button type="button" class="btn btn-sm btn-outline-danger" onclick="clearContacted()">' +
                    '<i class="ti ti-x me-1"></i>Clear</button>';

                // Card body — show notes if any, otherwise confirmation message
                if (body) {
                    if (c.contactedNotes) {
                        body.innerHTML =
                            '<div class="d-flex align-items-start gap-2">' +
                            '<i class="ti ti-message-circle text-muted mt-1 flex-shrink-0"></i>' +
                            '<div><div class="fw-semibold fs-xs text-uppercase text-muted mb-1">Contact Notes</div>' +
                            '<div class="fs-sm" style="white-space:pre-wrap">' + escHtml(c.contactedNotes) + '</div>' +
                            '</div></div>';
                    } else {
                        body.innerHTML =
                            '<p class="text-muted fs-sm mb-0">' +
                            '<i class="ti ti-check-circle me-1 text-success"></i>' +
                            'Owner contacted on ' + escHtml(c.dateDisplay) + '. No additional notes.</p>';
                    }
                }
                // Hide the form
                var fp = document.getElementById('contactedFormPanel');
                if (fp) fp.style.display = 'none';

            } else {
                // Not contacted — show Mark as Contacted button in header
                area.innerHTML =
                    '<button type="button" class="btn btn-sm btn-outline-success" onclick="toggleContactedForm(true)">' +
                    '<i class="ti ti-phone-check me-1"></i>Mark as Contacted</button>';

                // Card body — prompt
                if (body) {
                    body.innerHTML =
                        '<p class="text-muted fs-sm mb-0">' +
                        '<i class="ti ti-phone-off me-1"></i>' +
                        'Owner has not been contacted yet. Click <strong>Mark as Contacted</strong> to log the date.</p>';
                }
            }
        }

        function renderNotes(notes) {
            var list = document.getElementById('notesList');
            if (!list) return;
            if (!notes || notes.length === 0) {
                list.innerHTML = '<p class="text-muted fst-italic fs-sm mb-0">No outreach notes yet. Add your first note above.</p>';
                return;
            }
            var h = '';
            notes.forEach(function(n) {
                var edited = n.isEdited
                    ? '<span class="ms-1 text-muted">(edited ' + escHtml(n.updatedLocal) + ')</span>' : '';
                h += '<div class="border rounded p-3 mb-2" id="note_' + n.noteId + '">';
                // Read view
                h += '<div class="note-read-view">';
                h += '<div class="d-flex align-items-start justify-content-between gap-2">';
                h += '<div class="flex-grow-1">';
                h += '<div class="fs-sm" style="white-space:pre-wrap">' + escHtml(n.noteText) + '</div>';
                h += '<div class="text-muted fs-xs mt-1">';
                h += '<i class="ti ti-user me-1"></i>' + escHtml(n.authorName || 'Unknown');
                h += ' &nbsp;·&nbsp; <i class="ti ti-clock me-1"></i>' + escHtml(n.createdLocal);
                h += edited + '</div></div>';
                h += '<div class="d-flex gap-1 flex-shrink-0">';
                h += '<button type="button" class="btn btn-sm btn-outline-secondary py-0 px-2" onclick="toggleNoteEdit(\'' + n.noteId + '\')">';
                h += '<i class="ti ti-pencil"></i></button>';
                h += '<button type="button" class="btn btn-sm btn-outline-danger py-0 px-2" onclick="deleteNote(\'' + n.noteId + '\')">';
                h += '<i class="ti ti-trash"></i></button>';
                h += '</div></div></div>'; // /d-flex /flex-grow-1 /note-read-view
                // Edit view
                h += '<div class="note-edit-view mt-2" style="display:none">';
                h += '<textarea id="editTxt_' + n.noteId + '" class="form-control form-control-sm mb-2" rows="3" maxlength="2000">' + escHtml(n.noteText) + '</textarea>';
                h += '<div class="d-flex gap-2">';
                h += '<button type="button" class="btn btn-sm btn-primary" onclick="editNote(\'' + n.noteId + '\')">';
                h += '<i class="ti ti-device-floppy me-1"></i>Save</button>';
                h += '<button type="button" class="btn btn-sm btn-outline-secondary" onclick="toggleNoteEdit(\'' + n.noteId + '\')">Cancel</button>';
                h += '</div></div>';
                h += '</div>'; // /note item
            });
            list.innerHTML = h;
        }

        function toggleNoteEdit(noteId) {
            var item = document.getElementById('note_' + noteId);
            if (!item) return;
            var rv = item.querySelector('.note-read-view');
            var ev = item.querySelector('.note-edit-view');
            var editing = ev.style.display !== 'none';
            rv.style.display = editing ? '' : 'none';
            ev.style.display = editing ? 'none' : '';
        }

        function showNoteAlert(msg, type) {
            var el = document.getElementById('noteAlert');
            if (!el) return;
            el.innerHTML = '<div class="alert alert-' + type + ' py-2">' + escHtml(msg) + '</div>';
            el.style.display = '';
        }
        function hideNoteAlert() {
            var el = document.getElementById('noteAlert');
            if (el) { el.style.display = 'none'; el.innerHTML = ''; }
        }
        function showContactedAlert(msg, type) {
            var el = document.getElementById('contactedAlert');
            if (!el) return;
            el.innerHTML = '<div class="alert alert-' + type + ' py-2">' + escHtml(msg) + '</div>';
            el.style.display = '';
        }
        function hideContactedAlert() {
            var el = document.getElementById('contactedAlert');
            if (el) { el.style.display = 'none'; el.innerHTML = ''; }
        }
        function escHtml(s) {
            if (!s) return '';
            return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
        }

        // ── Phone mask & email validation ─────────────────────────────────

        function initContactMasks() {
            var phoneEl = document.getElementById('<%= txtContactPhone.ClientID %>');
            var emailEl = document.getElementById('<%= txtContactEmail.ClientID %>');

            if (phoneEl && !phoneEl._maskDone) {
                phoneEl._maskDone = true;
                phoneEl.addEventListener('input', function() {
                    var digits = phoneEl.value.replace(/\D/g, '').substring(0, 10);
                    var out = '';
                    if (digits.length === 0)      out = '';
                    else if (digits.length <= 3)  out = '(' + digits;
                    else if (digits.length <= 6)  out = '(' + digits.substring(0,3) + ') ' + digits.substring(3);
                    else out = '(' + digits.substring(0,3) + ') ' + digits.substring(3,6) + '-' + digits.substring(6);
                    phoneEl.value = out;
                });
                phoneEl.addEventListener('keydown', function(e) {
                    var allow = ['Backspace','Delete','ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Tab','Home','End'];
                    if (allow.indexOf(e.key) >= 0) return;
                    if (!/^\d$/.test(e.key)) e.preventDefault();
                });
                phoneEl.addEventListener('blur', function() {
                    var digits = phoneEl.value.replace(/\D/g, '');
                    if (digits.length > 0 && digits.length < 10) { phoneEl.classList.add('is-invalid'); }
                    else { phoneEl.classList.remove('is-invalid'); }
                });
                phoneEl.addEventListener('focus', function() { phoneEl.classList.remove('is-invalid'); });
            }

            if (emailEl && !emailEl._validateDone) {
                emailEl._validateDone = true;
                emailEl.addEventListener('blur', function() {
                    var val = emailEl.value.trim();
                    if (val && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val))
                        emailEl.classList.add('is-invalid');
                    else emailEl.classList.remove('is-invalid');
                });
                emailEl.addEventListener('focus', function() { emailEl.classList.remove('is-invalid'); });
            }
        }

        function contactEdit() {
            document.getElementById('<%= pnlContactRead.ClientID %>').style.display = 'none';
            document.getElementById('<%= pnlContactEdit.ClientID %>').style.display = '';
            document.getElementById('btnEditContactJs').style.display  = 'none';
            document.getElementById('btnCancelContactJs').style.display = '';
            initContactMasks();
        }

        function contactCancel() {
            document.getElementById('<%= pnlContactEdit.ClientID %>').style.display = 'none';
            document.getElementById('<%= pnlContactRead.ClientID %>').style.display = '';
            document.getElementById('btnCancelContactJs').style.display = 'none';
            document.getElementById('btnEditContactJs').style.display  = '';
        }

        // ── Tab deep-link + doc viewer restore ────────────────────────────
        (function() {
            var viewer = document.getElementById('<%= pnlDocViewer.ClientID %>');
            if (viewer && viewer.style.display !== 'none') {
                // Documents tab is index 4 now (contact tab added)
                document.querySelectorAll('#detailTabs .nav-link')[4].click();
                return;
            }

            var qs = window.location.search;
            if (qs.indexOf('tab=ownership') >= 0) {
                var btn = document.querySelector('#detailTabs .nav-link[onclick*="ownership"]');
                if (btn) btn.click();
            } else if (qs.indexOf('tab=contact') >= 0) {
                var btn2 = document.getElementById('tabBtnContact');
                if (btn2) btn2.click();
            }
        })();

        // ── Contact Save postback tab restore ─────────────────────────────
        (function() {
            var ownershipFlag = document.getElementById('<%= hdnOwnershipPostback.ClientID %>');
            if (ownershipFlag && ownershipFlag.value === '1') {
                var btn = document.querySelector('#detailTabs .nav-link[onclick*="ownership"]');
                if (btn) btn.click();
                ownershipFlag.value = '';
            }
        })();

        // ── Phone mask & email validation ─────────────────────────────────

        function initContactMasks() {
            var phoneEl = document.getElementById('<%= txtContactPhone.ClientID %>');
            var emailEl = document.getElementById('<%= txtContactEmail.ClientID %>');

            if (phoneEl && !phoneEl._maskDone) {
                phoneEl._maskDone = true;

                // Format as (###) ###-#### while typing
                phoneEl.addEventListener('input', function(e) {
                    var digits = phoneEl.value.replace(/\D/g, '').substring(0, 10);
                    var out = '';
                    if (digits.length === 0) {
                        out = '';
                    } else if (digits.length <= 3) {
                        out = '(' + digits;
                    } else if (digits.length <= 6) {
                        out = '(' + digits.substring(0, 3) + ') ' + digits.substring(3);
                    } else {
                        out = '(' + digits.substring(0, 3) + ') ' +
                              digits.substring(3, 6) + '-' +
                              digits.substring(6);
                    }
                    phoneEl.value = out;
                });

                // Block non-numeric keys (allow control keys)
                phoneEl.addEventListener('keydown', function(e) {
                    var allow = ['Backspace','Delete','ArrowLeft','ArrowRight','ArrowUp',
                                 'ArrowDown','Tab','Home','End'];
                    if (allow.indexOf(e.key) >= 0) return;
                    if (!/^\d$/.test(e.key)) e.preventDefault();
                });

                // Validate complete number on blur
                phoneEl.addEventListener('blur', function() {
                    var digits = phoneEl.value.replace(/\D/g, '');
                    if (digits.length > 0 && digits.length < 10) {
                        phoneEl.classList.add('is-invalid');
                        phoneEl.setCustomValidity('Please enter a complete 10-digit phone number.');
                    } else {
                        phoneEl.classList.remove('is-invalid');
                        phoneEl.setCustomValidity('');
                    }
                });
                phoneEl.addEventListener('focus', function() {
                    phoneEl.classList.remove('is-invalid');
                    phoneEl.setCustomValidity('');
                });
            }

            if (emailEl && !emailEl._validateDone) {
                emailEl._validateDone = true;

                emailEl.addEventListener('blur', function() {
                    var val = emailEl.value.trim();
                    if (val.length === 0) {
                        emailEl.classList.remove('is-invalid');
                        emailEl.setCustomValidity('');
                        return;
                    }
                    var valid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val);
                    if (!valid) {
                        emailEl.classList.add('is-invalid');
                        emailEl.setCustomValidity('Please enter a valid email address.');
                    } else {
                        emailEl.classList.remove('is-invalid');
                        emailEl.setCustomValidity('');
                    }
                });
                emailEl.addEventListener('focus', function() {
                    emailEl.classList.remove('is-invalid');
                    emailEl.setCustomValidity('');
                });
            }
        }

        function contactEdit() {
            document.getElementById('<%= pnlContactRead.ClientID %>').style.display = 'none';
            document.getElementById('<%= pnlContactEdit.ClientID %>').style.display = '';
            document.getElementById('btnEditContactJs').style.display  = 'none';
            document.getElementById('btnCancelContactJs').style.display = '';
            initContactMasks();
        }

        function contactCancel() {
            document.getElementById('<%= pnlContactEdit.ClientID %>').style.display = 'none';
            document.getElementById('<%= pnlContactRead.ClientID %>').style.display = '';
            document.getElementById('btnCancelContactJs').style.display = 'none';
            document.getElementById('btnEditContactJs').style.display  = '';
        }

        // ================================================================
        // People Tab — Property Contacts
        // ================================================================

        var _peopleContacts    = [];
        var _peopleTypes       = [];
        var _peoplePropertyId  = '';
        var _peopleHandlerUrl  = '';
        var _peopleSearchTimer = null;

        var _peoplePriorityTypes = ['Buyer', 'Seller', 'Property Owner'];

        var _peopleGroups = [
            { key: 'sellers',    label: 'Sellers & Owners', icon: 'ti-home',         types: ['Seller', 'Property Owner'],                                                                                    color: 'warning'   },
            { key: 'buyers',     label: 'Buyers',           icon: 'ti-shopping-bag', types: ['Buyer'],                                                                                                       color: 'success'   },
            { key: 'thirdparty', label: 'Third Parties',    icon: 'ti-briefcase',    types: ['Attorney','Property Manager','LLC / Trust Representative','Broker / Agent','Decision Maker','Third Party','Other'], color: 'secondary' }
        ];

        function initPeopleTab() {
            _peoplePropertyId = document.getElementById('<%= hdnPropertyId.ClientID %>').value;
            _peopleHandlerUrl = document.getElementById('<%= hdnPeopleHandlerUrl.ClientID %>').value;
            if (!_peoplePropertyId || _peopleContacts.length > 0) return;
            loadPeopleContacts();
        }

        function loadPeopleContacts() {
            document.getElementById('divPeopleLoading').style.display = '';
            document.getElementById('divPeopleEmpty').style.display   = 'none';
            document.getElementById('divPeopleGroups').style.display  = 'none';
            fetch(_peopleHandlerUrl + '?action=getcontacts&propertyId=' + _peoplePropertyId)
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    document.getElementById('divPeopleLoading').style.display = 'none';
                    if (!res.success) { alert('Error loading contacts: ' + res.error); return; }
                    _peopleContacts = res.contacts || [];
                    _peopleTypes    = res.types    || [];
                    buildPeopleTypeCheckboxes();
                    renderPeopleGroups();
                })
                .catch(function (err) {
                    document.getElementById('divPeopleLoading').style.display = 'none';
                    alert('Network error loading contacts: ' + err.message);
                });
        }

        function renderPeopleGroups() {
            var divGroups = document.getElementById('divPeopleGroups');
            var divEmpty  = document.getElementById('divPeopleEmpty');
            if (!_peopleContacts.length) {
                divEmpty.style.display  = '';
                divGroups.style.display = 'none';
                return;
            }
            divEmpty.style.display  = 'none';
            divGroups.style.display = '';
            var html = '';
            _peopleGroups.forEach(function (group) {
                var groupContacts = _peopleContacts.filter(function (c) {
                    return c.contactTypes && c.contactTypes.some(function (t) {
                        return group.types.indexOf(t) !== -1;
                    });
                });
                groupContacts.sort(function (a, b) {
                    if (a.isPrimary && !b.isPrimary) return -1;
                    if (!a.isPrimary && b.isPrimary) return 1;
                    return (a.fullName || '').localeCompare(b.fullName || '');
                });
                if (!groupContacts.length) return;
                html += '<div class="card mb-3">';
                html += '<div class="card-header d-flex align-items-center gap-2">';
                html += '<i class="ti ' + group.icon + ' text-' + group.color + '"></i>';
                html += '<h6 class="card-title mb-0 fw-bold">' + escPpl(group.label) + '</h6>';
                html += '<span class="badge text-bg-' + group.color + ' ms-1">' + groupContacts.length + '</span>';
                html += '</div><div class="card-body p-0">';
                groupContacts.forEach(function (c) { html += renderPeopleContactRow(c); });
                html += '</div></div>';
            });
            // Uncategorised
            var allGroupTypes = [];
            _peopleGroups.forEach(function (g) { allGroupTypes = allGroupTypes.concat(g.types); });
            var uncat = _peopleContacts.filter(function (c) {
                if (!c.contactTypes || !c.contactTypes.length) return true;
                return !c.contactTypes.some(function (t) { return allGroupTypes.indexOf(t) !== -1; });
            });
            if (uncat.length) {
                html += '<div class="card mb-3"><div class="card-header"><h6 class="card-title mb-0 fw-bold text-muted">Other</h6></div><div class="card-body p-0">';
                uncat.forEach(function (c) { html += renderPeopleContactRow(c); });
                html += '</div></div>';
            }
            divGroups.innerHTML = html;
        }

        function renderPeopleContactRow(c) {
            var initials  = getPplInitials(c.firstName, c.lastName, c.companyName);
            var color     = getPplAvatarColor(c.contactId);
            var detailUrl = '/Secure/Contacts/Contact.aspx?contactId=' + c.contactId;
            var html = '<div class="d-flex align-items-start gap-3 p-3 border-bottom">';
            html += '<div class="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0 fw-bold text-white" style="width:42px;height:42px;font-size:14px;background:' + color + ';">' + initials + '</div>';
            html += '<div class="flex-grow-1">';
            html += '<div class="d-flex align-items-center gap-2 flex-wrap">';
            html += '<a href="' + detailUrl + '" class="fw-semibold text-body text-decoration-none">' + escPpl(c.fullName || c.companyName || '(No Name)') + '</a>';
            if (c.isPrimary) html += '<span class="badge text-bg-primary fs-xs">Primary</span>';
            if (c.contactTypes && (c.contactTypes.indexOf('Property Owner') !== -1 || c.contactTypes.indexOf('Seller') !== -1)) {
                html += '<span class="badge text-bg-warning text-dark fs-xs">Owner</span>';
            }
            if (c.contactTypes && c.contactTypes.length) {
                var sorted = c.contactTypes.slice().sort(function (a, b) {
                    var ai = _peoplePriorityTypes.indexOf(a), bi = _peoplePriorityTypes.indexOf(b);
                    if (ai === -1 && bi === -1) return 0;
                    if (ai === -1) return 1; if (bi === -1) return -1;
                    return ai - bi;
                });
                sorted.slice(0, 3).forEach(function (t) {
                    var isPri = _peoplePriorityTypes.indexOf(t) !== -1;
                    html += '<span class="badge ' + getPplTypeBadgeClass(t) + ' me-1" style="font-size:' + (isPri ? '0.8rem;font-weight:700' : '0.7rem;opacity:0.8') + ';">' + escPpl(t) + '</span>';
                });
                if (c.contactTypes.length > 3) html += '<span class="text-muted" style="font-size:0.7rem;">+' + (c.contactTypes.length - 3) + '</span>';
            }
            html += '</div>';
            html += '<div class="text-muted fs-xs mt-1 d-flex flex-wrap gap-2">';
            if (c.companyName && c.fullName && c.fullName !== c.companyName) html += '<span><i class="ti ti-building me-1"></i>' + escPpl(c.companyName) + '</span>';
            if (c.phone)  html += '<a href="tel:' + escPpl(c.phone)  + '" class="text-muted text-decoration-none"><i class="ti ti-phone me-1"></i>' + escPpl(c.phone) + '</a>';
            if (c.email)  html += '<a href="mailto:' + escPpl(c.email) + '" class="text-muted text-decoration-none"><i class="ti ti-mail me-1"></i>' + escPpl(c.email) + '</a>';
            if (c.linkedInUrl) html += '<a href="' + escPpl(c.linkedInUrl) + '" target="_blank" class="text-primary text-decoration-none"><i class="ti ti-brand-linkedin me-1"></i>LinkedIn</a>';
            html += '</div>';
            if (c.buyerScore !== null && c.buyerScore !== undefined) {
                var sc = c.buyerScore >= 70 ? 'text-bg-success' : c.buyerScore >= 40 ? 'text-bg-warning' : 'text-bg-danger';
                html += '<div class="mt-1"><span class="badge ' + sc + ' fs-xs">Buyer Score: ' + c.buyerScore + '</span></div>';
            }
            html += '</div>';
            html += '<div class="d-flex flex-column gap-1 flex-shrink-0">';
            html += '<a href="' + detailUrl + '" class="btn btn-outline-primary btn-sm py-0 px-2" style="font-size:11px;" title="View"><i class="ti ti-eye"></i></a>';
            if (!c.isPrimary) {
                html += '<button type="button" class="btn btn-outline-secondary btn-sm py-0 px-2" style="font-size:11px;" title="Set as primary" onclick="setPeopleContactPrimary(\'' + c.propertyContactId + '\')"><i class="ti ti-star"></i></button>';
            }
            html += '<button type="button" class="btn btn-outline-danger btn-sm py-0 px-2" style="font-size:11px;" title="Remove from property" onclick="removePeopleContact(\'' + c.propertyContactId + '\')"><i class="ti ti-unlink"></i></button>';
            html += '</div></div>';
            return html;
        }

        function openAddPeopleModal() {
            clearPeopleModalForm();
            document.getElementById('modalPeopleContactLabel').textContent = 'Add Contact to Property';
            new bootstrap.Modal(document.getElementById('modalPeopleContact')).show();
            initPeopleMasks();
        }

        function initPeopleMasks() {
            ['pplPhone', 'pplMobile'].forEach(function (id) {
                var el = document.getElementById(id);
                if (!el || el._maskDone) return;
                el._maskDone = true;

                el.addEventListener('input', function () {
                    var digits = el.value.replace(/\D/g, '').substring(0, 10);
                    if      (digits.length === 0) el.value = '';
                    else if (digits.length <= 3)  el.value = '(' + digits;
                    else if (digits.length <= 6)  el.value = '(' + digits.substring(0,3) + ') ' + digits.substring(3);
                    else                          el.value = '(' + digits.substring(0,3) + ') ' + digits.substring(3,6) + '-' + digits.substring(6);
                });

                el.addEventListener('keydown', function (e) {
                    var allow = ['Backspace','Delete','ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Tab','Home','End'];
                    if (allow.indexOf(e.key) >= 0) return;
                    if (!/^\d$/.test(e.key)) e.preventDefault();
                });

                el.addEventListener('blur', function () {
                    var digits = el.value.replace(/\D/g, '');
                    if (digits.length > 0 && digits.length < 10) el.classList.add('is-invalid');
                    else el.classList.remove('is-invalid');
                });

                el.addEventListener('focus', function () { el.classList.remove('is-invalid'); });
            });

            var emailEl = document.getElementById('pplEmail');
            if (emailEl && !emailEl._validateDone) {
                emailEl._validateDone = true;
                emailEl.addEventListener('blur', function () {
                    var val = emailEl.value.trim();
                    if (val && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val)) emailEl.classList.add('is-invalid');
                    else emailEl.classList.remove('is-invalid');
                });
                emailEl.addEventListener('focus', function () { emailEl.classList.remove('is-invalid'); });
            }
        }

        function clearPeopleModalForm() {
            ['pplFirstName','pplLastName','pplCompany','pplEmail','pplPhone','pplMobile','pplLinkedIn','pplNotes'].forEach(function (id) {
                var el = document.getElementById(id);
                el.value = '';
                el.classList.remove('is-invalid');
            });
            document.getElementById('chkPeopleIsPrimary').checked = false;
            document.querySelectorAll('#divPeopleTypes input[type=checkbox]').forEach(function (cb) { cb.checked = false; });
            document.getElementById('divPeopleValidation').classList.add('d-none');
            document.getElementById('ulPeopleValidation').innerHTML = '';
            document.getElementById('txtPeopleSearch').value = '';
            document.getElementById('divSearchResults').style.display = 'none';
            document.getElementById('listSearchResults').innerHTML = '';
        }

        function buildPeopleTypeCheckboxes() {
            var html = '';
            _peopleTypes.forEach(function (t) {
                html += '<div class="form-check form-check-inline mb-1">';
                html += '<input class="form-check-input" type="checkbox" id="pplt_' + t.ContactTypeId + '" value="' + t.ContactTypeId + '" data-name="' + escPpl(t.Name) + '">';
                html += '<label class="form-check-label fs-sm" for="pplt_' + t.ContactTypeId + '">' + escPpl(t.Name) + '</label>';
                html += '</div>';
            });
            document.getElementById('divPeopleTypes').innerHTML = html;
        }

        function searchExistingContacts() {
            clearTimeout(_peopleSearchTimer);
            var q = document.getElementById('txtPeopleSearch').value.trim();
            if (q.length < 2) { document.getElementById('divSearchResults').style.display = 'none'; return; }
            _peopleSearchTimer = setTimeout(function () {
                fetch(_peopleHandlerUrl + '?action=searchcontacts&propertyId=' + _peoplePropertyId + '&q=' + encodeURIComponent(q))
                    .then(function (r) { return r.json(); })
                    .then(function (res) {
                        if (!res.success || !res.results.length) { document.getElementById('divSearchResults').style.display = 'none'; return; }
                        var html = '<div class="list-group list-group-flush border rounded">';
                        res.results.forEach(function (c) {
                            html += '<button type="button" class="list-group-item list-group-item-action py-2" onclick="linkExistingContact(\'' + c.contactId + '\')">';
                            html += '<div class="fw-semibold fs-sm">' + escPpl(c.displayName || c.companyName || '(No Name)') + '</div>';
                            if (c.email) html += '<div class="text-muted fs-xs">' + escPpl(c.email) + '</div>';
                            if (c.phone) html += '<div class="text-muted fs-xs">' + escPpl(c.phone) + '</div>';
                            html += '</button>';
                        });
                        html += '</div>';
                        document.getElementById('listSearchResults').innerHTML = html;
                        document.getElementById('divSearchResults').style.display = '';
                    })
                    .catch(function () {});
            }, 300);
        }

        function linkExistingContact(contactId) {
            var isPrimary = document.getElementById('chkPeopleIsPrimary').checked;
            var params = new URLSearchParams({ action: 'linkexisting', propertyId: _peoplePropertyId, contactId: contactId, isPrimary: isPrimary ? 'true' : 'false' });
            fetch(_peopleHandlerUrl + '?' + params.toString(), { method: 'POST', body: '{}' })
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    if (!res.success) { alert('Error: ' + res.error); return; }
                    bootstrap.Modal.getInstance(document.getElementById('modalPeopleContact')).hide();
                    _peopleContacts = res.contacts || [];
                    renderPeopleGroups();
                })
                .catch(function (err) { alert('Network error: ' + err.message); });
        }

        function savePeopleContact() {
            var errors    = [];
            var firstName = document.getElementById('pplFirstName').value.trim();
            var lastName  = document.getElementById('pplLastName').value.trim();
            var email     = document.getElementById('pplEmail').value.trim();
            var phone     = document.getElementById('pplPhone').value.trim();
            var mobile    = document.getElementById('pplMobile').value.trim();
            document.getElementById('divPeopleValidation').classList.add('d-none');
            if (!firstName && !lastName) errors.push('Please enter at least a First Name or Last Name.');
            if (email  && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))   errors.push('Please enter a valid email address.');
            if (phone  && phone.replace(/\D/g,'').length !== 10)         errors.push('Phone must be 10 digits.');
            if (mobile && mobile.replace(/\D/g,'').length !== 10)        errors.push('Mobile phone must be 10 digits.');
            if (errors.length) {
                document.getElementById('ulPeopleValidation').innerHTML = errors.map(function (e) { return '<li>' + escPpl(e) + '</li>'; }).join('');
                document.getElementById('divPeopleValidation').classList.remove('d-none');
                return;
            }
            var btn = document.getElementById('btnSavePeopleContact');
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';
            var selectedTypeIds = [];
            document.querySelectorAll('#divPeopleTypes input[type=checkbox]:checked').forEach(function (cb) { selectedTypeIds.push(cb.value); });
            var payload = {
                FirstName: firstName, LastName: lastName,
                CompanyName: document.getElementById('pplCompany').value.trim(),
                Email: email, Phone: phone,
                MobilePhone: mobile,
                LinkedInUrl: document.getElementById('pplLinkedIn').value.trim(),
                Notes:       document.getElementById('pplNotes').value.trim(),
                ContactTypeIds: selectedTypeIds
            };
            var isPrimary = document.getElementById('chkPeopleIsPrimary').checked;
            var url = _peopleHandlerUrl + '?action=linknew&propertyId=' + _peoplePropertyId + '&isPrimary=' + (isPrimary ? 'true' : 'false');
            fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    btn.disabled = false;
                    btn.innerHTML = '<i class="ti ti-user-plus me-1"></i>Add to Property';
                    if (!res.success) { alert('Error: ' + res.error); return; }
                    bootstrap.Modal.getInstance(document.getElementById('modalPeopleContact')).hide();
                    _peopleContacts = res.contacts || [];
                    renderPeopleGroups();
                })
                .catch(function (err) {
                    btn.disabled = false;
                    btn.innerHTML = '<i class="ti ti-user-plus me-1"></i>Add to Property';
                    alert('Network error: ' + err.message);
                });
        }

        function setPeopleContactPrimary(propertyContactId) {
            fetch(_peopleHandlerUrl + '?action=setprimary&propertyId=' + _peoplePropertyId + '&propertyContactId=' + propertyContactId, { method: 'POST', body: '{}' })
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    if (!res.success) { alert('Error: ' + res.error); return; }
                    _peopleContacts = res.contacts || [];
                    renderPeopleGroups();
                });
        }

        function removePeopleContact(propertyContactId) {
            if (!confirm('Remove this contact from the property? The contact record will not be deleted.')) return;
            fetch(_peopleHandlerUrl + '?action=unlink&propertyId=' + _peoplePropertyId + '&propertyContactId=' + propertyContactId, { method: 'POST', body: '{}' })
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    if (!res.success) { alert('Error: ' + res.error); return; }
                    _peopleContacts = res.contacts || [];
                    renderPeopleGroups();
                });
        }

        function getPplInitials(first, last, company) {
            if (first && last) return (first[0] + last[0]).toUpperCase();
            if (first) return first[0].toUpperCase();
            if (last)  return last[0].toUpperCase();
            if (company) return company[0].toUpperCase();
            return '?';
        }

        function getPplAvatarColor(id) {
            var colors = ['#3b82f6','#10b981','#f59e0b','#ef4444','#8b5cf6','#06b6d4','#ec4899','#84cc16'];
            var sum = 0;
            for (var i = 0; i < Math.min((id||'').length, 8); i++) sum += id.charCodeAt(i);
            return colors[sum % colors.length];
        }

        function getPplTypeBadgeClass(t) {
            var map = { 'Property Owner':'text-bg-primary','Buyer':'text-bg-success','Seller':'text-bg-warning','Decision Maker':'text-bg-info','Attorney':'text-bg-secondary','Property Manager':'text-bg-secondary','Broker / Agent':'text-bg-secondary','LLC / Trust Representative':'text-bg-dark','Third Party':'text-bg-secondary','Other':'text-bg-secondary' };
            return map[t] || 'text-bg-secondary';
        }

        function escPpl(str) {
            if (!str) return '';
            return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;').replace(/'/g,'&#39;');
        }

        // ── Preferred Contact — Ajax save ────────────────────────────────
        (function () {
            // Seed the visible JS select from the hidden server-side dropdown value
            var serverVal = document.getElementById('<%= ddlPreferredContact.ClientID %>');
            var jsSelect  = document.getElementById('selPreferredContact');
            if (serverVal && jsSelect) {
                jsSelect.value = serverVal.value || '';
            }
        })();

        function savePreferredContact(sel) {
            var contactId = document.getElementById('<%= hdnContactSaved.ClientID %>').value;
            if (!contactId) return;

            var handlerUrl = '<%= ResolveUrl("~/Secure/Prospects/PropertyContactHandler.ashx") %>';
            var preferred  = sel.value;

            fetch(handlerUrl + '?action=setpreferred&contactId=' + contactId + '&preferred=' + encodeURIComponent(preferred), {
                method: 'POST',
                body: '{}'
            })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                var icon = document.getElementById('spnPreferredSaved');
                if (res.success) {
                    if (icon) {
                        icon.style.display = '';
                        setTimeout(function () { icon.style.display = 'none'; }, 2000);
                    }
                } else {
                    alert('Could not save preferred contact method: ' + (res.error || 'Unknown error'));
                }
            })
            .catch(function (err) {
                alert('Network error saving preferred contact: ' + err.message);
            });
        }

    </script>

</asp:Content>
