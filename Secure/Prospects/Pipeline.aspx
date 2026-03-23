<%@ Page Title="Deal Pipeline" Language="C#" MasterPageFile="~/Reyla.Master" AutoEventWireup="true"
         CodeBehind="Pipeline.aspx.cs" Inherits="Reyla.Secure.Prospects.Pipeline" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

<style>
    /* ── Board layout ─────────────────────────────────────────────────── */
    .pipeline-board {
        display: flex;
        gap: 14px;
        overflow-x: auto;
        padding-bottom: 16px;
        align-items: flex-start;
        /* Smooth scrolling on mobile */
        -webkit-overflow-scrolling: touch;
        scroll-snap-type: x proximity;
    }

    /* On mobile stack columns vertically */
    @media (max-width: 767px) {
        .pipeline-board {
            flex-direction: column;
            overflow-x: visible;
            gap: 10px;
        }
        .pipeline-col {
            flex: none !important;
            width: 100% !important;
            min-width: 0 !important;
        }
        .pipeline-cards {
            /* Remove max-height constraint on mobile so all cards show */
            max-height: none !important;
        }
    }

    .pipeline-col {
        flex: 0 0 240px;
        min-width: 240px;
        background: #f8fafc;
        border-radius: 10px;
        border: 1px solid #e2e8f0;
        display: flex;
        flex-direction: column;
        scroll-snap-align: start;
    }

    .pipeline-col-header {
        padding: 10px 14px 8px;
        border-bottom: 1px solid #e2e8f0;
        display: flex;
        align-items: center;
        justify-content: space-between;
        border-radius: 10px 10px 0 0;
    }

    .pipeline-col-title {
        font-size: 12px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: .05em;
        color: #475569;
    }

    .pipeline-col-count {
        font-size: 11px;
        font-weight: 600;
        background: #e2e8f0;
        color: #64748b;
        border-radius: 10px;
        padding: 1px 7px;
        min-width: 20px;
        text-align: center;
    }

    /* Stage colour accents */
    .col-Lead         .pipeline-col-header { border-top: 3px solid #94a3b8; }
    .col-Qualified    .pipeline-col-header { border-top: 3px solid #3b82f6; }
    .col-LOI          .pipeline-col-header { border-top: 3px solid #8b5cf6; }
    .col-UnderContract .pipeline-col-header { border-top: 3px solid #f59e0b; }
    .col-Closed       .pipeline-col-header { border-top: 3px solid #10b981; }
    .col-Dead         .pipeline-col-header { border-top: 3px solid #ef4444; }

    .pipeline-cards {
        padding: 8px;
        min-height: 120px;
        flex: 1;
    }

    .pipeline-cards.drag-over {
        background: #eff6ff;
        border-radius: 0 0 10px 10px;
    }

    /* ── Deal card ───────────────────────────────────────────────────── */
    .deal-card {
        background: #fff;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        padding: 10px 12px;
        margin-bottom: 8px;
        cursor: grab;
        transition: box-shadow 0.15s, border-color 0.15s;
        user-select: none;
    }

    .deal-card:hover         { box-shadow: 0 2px 8px rgba(0,0,0,.08); border-color: #bfdbfe; }
    .deal-card.dragging      { opacity: 0.4; cursor: grabbing; }
    .deal-card.drag-placeholder {
        border: 2px dashed #93c5fd;
        background: #eff6ff;
        min-height: 64px;
    }

    .deal-card-title {
        font-size: 13px;
        font-weight: 600;
        color: #1e293b;
        margin-bottom: 3px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .deal-card-addr {
        font-size: 11px;
        color: #94a3b8;
        margin-bottom: 6px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .deal-card-meta {
        display: flex;
        flex-wrap: wrap;
        gap: 4px;
        margin-bottom: 6px;
    }

    .deal-tag {
        font-size: 10px;
        font-weight: 500;
        padding: 1px 6px;
        border-radius: 8px;
        white-space: nowrap;
    }

    .deal-tag-price  { background: #fefce8; color: #854d0e; border: 1px solid #fde68a; }
    .deal-tag-offer  { background: #f0fdf4; color: #166534; border: 1px solid #bbf7d0; }
    .deal-tag-date   { background: #eff6ff; color: #1e40af; border: 1px solid #bfdbfe; }
    .deal-tag-agent  { background: #f5f3ff; color: #6d28d9; border: 1px solid #ddd6fe; }

    .deal-card-actions {
        display: flex;
        gap: 6px;
        margin-top: 4px;
        align-items: center;
        justify-content: space-between;
    }

    .deal-card-links { display: flex; gap: 5px; }
    .deal-card-links a {
        font-size: 11px;
        color: #1e40af;
        text-decoration: none;
        opacity: .75;
    }
    .deal-card-links a:hover { opacity: 1; text-decoration: underline; }

    .deal-card-btns { display: flex; gap: 4px; }
    .deal-btn {
        font-size: 10px;
        padding: 1px 6px;
        border-radius: 4px;
        border: 1px solid #e2e8f0;
        background: transparent;
        color: #64748b;
        cursor: pointer;
        line-height: 1.6;
    }
    .deal-btn:hover { background: #f1f5f9; color: #1e293b; border-color: #cbd5e1; }
    .deal-btn-danger:hover { background: #fef2f2; color: #dc2626; border-color: #fecaca; }

    /* ── Deal notes panel (per card) ─────────────────────────────────── */
    .deal-notes-toggle {
        display: flex;
        align-items: center;
        gap: 4px;
        font-size: 11px;
        color: #64748b;
        background: none;
        border: none;
        padding: 2px 0;
        cursor: pointer;
        width: 100%;
        text-align: left;
        border-top: 1px dashed #e2e8f0;
        margin-top: 6px;
        padding-top: 5px;
    }
    .deal-notes-toggle:hover { color: #1e40af; }

    .deal-notes-panel {
        display: none;
        margin-top: 8px;
        border-top: 1px solid #e2e8f0;
        padding-top: 8px;
    }
    .deal-notes-panel.open { display: block; }

    .deal-note-item {
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 6px;
        padding: 7px 10px;
        margin-bottom: 6px;
        font-size: 11px;
    }
    .deal-note-item .note-text   { color: #1e293b; margin-bottom: 3px; white-space: pre-wrap; }
    .deal-note-item .note-meta   { color: #94a3b8; font-size: 10px; }
    .deal-note-item .note-edit-area { display:none; margin-top: 5px; }
    .deal-note-item .note-edit-area textarea {
        font-size: 11px; width: 100%; border-radius: 4px;
        border: 1px solid #d1d5db; padding: 4px 6px; resize: vertical;
    }

    .deal-note-add {
        display: flex;
        gap: 5px;
        margin-top: 4px;
    }
    .deal-note-add textarea {
        flex: 1;
        font-size: 11px;
        border-radius: 4px;
        border: 1px solid #d1d5db;
        padding: 4px 6px;
        resize: none;
    }
    .deal-note-add .btn-note-save {
        font-size: 11px;
        padding: 3px 8px;
        background: #1e40af;
        color: #fff;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        align-self: flex-end;
        white-space: nowrap;
    }
    .deal-note-add .btn-note-save:hover { background: #1d3a9e; }
    .btn-add-card {
        display: flex;
        align-items: center;
        gap: 5px;
        width: 100%;
        padding: 7px 14px;
        font-size: 12px;
        color: #94a3b8;
        background: transparent;
        border: none;
        border-top: 1px solid #e2e8f0;
        border-radius: 0 0 10px 10px;
        cursor: pointer;
        text-align: left;
        transition: background 0.12s, color 0.12s;
    }
    .btn-add-card:hover { background: #eff6ff; color: #1e40af; }

    /* ── Modal ───────────────────────────────────────────────────────── */
    .deal-modal-overlay {
        display: none;
        position: fixed;
        inset: 0;
        background: rgba(0,0,0,.45);
        z-index: 2000;
        align-items: center;
        justify-content: center;
    }
    .deal-modal-overlay.open { display: flex; }

    .deal-modal {
        background: #fff;
        border-radius: 12px;
        width: 440px;
        max-width: 95vw;
        max-height: 90vh;
        overflow-y: auto;
        padding: 24px;
        box-shadow: 0 20px 60px rgba(0,0,0,.2);
        position: relative;
    }

    @media (max-width: 480px) {
        .deal-modal {
            width: 100%;
            max-width: 100%;
            max-height: 100vh;
            border-radius: 12px 12px 0 0;
            padding: 20px 16px;
        }
        .deal-modal-overlay {
            align-items: flex-end !important;
        }
    }

    .deal-modal-title {
        font-size: 16px;
        font-weight: 700;
        color: #1e293b;
        margin-bottom: 18px;
    }

    .deal-modal .form-label { font-size: 12px; font-weight: 600; color: #475569; margin-bottom: 4px; }
    .deal-modal .form-control,
    .deal-modal .form-select { font-size: 13px; }

    .deal-modal-footer {
        display: flex;
        justify-content: flex-end;
        gap: 8px;
        margin-top: 20px;
    }

    .deal-modal-close {
        position: absolute;
        top: 14px; right: 14px;
        background: none; border: none;
        font-size: 18px; color: #94a3b8;
        cursor: pointer; line-height: 1;
    }
    .deal-modal-close:hover { color: #1e293b; }

    /* ── Alert ───────────────────────────────────────────────────────── */
    #boardAlert { display: none; margin-bottom: 12px; }
</style>

<%-- Page header --%>
<div class="page-title-head d-flex align-items-sm-center flex-sm-row flex-column gap-2 mb-3">
    <div class="flex-grow-1">
        <h4 class="fs-lg fw-bold mb-1">Deal Pipeline</h4>
        <p class="text-muted mb-0 fs-xs" id="boardSubtitle">Loading...</p>
    </div>
    <div class="d-flex align-items-center gap-2">
        <button type="button" class="btn btn-primary btn-sm" onclick="openAddModal(null, event)">
            <i class="ti ti-plus me-1"></i>New Deal
        </button>
        <ol class="breadcrumb m-0 py-0 fs-xs">
            <li class="breadcrumb-item"><a href="/Secure/Index">Home</a></li>
            <li class="breadcrumb-item active">Pipeline</li>
        </ol>
    </div>
</div>

<div class="alert" id="boardAlert" role="alert"></div>

<%-- Board --%>
<div class="pipeline-board" id="pipelineBoard"></div>

<%-- Add / Edit Deal Modal --%>
<div class="deal-modal-overlay" id="dealModal">
    <div class="deal-modal">
        <button class="deal-modal-close" onclick="closeModal()" type="button" aria-label="Close">&times;</button>
        <div class="deal-modal-title" id="modalTitle">New Deal</div>
        <input type="hidden" id="modalDealId"    value="" />
        <input type="hidden" id="modalStage"     value="" />
        <input type="hidden" id="modalProspectHidden" value="" />

        <%-- New deal: pick a prospect --%>
        <div class="mb-3" id="prospectPickerRow">
            <label class="form-label fw-semibold">Property / Prospect <span class="text-danger">*</span></label>
            <select id="modalProspectId" class="form-select form-select-sm" onchange="onProspectPicked()">
                <option value="">-- Select a prospect --</option>
            </select>
            <div class="text-muted fs-xs mt-1" id="modalProspectHint"></div>
        </div>

        <%-- Edit deal: show property read-only --%>
        <div class="mb-3" id="prospectReadRow" style="display:none">
            <label class="form-label fw-semibold">Property</label>
            <div class="form-control form-control-sm bg-light" id="modalProspectDisplay"
                 style="color:#475569; cursor:default;"></div>
        </div>

        <div class="mb-3">
            <label class="form-label fw-semibold">Deal Title</label>
            <input type="text" id="modalTitleInput" class="form-control form-control-sm"
                   placeholder="Defaults to property address if left blank" maxlength="200" />
        </div>

        <div class="row g-2 mb-3">
            <div class="col-6">
                <label class="form-label fw-semibold">Asking Price</label>
                <div class="input-group input-group-sm">
                    <span class="input-group-text">$</span>
                    <input type="number" id="modalAsking" class="form-control"
                           placeholder="0" min="0" step="1000" />
                </div>
            </div>
            <div class="col-6">
                <label class="form-label fw-semibold">Offer Price</label>
                <div class="input-group input-group-sm">
                    <span class="input-group-text">$</span>
                    <input type="number" id="modalOffer" class="form-control"
                           placeholder="0" min="0" step="1000" />
                </div>
            </div>
        </div>

        <div class="row g-2 mb-3">
            <div class="col-6">
                <label class="form-label fw-semibold">Target Close Date</label>
                <input type="date" id="modalClose" class="form-control form-control-sm" />
            </div>
            <div class="col-6">
                <label class="form-label fw-semibold">Stage</label>
                <select id="modalStageSelect" class="form-select form-select-sm">
                    <option value="Lead">Lead</option>
                    <option value="Qualified">Qualified</option>
                    <option value="LOI">LOI</option>
                    <option value="UnderContract">Under Contract</option>
                    <option value="Closed">Closed</option>
                    <option value="Dead">Dead</option>
                </select>
            </div>
        </div>

        <div id="modalAlert" class="mt-2" style="display:none"></div>
        <div class="deal-modal-footer">
            <button type="button" class="btn btn-outline-secondary btn-sm" onclick="closeModal()">Cancel</button>
            <button type="button" class="btn btn-primary btn-sm" id="modalSaveBtn" onclick="saveModal()">
                <i class="ti ti-device-floppy me-1"></i>Save
            </button>
        </div>
    </div>
</div>

<asp:HiddenField ID="hdnBoardJson"     runat="server" />
<asp:HiddenField ID="hdnProspectsJson" runat="server" />

<script>
(function () {

    // ── Prospect picker data ──────────────────────────────────────────
    var PROSPECTS = [];
    try {
        var rawP = document.getElementById('<%= hdnProspectsJson.ClientID %>').value;
        if (rawP) PROSPECTS = JSON.parse(rawP);
    } catch(e) {}

    function populateProspectPicker(excludeProspectId) {
        var sel = document.getElementById('modalProspectId');
        sel.innerHTML = '<option value="">-- Select a prospect --</option>';
        PROSPECTS.forEach(function(p) {
            var opt = document.createElement('option');
            opt.value       = p.prospectId;
            opt.textContent = p.display;
            if (p.inDeal && p.prospectId !== excludeProspectId)
                opt.textContent += ' (in deal)';
            sel.appendChild(opt);
        });
    }

    window.onProspectPicked = function() {
        var sel        = document.getElementById('modalProspectId');
        var prospectId = sel.value;
        var hint       = document.getElementById('modalProspectHint');
        var titleInp   = document.getElementById('modalTitleInput');

        if (!prospectId) { hint.textContent = ''; return; }

        var p = PROSPECTS.find(function(x) { return x.prospectId === prospectId; });
        if (!p) return;

        // Auto-fill title if it's empty or was previously auto-filled
        if (!titleInp.value || titleInp.dataset.autoFilled === '1') {
            titleInp.value           = p.address || p.display;
            titleInp.dataset.autoFilled = '1';
        }

        hint.textContent = p.cityLine || '';
    };
    try {
        var raw = document.getElementById('<%= hdnBoardJson.ClientID %>').value;
        if (raw) BOARD = JSON.parse(raw);
    } catch(e) {}

    var STAGES = [
        { key: 'Lead',          label: 'Lead' },
        { key: 'Qualified',     label: 'Qualified' },
        { key: 'LOI',           label: 'LOI' },
        { key: 'UnderContract', label: 'Under Contract' },
        { key: 'Closed',        label: 'Closed' },
        { key: 'Dead',          label: 'Dead' }
    ];

    // ── Render board ──────────────────────────────────────────────────
    function renderBoard(board) {
        BOARD = board;
        var el = document.getElementById('pipelineBoard');
        el.innerHTML = '';

        var totalDeals = 0;
        STAGES.forEach(function(s) {
            var deals = (board[s.key] || []);
            totalDeals += deals.filter(function(d){ return d.stage !== 'Dead'; }).length;
            el.appendChild(buildCol(s, deals));
        });

        document.getElementById('boardSubtitle').textContent =
            totalDeals + ' active deal' + (totalDeals !== 1 ? 's' : '') + ' in pipeline';

        initDragDrop();
    }

    function buildCol(stage, deals) {
        var col = document.createElement('div');
        col.className  = 'pipeline-col col-' + stage.key;
        col.dataset.stage = stage.key;

        // Header
        var hdr = document.createElement('div');
        hdr.className = 'pipeline-col-header';
        hdr.innerHTML =
            '<span class="pipeline-col-title">' + esc(stage.label) + '</span>' +
            '<span class="pipeline-col-count">' + deals.length + '</span>';
        col.appendChild(hdr);

        // Cards
        var cards = document.createElement('div');
        cards.className    = 'pipeline-cards';
        cards.dataset.stage = stage.key;

        deals.forEach(function(deal) {
            cards.appendChild(buildCard(deal));
        });
        col.appendChild(cards);

        // Add button
        var addBtn = document.createElement('button');
        addBtn.type      = 'button';
        addBtn.className = 'btn-add-card';
        addBtn.innerHTML = '<i class="ti ti-plus fs-13"></i> Add deal';
        addBtn.addEventListener('click', function(e) { openAddModal(stage.key, e); });
        col.appendChild(addBtn);

        return col;
    }

    function buildCard(deal) {
        var card = document.createElement('div');
        card.className       = 'deal-card';
        card.dataset.dealId  = deal.dealId;
        card.dataset.stage   = deal.stage;
        card.draggable       = true;

        var tags = '';
        if (deal.askingPrice)
            tags += '<span class="deal-tag deal-tag-price">Ask $' + fmtMoney(deal.askingPrice) + '</span>';
        if (deal.offerPrice)
            tags += '<span class="deal-tag deal-tag-offer">Offer $' + fmtMoney(deal.offerPrice) + '</span>';
        if (deal.closeDateDisplay)
            tags += '<span class="deal-tag deal-tag-date">Close ' + esc(deal.closeDateDisplay) + '</span>';
        if (deal.agentName)
            tags += '<span class="deal-tag deal-tag-agent">' + esc(deal.agentName) + '</span>';

        var links = '';
        if (deal.detailUrl)
            links += '<a href="' + esc(deal.detailUrl) + '">Property Detail &rarr;</a>';
        if (deal.compsUrl)
            links += '<a href="' + esc(deal.compsUrl) + '">Comps &rarr;</a>';

        // ── Reyla Valuation comparison ────────────────────────────────
        var valHtml = '';
        if (deal.reylaEstimate) {
            var est      = deal.reylaEstimate;
            var estFmt   = '$' + fmtMoney(est);
            var confBadge = deal.confidenceLabel
                ? '<span class="deal-tag" style="background:#f0fdf4;color:#166534;border-color:#bbf7d0;font-size:9px;">' +
                  esc(deal.confidenceLabel) + ' conf</span> '
                : '';

            // Compare asking price to estimate
            var askStatus = '', offerStatus = '';
            if (deal.askingPrice) {
                var pct = ((deal.askingPrice - est) / est) * 100;
                if (pct > 5)       askStatus = '<span style="color:#dc2626;font-size:10px;" title="Asking ' + Math.abs(pct).toFixed(0) + '% above estimate">&#9650; ' + Math.abs(pct).toFixed(0) + '% over</span>';
                else if (pct < -5) askStatus = '<span style="color:#16a34a;font-size:10px;" title="Asking ' + Math.abs(pct).toFixed(0) + '% below estimate">&#9660; ' + Math.abs(pct).toFixed(0) + '% under</span>';
                else               askStatus = '<span style="color:#6b7280;font-size:10px;">&#9472; At estimate</span>';
            }
            if (deal.offerPrice) {
                var opct = ((deal.offerPrice - est) / est) * 100;
                if (opct > 5)       offerStatus = '<span style="color:#dc2626;font-size:10px;">Offer ' + Math.abs(opct).toFixed(0) + '% over</span>';
                else if (opct < -5) offerStatus = '<span style="color:#16a34a;font-size:10px;">Offer ' + Math.abs(opct).toFixed(0) + '% under</span>';
                else                offerStatus = '<span style="color:#6b7280;font-size:10px;">Offer at estimate</span>';
            }

            valHtml =
                '<div style="border-top:1px dashed #e2e8f0;margin-top:6px;padding-top:5px;font-size:11px;">' +
                '<div style="display:flex;align-items:center;justify-content:space-between;">' +
                '<span style="color:#475569;"><i class="ti ti-calculator" style="font-size:10px;margin-right:3px;"></i>' +
                '<strong>Reyla:</strong> ' + estFmt + '</span>' +
                confBadge +
                '</div>' +
                (askStatus || offerStatus
                    ? '<div style="display:flex;gap:8px;margin-top:2px;">' + askStatus +
                      (offerStatus ? '<span style="color:#d1d5db;">|</span>' + offerStatus : '') + '</div>'
                    : '') +
                '</div>';
        }

        card.innerHTML =
            '<div class="deal-card-title">' + esc(deal.title) + '</div>' +
            (deal.propertyAddress ? '<div class="deal-card-addr">' + esc(deal.propertyAddress) + '</div>' : '') +
            (tags ? '<div class="deal-card-meta">' + tags + '</div>' : '') +
            valHtml +
            '<div class="deal-card-actions">' +
                '<div class="deal-card-links">' + links + '</div>' +
                '<div class="deal-card-btns">' +
                    '<button type="button" class="deal-btn" onclick="event.stopPropagation();openEditModal(\'' + deal.dealId + '\', event)">Edit</button>' +
                    '<button type="button" class="deal-btn deal-btn-danger" onclick="event.stopPropagation();deleteDeal(\'' + deal.dealId + '\',this)">Del</button>' +
                '</div>' +
            '</div>' +
            '<button type="button" class="deal-notes-toggle" ' +
                'data-notes-toggle="' + deal.dealId + '" ' +
                'onclick="toggleNotes(\'' + deal.dealId + '\', event)">' +
                '<i class="ti ti-notes" style="font-size:11px;pointer-events:none"></i> ' +
                '<span class="notes-toggle-label" style="pointer-events:none">Notes</span>' +
            '</button>' +
            '<div class="deal-notes-panel" id="notes_' + deal.dealId + '" ' +
                'onclick="event.stopPropagation()" ' +
                'ondragstart="event.stopPropagation()">' +
                '<div class="deal-notes-list" id="notesList_' + deal.dealId + '">' +
                    '<span style="font-size:11px;color:#94a3b8;">Loading...</span>' +
                '</div>' +
                '<div class="deal-note-add mt-2">' +
                    '<textarea id="noteInput_' + deal.dealId + '" rows="2" ' +
                              'placeholder="Add a note..." maxlength="2000" ' +
                              'onclick="event.stopPropagation()" ' +
                              'ondragstart="event.stopPropagation()"></textarea>' +
                    '<button type="button" class="btn-note-save" ' +
                        'onclick="event.stopPropagation();addDealNote(\'' + deal.dealId + '\')">Add</button>' +
                '</div>' +
            '</div>';

        return card;
    }

    // ── Drag and drop ─────────────────────────────────────────────────
    var dragCard   = null;
    var dragSource = null;
    var placeholder = null;

    function initDragDrop() {
        document.querySelectorAll('.deal-card').forEach(function(card) {
            card.addEventListener('dragstart', onDragStart);
            card.addEventListener('dragend',   onDragEnd);
        });
        document.querySelectorAll('.pipeline-cards').forEach(function(zone) {
            zone.addEventListener('dragover',  onDragOver);
            zone.addEventListener('drop',      onDrop);
            zone.addEventListener('dragleave', onDragLeave);
        });
    }

    function onDragStart(e) {
        dragCard   = this;
        dragSource = this.closest('.pipeline-cards');
        this.classList.add('dragging');
        placeholder = document.createElement('div');
        placeholder.className = 'deal-card drag-placeholder';
        e.dataTransfer.effectAllowed = 'move';
    }

    function onDragEnd() {
        this.classList.remove('dragging');
        if (placeholder && placeholder.parentNode)
            placeholder.parentNode.removeChild(placeholder);
        document.querySelectorAll('.pipeline-cards').forEach(function(z) {
            z.classList.remove('drag-over');
        });
        dragCard = null; placeholder = null;
    }

    function onDragOver(e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        this.classList.add('drag-over');

        var afterEl = getDragAfterElement(this, e.clientY);
        if (afterEl) this.insertBefore(placeholder, afterEl);
        else          this.appendChild(placeholder);
    }

    function onDragLeave() { this.classList.remove('drag-over'); }

    function onDrop(e) {
        e.preventDefault();
        this.classList.remove('drag-over');
        if (!dragCard) return;

        var targetStage = this.dataset.stage;
        var newOrder    = getCardOrder(this, dragCard);

        if (placeholder && placeholder.parentNode)
            this.insertBefore(dragCard, placeholder);

        if (dragCard.dataset.stage !== targetStage) {
            // Move to new stage
            dragCard.dataset.stage = targetStage;
            dealFetch('moveStage', {
                dealId: dragCard.dataset.dealId,
                stage:  targetStage,
                newOrder: newOrder
            }, null, false);
        } else {
            // Reorder within stage
            var orderedIds = Array.from(this.querySelectorAll('.deal-card'))
                .map(function(c) { return c.dataset.dealId; })
                .filter(Boolean);
            dealFetch('reorder', {
                stage:      targetStage,
                orderedIds: JSON.stringify(orderedIds)
            }, null, false);
        }
    }

    function getDragAfterElement(container, y) {
        var cards = Array.from(container.querySelectorAll('.deal-card:not(.dragging)'));
        return cards.reduce(function(closest, child) {
            var box = child.getBoundingClientRect();
            var offset = y - box.top - box.height / 2;
            if (offset < 0 && offset > closest.offset)
                return { offset: offset, element: child };
            return closest;
        }, { offset: Number.NEGATIVE_INFINITY }).element;
    }

    function getCardOrder(container, card) {
        var cards = Array.from(container.querySelectorAll('.deal-card:not(.dragging)'));
        var after = getDragAfterElement(container, card.getBoundingClientRect().top);
        return after ? cards.indexOf(after) : cards.length;
    }

    // ── Modal ─────────────────────────────────────────────────────────

    window.openAddModal = function(stage, e) {
        if (e) { e.preventDefault(); e.stopPropagation(); }

        document.getElementById('modalTitle').textContent      = 'New Deal';
        document.getElementById('modalDealId').value           = '';
        document.getElementById('modalProspectHidden').value   = '';
        document.getElementById('modalTitleInput').value       = '';
        document.getElementById('modalTitleInput').dataset.autoFilled = '';
        document.getElementById('modalAsking').value           = '';
        document.getElementById('modalOffer').value            = '';
        document.getElementById('modalClose').value            = '';
        document.getElementById('modalStageSelect').value      = stage || 'Lead';
        document.getElementById('modalProspectHint').textContent = '';

        // Show prospect picker, hide read-only row
        document.getElementById('prospectPickerRow').style.display = '';
        document.getElementById('prospectReadRow').style.display   = 'none';
        populateProspectPicker(null);
        document.getElementById('modalProspectId').value = '';

        hideModalAlert();
        document.getElementById('dealModal').classList.add('open');
        setTimeout(function() {
            document.getElementById('modalProspectId').focus();
        }, 50);
    };

    window.openEditModal = function(dealId, e) {
        if (e) { e.preventDefault(); e.stopPropagation(); }
        var deal = null;
        Object.keys(BOARD).forEach(function(s) {
            (BOARD[s] || []).forEach(function(d) { if (d.dealId === dealId) deal = d; });
        });
        if (!deal) return;

        document.getElementById('modalTitle').textContent      = 'Edit Deal';
        document.getElementById('modalDealId').value           = deal.dealId;
        document.getElementById('modalProspectHidden').value   = deal.prospectId || '';
        document.getElementById('modalTitleInput').value       = deal.title || '';
        document.getElementById('modalAsking').value           = deal.askingPrice || '';
        document.getElementById('modalOffer').value            = deal.offerPrice  || '';
        document.getElementById('modalClose').value            = deal.closeDate   || '';
        document.getElementById('modalStageSelect').value      = deal.stage       || 'Lead';

        // Show read-only property, hide picker
        document.getElementById('prospectPickerRow').style.display = 'none';
        document.getElementById('prospectReadRow').style.display   = '';
        document.getElementById('modalProspectDisplay').textContent =
            deal.propertyAddress || deal.title || 'Unknown property';

        hideModalAlert();
        document.getElementById('dealModal').classList.add('open');
        setTimeout(function() {
            document.getElementById('modalTitleInput').focus();
        }, 50);
    };

    window.closeModal = function() {
        document.getElementById('dealModal').classList.remove('open');
    };

    window.saveModal = function() {
        var dealId     = document.getElementById('modalDealId').value;
        var prospectId = dealId ? document.getElementById('modalProspectHidden').value
                                : document.getElementById('modalProspectId').value;

        // For new deals, prospect is required
        if (!dealId && !prospectId) {
            showModalAlert('Please select a prospect.', 'warning');
            return;
        }

        // Title defaults to property address if blank
        var titleInp   = document.getElementById('modalTitleInput');
        var title      = titleInp.value.trim();
        if (!title && prospectId) {
            var p = PROSPECTS.find(function(x) { return x.prospectId === prospectId; });
            if (p) title = p.address || p.display;
        }
        if (!title) { showModalAlert('Please enter a title or select a prospect.', 'warning'); return; }

        var params = {
            title:        title,
            askingPrice:  document.getElementById('modalAsking').value,
            offerPrice:   document.getElementById('modalOffer').value,
            closeDate:    document.getElementById('modalClose').value,
            stage:        document.getElementById('modalStageSelect').value
        };

        var btn = document.getElementById('modalSaveBtn');
        btn.disabled  = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';

        if (dealId) {
            params.dealId = dealId;
            dealFetch('updateDeal', params, function() {
                closeModal();
                btn.disabled  = false;
                btn.innerHTML = '<i class="ti ti-device-floppy me-1"></i>Save';
            }, false);
        } else {
            params.prospectId = prospectId;
            // propertyAddress comes from prospect server-side via ProspectId FK
            dealFetch('createDeal', params, function() {
                closeModal();
                btn.disabled  = false;
                btn.innerHTML = '<i class="ti ti-device-floppy me-1"></i>Save';
            }, false);
        }
    };

    window.deleteDeal = function(dealId, btn) {
        if (!confirm('Delete this deal? This cannot be undone.')) return;
        btn.disabled = true;
        dealFetch('deleteDeal', { dealId: dealId }, null, false);
    };

    // ── Deal Notes ────────────────────────────────────────────────────

    window.toggleNotes = function(dealId, e) {
        if (e) { e.stopPropagation(); e.preventDefault(); }
        var panel = document.getElementById('notes_' + dealId);
        if (!panel) return;
        var opening = !panel.classList.contains('open');
        panel.classList.toggle('open');
        // Walk up to find the toggle button regardless of which child was clicked
        var btn = document.querySelector('[data-notes-toggle="' + dealId + '"]');
        if (btn) {
            var lbl = btn.querySelector('.notes-toggle-label');
            if (lbl) lbl.textContent = opening ? 'Hide Notes' : 'Notes';
        }
        if (opening) loadDealNotes(dealId);
    };

    function loadDealNotes(dealId) {
        dealFetch('getDealNotes', { dealId: dealId }, null, true);
    }

    window.addDealNote = function(dealId) {
        var ta  = document.getElementById('noteInput_' + dealId);
        var val = ta ? ta.value.trim() : '';
        if (!val) return;
        dealFetch('addDealNote', { dealId: dealId, noteText: val }, function(data) {
            if (ta) ta.value = '';
            if (data.notes) renderDealNotes(dealId, data.notes);
        }, true);
    };

    window.editDealNote = function(noteId, dealId) {
        var item = document.getElementById('dealNote_' + noteId);
        if (!item) return;
        item.querySelector('.note-text').style.display      = 'none';
        item.querySelector('.note-edit-area').style.display = '';
        item.querySelector('.note-edit-area textarea').focus();
    };

    window.saveDealNote = function(noteId, dealId) {
        var ta  = document.querySelector('#dealNote_' + noteId + ' .note-edit-area textarea');
        var val = ta ? ta.value.trim() : '';
        if (!val) return;
        dealFetch('editDealNote', { noteId: noteId, dealId: dealId, noteText: val }, function(data) {
            if (data.notes) renderDealNotes(dealId, data.notes);
        }, true);
    };

    window.cancelDealNoteEdit = function(noteId) {
        var item = document.getElementById('dealNote_' + noteId);
        if (!item) return;
        item.querySelector('.note-text').style.display      = '';
        item.querySelector('.note-edit-area').style.display = 'none';
    };

    window.deleteDealNote = function(noteId, dealId) {
        if (!confirm('Delete this note?')) return;
        dealFetch('deleteDealNote', { noteId: noteId, dealId: dealId }, function(data) {
            if (data.notes) renderDealNotes(dealId, data.notes);
        }, true);
    };

    function renderDealNotes(dealId, notes) {
        var list = document.getElementById('notesList_' + dealId);
        if (!list) return;

        // Update toggle label count
        var toggle = document.querySelector('[data-notes-toggle="' + dealId + '"]');
        if (toggle) {
            var lbl = toggle.querySelector('.notes-toggle-label');
            if (lbl) lbl.textContent = 'Hide Notes (' + notes.length + ')';
        }

        if (!notes || notes.length === 0) {
            list.innerHTML = '<p style="font-size:11px;color:#94a3b8;margin:0;">No notes yet.</p>';
            return;
        }

        list.innerHTML = notes.map(function(n) {
            return '<div class="deal-note-item" id="dealNote_' + n.noteId + '">' +
                '<div class="note-text">' + esc(n.noteText) + '</div>' +
                '<div class="note-meta">' +
                    esc(n.authorName || 'Unknown') + ' · ' + esc(n.createdLocal) +
                    (n.isEdited ? ' <em>(edited)</em>' : '') +
                '</div>' +
                '<div class="note-meta" style="margin-top:3px;">' +
                    '<button class="deal-btn" style="font-size:10px;padding:1px 5px;" ' +
                        'onclick="editDealNote(\'' + n.noteId + '\',\'' + dealId + '\')">Edit</button> ' +
                    '<button class="deal-btn deal-btn-danger" style="font-size:10px;padding:1px 5px;" ' +
                        'onclick="deleteDealNote(\'' + n.noteId + '\',\'' + dealId + '\')">Delete</button>' +
                '</div>' +
                '<div class="note-edit-area">' +
                    '<textarea rows="2" maxlength="2000" ' +
                        'ondragstart="event.stopPropagation()" ' +
                        'onclick="event.stopPropagation()">' + esc(n.noteText) + '</textarea>' +
                    '<div style="display:flex;gap:4px;margin-top:3px;">' +
                        '<button class="deal-btn" style="font-size:10px;" ' +
                            'onclick="saveDealNote(\'' + n.noteId + '\',\'' + dealId + '\')">Save</button>' +
                        '<button class="deal-btn" style="font-size:10px;" ' +
                            'onclick="cancelDealNoteEdit(\'' + n.noteId + '\')">Cancel</button>' +
                    '</div>' +
                '</div>' +
            '</div>';
        }).join('');
    }

    // ── Fetch helper — updated to handle note responses ───────────────

    function dealFetch(action, params, callback, notesOnly) {
        var fd = new FormData();
        fd.append('action', action);
        Object.keys(params).forEach(function(k) {
            if (params[k] !== null && params[k] !== undefined && params[k] !== '')
                fd.append(k, params[k]);
        });

        fetch('/Secure/Prospects/DealToggle.ashx', {
            method: 'POST', body: fd, credentials: 'same-origin'
        })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (!data.success) { showBoardAlert(data.errorMessage, 'danger'); return; }
            // Always re-render board if returned
            if (data.board) renderBoard(data.board);
            // Re-render notes for this deal if returned
            if (data.notes && data.dealId) {
                // Panel may have been re-created by renderBoard — re-open it
                var panel = document.getElementById('notes_' + data.dealId);
                if (panel && panel.classList.contains('open'))
                    renderDealNotes(data.dealId, data.notes);
                else if (panel && notesOnly) {
                    panel.classList.add('open');
                    renderDealNotes(data.dealId, data.notes);
                }
            }
            if (callback) callback(data);
        })
        .catch(function(e) { showBoardAlert('Request failed: ' + e.message, 'danger'); });
    }

    // ── Alerts ────────────────────────────────────────────────────────
    function showBoardAlert(msg, type) {
        var el = document.getElementById('boardAlert');
        el.className   = 'alert alert-' + type;
        el.textContent = msg;
        el.style.display = '';
        setTimeout(function() { el.style.display = 'none'; }, 4000);
    }

    function showModalAlert(msg, type) {
        var el = document.getElementById('modalAlert');
        el.innerHTML = '<div class="alert alert-' + type + ' py-2 mb-0">' + esc(msg) + '</div>';
        el.style.display = '';
    }
    function hideModalAlert() {
        var el = document.getElementById('modalAlert');
        el.style.display = 'none'; el.innerHTML = '';
    }

    function fmtMoney(n) {
        if (!n) return '0';
        var v = parseFloat(n);
        if (v >= 1000000) return (v/1000000).toFixed(1).replace(/\.0$/,'') + 'M';
        if (v >= 1000)    return (v/1000).toFixed(0) + 'K';
        return Math.round(v).toLocaleString();
    }

    function esc(s) {
        return String(s || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

    // ── Close modal on overlay click (not inner modal) ───────────────
    document.getElementById('dealModal').addEventListener('click', function(e) {
        if (e.target === this) closeModal();
    });
    // Prevent clicks inside the modal box from bubbling to overlay
    document.querySelector('.deal-modal').addEventListener('click', function(e) {
        e.stopPropagation();
    });

    // ── Close modal on Escape ─────────────────────────────────────────
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') closeModal();
    });

    // ── Boot ──────────────────────────────────────────────────────────
    renderBoard(BOARD);

})();
</script>

</asp:Content>
