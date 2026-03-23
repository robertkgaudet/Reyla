<%@ Page Title="Review Comparables" Language="C#" MasterPageFile="~/Reyla.Master" AutoEventWireup="true" CodeBehind="Comps.aspx.cs" Inherits="Reyla.Secure.Prospects.Comps" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

<link href="/Theme/assets/plugins/leaflet/leaflet.css" rel="stylesheet" type="text/css" />

<style>
    .comps-wrap {
        max-width: 1100px;
        margin: 32px auto;
        padding: 0 16px;
    }

    /* ── Page heading ────────────────────────────────────── */
    .comps-heading { margin-bottom: 20px; }
    .comps-heading h4 { font-size: 20px; font-weight: 700; color: #111827; margin: 0 0 4px; }
    .comps-heading p  { font-size: 14px; color: #6b7280; margin: 0; }

    /* ── Alert ───────────────────────────────────────────── */
    .page-alert { display: none; padding: 10px 14px; border-radius: 6px; font-size: 13px; margin-bottom: 16px; }
    .page-alert.show-danger  { display: block; background: #fef2f2; color: #991b1b; border: 1px solid #fecaca; }
    .page-alert.show-success { display: block; background: #f0fdf4; color: #166534; border: 1px solid #bbf7d0; }
    .page-alert.show-info    { display: block; background: #eff6ff; color: #1e40af; border: 1px solid #bfdbfe; }

    /* ── Two-panel layout ────────────────────────────────── */
    .comps-body {
        display: flex;
        gap: 20px;
        align-items: flex-start;
    }

    .comps-list-panel {
        flex: 0 0 380px;
        min-width: 0;
    }

    .comps-map-panel {
        flex: 1;
        min-width: 0;
    }

    /* ── Map ─────────────────────────────────────────────── */
    #compsMap {
        height: 560px;
        border-radius: 8px;
        border: 1px solid #e5e7eb;
    }

    /* ── Subject property card ───────────────────────────── */
    .subject-card {
        background: #eff6ff;
        border: 1px solid #bfdbfe;
        border-radius: 8px;
        margin-bottom: 14px;
        overflow: hidden;
    }

    .subject-card-thumb {
        width: 100%;
        height: 110px;
        object-fit: cover;
        display: block;
        border-bottom: 1px solid #bfdbfe;
    }

    .subject-card-body {
        padding: 10px 14px;
    }

    .subject-label {
        font-size: 11px;
        font-weight: 700;
        color: #1e40af;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        margin-bottom: 4px;
    }

    .subject-address {
        font-size: 14px;
        font-weight: 600;
        color: #111827;
        margin-bottom: 6px;
    }

    .subject-actions {
        display: flex;
        gap: 8px;
        margin-top: 6px;
        flex-wrap: wrap;
    }

    .subject-detail-link {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        font-size: 12px;
        color: #1e40af;
        text-decoration: none;
        font-weight: 500;
    }
    .subject-detail-link:hover { text-decoration: underline; }

    /* ── Comp list ───────────────────────────────────────── */
    .sort-bar {
        display: flex;
        align-items: center;
        gap: 4px;
        flex-wrap: wrap;
        margin-bottom: 8px;
    }

    .sort-label {
        font-size: 11px;
        font-weight: 700;
        color: #9ca3af;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        margin-right: 2px;
        flex-shrink: 0;
    }

    .sort-btn {
        display: inline-flex;
        align-items: center;
        gap: 3px;
        font-size: 11px;
        font-weight: 500;
        color: #6b7280;
        background: #f9fafb;
        border: 1px solid #e5e7eb;
        border-radius: 4px;
        padding: 3px 8px;
        cursor: pointer;
        white-space: nowrap;
        transition: background 0.12s, color 0.12s, border-color 0.12s;
    }

    .sort-btn:hover { background: #f3f4f6; border-color: #d1d5db; color: #374151; }

    .sort-btn.active {
        background: #eff6ff;
        border-color: #bfdbfe;
        color: #1e40af;
        font-weight: 700;
    }

    .sort-arrow { font-size: 10px; }

    .comp-list-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 8px;
    }

    .comp-list-label {
        font-size: 12px;
        font-weight: 700;
        color: #6b7280;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }

    .select-all-wrap {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 12px;
        color: #374151;
        cursor: pointer;
    }

    .comp-list {
        max-height: 420px;
        overflow-y: auto;
        margin-bottom: 14px;
    }

    .comp-item {
        display: flex;
        align-items: flex-start;
        border: 1px solid #e5e7eb;
        border-radius: 7px;
        margin-bottom: 6px;
        background: #fff;
        cursor: pointer;
        transition: border-color 0.15s, background 0.15s;
        overflow: hidden;
    }

    .comp-item:hover       { border-color: #93c5fd; }
    .comp-item.highlighted { border-color: #1e40af; background: #eff6ff; }

    .comp-item input[type=checkbox] {
        margin-top: 2px;
        flex-shrink: 0;
        width: 15px;
        height: 15px;
        cursor: pointer;
    }

    .comp-item {
        flex-direction: column;
        padding: 0;
        overflow: hidden;
    }

    .comp-thumb {
        width: 100%;
        height: 110px;
        object-fit: cover;
        display: block;
        background: #f1f5f9;
        border-bottom: 1px solid #e5e7eb;
        flex-shrink: 0;
    }

    .comp-thumb-placeholder {
        width: 100%;
        height: 110px;
        background: #f1f5f9;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #9ca3af;
        font-size: 12px;
        border-bottom: 1px solid #e5e7eb;
        flex-shrink: 0;
    }

    .comp-item-inner {
        display: flex;
        align-items: flex-start;
        gap: 10px;
        padding: 10px 12px;
        width: 100%;
    }

    .comp-item-body { flex: 1; min-width: 0; }

    .comp-address {
        font-size: 13px;
        font-weight: 600;
        color: #111827;
        margin-bottom: 2px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .comp-city {
        font-size: 12px;
        color: #6b7280;
        margin-bottom: 5px;
    }

    .comp-tags {
        display: flex;
        flex-wrap: wrap;
        gap: 4px;
        margin-top: 4px;
    }

    .comp-tag {
        display: inline-block;
        font-size: 11px;
        font-weight: 500;
        padding: 2px 7px;
        border-radius: 10px;
        white-space: nowrap;
        background: #f1f5f9;
        color: #475569;
        border: 1px solid #e2e8f0;
    }

    .comp-tag.tag-distance { background: #eff6ff; color: #1e40af; border-color: #bfdbfe; }
    .comp-tag.tag-sale     { background: #f0fdf4; color: #166534; border-color: #bbf7d0; }
    .comp-tag.tag-price    { background: #fefce8; color: #854d0e; border-color: #fde68a; }
    .comp-tag.tag-size     { background: #faf5ff; color: #6b21a8; border-color: #e9d5ff; }
    .comp-tag.tag-muted    { background: #f9fafb; color: #9ca3af; border-color: #e5e7eb; font-style: italic; }
    .comp-tag.tag-ppsf     { background: #fff7ed; color: #9a3412; border-color: #fed7aa; }

    .comp-gmaps-link {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        font-size: 11px;
        color: #1e40af;
        text-decoration: none;
        margin-top: 6px;
        opacity: 0.75;
        transition: opacity 0.15s;
    }
    .comp-gmaps-link:hover { opacity: 1; text-decoration: underline; }

    .comp-distance {
        font-size: 11px;
        font-weight: 600;
        color: #1e40af;
        flex-shrink: 0;
        margin-top: 2px;
    }

    /* ── No comps state ──────────────────────────────────── */
    .no-comps {
        text-align: center;
        padding: 32px 16px;
        color: #6b7280;
        font-size: 14px;
        border: 1px dashed #d1d5db;
        border-radius: 8px;
    }

    /* ── Action bar ──────────────────────────────────────── */
    .comps-actions {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        padding-top: 4px;
    }

    .selected-count {
        font-size: 13px;
        color: #374151;
    }

    .selected-count strong { color: #111827; }

    .btn-save-comps {
        height: 38px;
        padding: 0 20px;
        background: #1e40af;
        color: #fff;
        border: none;
        border-radius: 6px;
        font-size: 14px;
        font-weight: 600;
        cursor: pointer;
        transition: background 0.15s;
        display: inline-flex;
        align-items: center;
        gap: 6px;
    }
    .btn-save-comps:hover    { background: #1d3a9e; }
    .btn-save-comps:disabled { background: #93c5fd; cursor: not-allowed; }

    .btn-skip {
        height: 38px;
        padding: 0 16px;
        background: transparent;
        color: #6b7280;
        border: 1px solid #d1d5db;
        border-radius: 6px;
        font-size: 13px;
        font-weight: 500;
        cursor: pointer;
        transition: border-color 0.15s, color 0.15s;
    }
    .btn-skip:hover { border-color: #9ca3af; color: #374151; }

    /* ── Spinner ─────────────────────────────────────────── */
    .spinner {
        display: inline-block;
        width: 12px; height: 12px;
        border: 2px solid rgba(255,255,255,0.4);
        border-top-color: #fff;
        border-radius: 50%;
        animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    /* Turn the default blue Leaflet marker red for the subject property */
    .marker-red { filter: hue-rotate(140deg) saturate(2); }

    /* ── Loading state ───────────────────────────────────── */
    .loading-state {
        text-align: center;
        padding: 48px 16px;
        color: #6b7280;
        font-size: 14px;
    }

    .btn-refresh-comps {
        display: inline-flex;
        align-items: center;
        gap: 5px;
        font-size: 12px;
        font-weight: 500;
        color: #6b7280;
        background: transparent;
        border: 1px solid #e5e7eb;
        border-radius: 5px;
        padding: 4px 10px;
        cursor: pointer;
        transition: color 0.15s, border-color 0.15s;
        text-decoration: none;
        margin-top: 4px;
    }
    .btn-refresh-comps:hover { color: #374151; border-color: #9ca3af; }
    .btn-refresh-comps svg   { flex-shrink: 0; }

    .loading-spinner-lg {
        display: inline-block;
        width: 28px; height: 28px;
        border: 3px solid #e5e7eb;
        border-top-color: #1e40af;
        border-radius: 50%;
        animation: spin 0.8s linear infinite;
        margin-bottom: 12px;
    }
</style>

<div class="row mb-3">
    <div class="col-12">
        <div class="page-title-head d-flex align-items-sm-center flex-sm-row flex-column gap-2">
            <div class="flex-grow-1">
                <h4 class="fs-lg fw-bold mb-1">Review Comparables</h4>
                <p class="text-muted mb-0 fs-xs" id="headingSubtext">Loading comparable properties...</p>
            </div>
            <div>
                <button type="button" class="btn btn-outline-secondary btn-sm" id="btnRefreshComps"
                        onclick="refreshComps()" style="display:none;">
                    <i class="ti ti-refresh me-1"></i>Refresh comps
                </button>
            </div>
        </div>
    </div>
</div>

<div class="alert d-none" id="pageAlert" role="alert"></div>

<div class="row g-3">

    <%-- Left: list panel --%>
    <div class="col-12 col-xl-4">

        <%-- Subject property card --%>
        <div class="card border-primary mb-3" id="subjectCard" style="display:none;">
            <img id="subjectThumb" class="card-img-top" alt="" style="display:none;height:140px;object-fit:cover;"
                 onerror="this.style.display='none'" />
            <div class="card-body pb-2">
                <p class="text-primary fs-xxs fw-bold text-uppercase mb-1">Subject Property</p>
                <h6 class="fw-bold mb-1" id="subjectAddress"></h6>
                <div class="comp-tags mb-2" id="subjectTags"></div>
                <div class="d-flex gap-2 flex-wrap">
                    <a id="subjectDetailLink" href="#" class="btn btn-outline-primary btn-sm">
                        <i class="ti ti-zoom-in me-1"></i>View Detail
                    </a>
                    <a id="subjectMapsLink" href="#" class="btn btn-outline-secondary btn-sm" target="_blank" rel="noopener">
                        <i class="ti ti-map-pin me-1"></i>Street View
                    </a>
                </div>
            </div>
        </div>

        <%-- Loading state --%>
        <div class="text-center py-5" id="loadingState">
            <div class="spinner-border text-primary mb-3" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="text-muted">Fetching comparable properties...</p>
        </div>

        <%-- Comp list --%>
        <div id="compListWrap" style="display:none;">
            <%-- Sort bar --%>
            <div class="d-flex align-items-center gap-1 flex-wrap mb-2">
                <span class="text-muted fs-xxs fw-bold text-uppercase me-1">Sort:</span>
                <button type="button" class="btn btn-primary btn-sm py-0 px-2" id="srt-distance"  onclick="setSort('distance')">Distance <span id="arr-distance">&darr;</span></button>
                <button type="button" class="btn btn-outline-secondary btn-sm py-0 px-2" id="srt-saleDate"  onclick="setSort('saleDate')">Date Sold <span id="arr-saleDate"></span></button>
                <button type="button" class="btn btn-outline-secondary btn-sm py-0 px-2" id="srt-salePrice" onclick="setSort('salePrice')">Price <span id="arr-salePrice"></span></button>
                <button type="button" class="btn btn-outline-secondary btn-sm py-0 px-2" id="srt-ppsf"      onclick="setSort('ppsf')">$/sqft <span id="arr-ppsf"></span></button>
                <button type="button" class="btn btn-outline-secondary btn-sm py-0 px-2" id="srt-sqft"      onclick="setSort('sqft')">Sq Ft <span id="arr-sqft"></span></button>
                <button type="button" class="btn btn-outline-secondary btn-sm py-0 px-2" id="srt-yearBuilt" onclick="setSort('yearBuilt')">Yr Built <span id="arr-yearBuilt"></span></button>
            </div>
            <%-- Header row --%>
            <div class="d-flex align-items-center justify-content-between mb-2">
                <span class="text-muted fs-xxs fw-bold text-uppercase" id="compListLabel">Comparables</span>
                <div class="form-check mb-0">
                    <input class="form-check-input" type="checkbox" id="chkSelectAll" onchange="toggleSelectAll(this)" />
                    <label class="form-check-label fs-xs" for="chkSelectAll">Select all</label>
                </div>
            </div>
            <div class="comp-list" id="compList"></div>
            <%-- Action bar --%>
            <div class="d-flex align-items-center justify-content-between gap-2 pt-2 border-top">
                <span class="text-muted fs-xs"><strong id="selectedCountLabel">0</strong> selected</span>
                <div class="d-flex gap-2">
                    <button type="button" class="btn btn-outline-secondary btn-sm" onclick="skipComps()">Skip</button>
                    <button type="button" class="btn btn-primary btn-sm" id="btnSaveComps" onclick="saveComps()">
                        <i class="ti ti-device-floppy me-1"></i>Save Selected
                    </button>
                </div>
            </div>
        </div>

        <%-- No comps state --%>
        <div class="text-center py-4 border border-dashed rounded" id="noCompsState" style="display:none;">
            <i class="ti ti-map-search fs-36 text-muted d-block mb-2"></i>
            <p class="text-muted mb-3">No comparable properties found within the search radius.</p>
            <button type="button" class="btn btn-outline-secondary btn-sm" onclick="skipComps()">
                Continue without comps
            </button>
        </div>

    </div>

    <%-- Right: map panel --%>
    <div class="col-12 col-xl-8">
        <div id="compsMap" style="height:calc(100vh - 260px);min-height:400px;border-radius:8px;border:1px solid #e5e7eb;"></div>
    </div>

</div>

<!-- Hidden fields -->
<asp:HiddenField ID="hdnStreetViewKey" runat="server" />
<asp:HiddenField ID="hdnProspectId"    runat="server" />
<asp:HiddenField ID="hdnPropertyId"    runat="server" />
<asp:HiddenField ID="hdnSubjectClip"   runat="server" />
<asp:HiddenField ID="hdnSubjectLat"    runat="server" />
<asp:HiddenField ID="hdnSubjectLng"    runat="server" />
<asp:HiddenField ID="hdnSubjectAddr"     runat="server" />
<asp:HiddenField ID="hdnSubjectSqFt"     runat="server" />
<asp:HiddenField ID="hdnSubjectYearBuilt" runat="server" />
<asp:HiddenField ID="hdnSubjectSalePrice" runat="server" />
<asp:HiddenField ID="hdnSubjectSaleDate"  runat="server" />
<asp:HiddenField ID="hdnSubjectPpsf"      runat="server" />
<asp:HiddenField ID="hdnRadiusMiles"   runat="server" />
<asp:HiddenField ID="hdnCompsJson"     runat="server" />
<asp:HiddenField ID="hdnSelectedClips" runat="server" />
<asp:HiddenField ID="hdnMessage"       runat="server" />
<asp:HiddenField ID="hdnMessageType"   runat="server" />

<script src="/Theme/assets/plugins/leaflet/leaflet.js"></script>
<script>
(function () {

    // ── ClientID map ──────────────────────────────────────
    var flds = {
        hdnProspectId    : '<%= hdnProspectId.ClientID %>',
        hdnPropertyId    : '<%= hdnPropertyId.ClientID %>',
        hdnSubjectClip   : '<%= hdnSubjectClip.ClientID %>',
        hdnSubjectLat    : '<%= hdnSubjectLat.ClientID %>',
        hdnSubjectLng    : '<%= hdnSubjectLng.ClientID %>',
        hdnSubjectAddr   : '<%= hdnSubjectAddr.ClientID %>',
        hdnSubjectSqFt   : '<%= hdnSubjectSqFt.ClientID %>',
        hdnSubjectYearBuilt : '<%= hdnSubjectYearBuilt.ClientID %>',
        hdnSubjectSalePrice : '<%= hdnSubjectSalePrice.ClientID %>',
        hdnSubjectSaleDate  : '<%= hdnSubjectSaleDate.ClientID %>',
        hdnSubjectPpsf      : '<%= hdnSubjectPpsf.ClientID %>',
        hdnRadiusMiles   : '<%= hdnRadiusMiles.ClientID %>',
        hdnCompsJson     : '<%= hdnCompsJson.ClientID %>',
        hdnSelectedClips : '<%= hdnSelectedClips.ClientID %>',
        hdnMessage       : '<%= hdnMessage.ClientID %>',
        hdnMessageType   : '<%= hdnMessageType.ClientID %>',
        hdnStreetViewKey : '<%= hdnStreetViewKey.ClientID %>'
    };

    function fld(key)          { var el = document.getElementById(flds[key]); return el ? el.value : ''; }
    function setFld(key, val)  { var el = document.getElementById(flds[key]); if (el) el.value = val; }

    var map         = null;
    var comps       = [];
    var markers     = {};   // clip -> L.marker
    var subjectMarker = null;

    var subjectIcon = L.icon({
        iconUrl     : '/Theme/assets/images/leaflet/marker-icon.png',
        shadowUrl   : '/Theme/assets/images/leaflet/marker-shadow.png',
        iconSize    : [25, 41],
        iconAnchor  : [12, 41],
        popupAnchor : [1, -34],
        className   : 'marker-red'
    });

    var compIcon = L.icon({
        iconUrl     : '/Theme/assets/images/leaflet/marker-icon.png',
        shadowUrl   : '/Theme/assets/images/leaflet/marker-shadow.png',
        iconSize    : [20, 33],
        iconAnchor  : [10, 33],
        popupAnchor : [1, -28],
        className   : 'comp-marker-dim'
    });

    // ── On load ───────────────────────────────────────────
    window.addEventListener('DOMContentLoaded', function () {

        // Show message from postback if any
        var msg  = fld('hdnMessage');
        var type = fld('hdnMessageType') || 'info';
        if (msg) showAlert(msg, type);

        var compsJson = fld('hdnCompsJson');

        if (compsJson) {
            // Postback returned — comps already in hidden field
            try { comps = JSON.parse(compsJson); } catch (e) {}
            initPage();
        } else {
            // First load — server-side already populated fields, but
            // comps are fetched server-side and put in hdnCompsJson on Page_Load.
            // If still empty after postback something went wrong.
            document.getElementById('loadingState').style.display = 'none';
            document.getElementById('noCompsState').style.display = 'block';
        }
    });

    // ── Init page after comps are available ───────────────
    function initPage() {
        var subjectLat  = parseFloat(fld('hdnSubjectLat'))  || 0;
        var subjectLng  = parseFloat(fld('hdnSubjectLng'))  || 0;
        var subjectAddr = fld('hdnSubjectAddr');
        var radius      = parseFloat(fld('hdnRadiusMiles')) || 0.5;

        // Subject card
        document.getElementById('subjectAddress').textContent = subjectAddr;
        document.getElementById('subjectCard').style.display  = 'block';

        // Subject street view thumb
        var svKey = fld('hdnStreetViewKey');
        if (subjectLat && subjectLng && svKey) {
            var thumb = document.getElementById('subjectThumb');
            thumb.src = streetViewUrl(subjectLat, subjectLng, 380, 110);
            thumb.style.display = 'block';
        }

        // Subject stat tags — same style as comp tags
        var subjectTags = [];
        var subSqFt      = fld('hdnSubjectSqFt');
        var subYrBuilt   = fld('hdnSubjectYearBuilt');
        var subSalePrice = fld('hdnSubjectSalePrice');
        var subSaleDate  = fld('hdnSubjectSaleDate');

        // Calculate ppsf client-side from price / sqft
        var subPpsf = '';
        if (subSalePrice && subSqFt && parseFloat(subSqFt) > 0)
            subPpsf = (parseFloat(subSalePrice) / parseFloat(subSqFt)).toFixed(2);

        if (subSaleDate)
            subjectTags.push({ label: 'Sold ' + formatYearMonth(subSaleDate), cls: 'tag-sale' });
        if (subSalePrice)
            subjectTags.push({ label: '$' + Math.round(parseFloat(subSalePrice)).toLocaleString(), cls: 'tag-price' });
        if (subSqFt)
            subjectTags.push({ label: parseInt(subSqFt).toLocaleString() + ' sqft', cls: 'tag-size' });
        if (subPpsf)
            subjectTags.push({ label: '$' + Math.round(parseFloat(subPpsf)).toLocaleString() + '/sqft', cls: 'tag-ppsf' });
        if (subYrBuilt)
            subjectTags.push({ label: 'Built ' + subYrBuilt, cls: '' });

        var subjectTagsEl = document.getElementById('subjectTags');
        if (subjectTagsEl) {
            subjectTagsEl.innerHTML = subjectTags.length
                ? subjectTags.map(function(t) {
                    return '<span class="comp-tag ' + t.cls + '">' + esc(t.label) + '</span>';
                  }).join('')
                : '';
        }

        // Subject detail link
        var clip = fld('hdnSubjectClip');
        if (clip) {
            document.getElementById('subjectDetailLink').href =
                '/Secure/Prospects/PropertyDetail.aspx?clip=' + encodeURIComponent(clip) +
                '&prospectId=' + encodeURIComponent(fld('hdnProspectId'));
        }

        // Subject street view maps link
        if (subjectLat && subjectLng) {
            document.getElementById('subjectMapsLink').href =
                'https://www.google.com/maps/@?api=1&map_action=pano&viewpoint=' + subjectLat + ',' + subjectLng;
        }
        document.getElementById('headingSubtext').textContent =
            comps.length + ' comparable' + (comps.length !== 1 ? 's' : '') +
            ' found within ' + radius + ' mile' + (radius !== 1 ? 's' : '');

        // Hide loading, show list — reveal refresh button now we have data
        document.getElementById('loadingState').style.display   = 'none';
        document.getElementById('btnRefreshComps').style.display = 'inline-flex';

        if (comps.length === 0) {
            document.getElementById('noCompsState').style.display = 'block';
        } else {
            document.getElementById('compListWrap').style.display = 'block';
            renderCompList();
        }

        // Init map
        initMap(subjectLat, subjectLng, subjectAddr, radius);
    }

    // ── Sort state ────────────────────────────────────────
    var currentSort = { field: 'distance', dir: 'asc' };

    var sortFields = {
        distance : function(c) { return c.distance        != null ? c.distance        : 9999; },
        saleDate  : function(c) { return c.saleDate        ? c.saleDate        : ''; },
        salePrice : function(c) { return c.salePrice       != null ? c.salePrice       : -1; },
        ppsf      : function(c) { return c.pricePerSquareFoot != null ? c.pricePerSquareFoot : -1; },
        sqft      : function(c) { return c.buildingSquareFeet != null ? c.buildingSquareFeet : -1; },
        yearBuilt : function(c) { return c.yearBuilt       ? c.yearBuilt       : ''; }
    };

    // Fields where lower = better (ascending default)
    var ascDefault = { distance: true, yearBuilt: true };

    window.setSort = function (field) {
        if (currentSort.field === field) {
            // Toggle direction
            currentSort.dir = (currentSort.dir === 'asc') ? 'desc' : 'asc';
        } else {
            currentSort.field = field;
            // Distance and year built default ascending; everything else descending
            currentSort.dir = ascDefault[field] ? 'asc' : 'desc';
        }

        // Update button styles and arrows
        ['distance','saleDate','salePrice','ppsf','sqft','yearBuilt'].forEach(function(f) {
            var btn = document.getElementById('srt-' + f);
            var arr = document.getElementById('arr-' + f);
            if (f === currentSort.field) {
                btn.className = 'btn btn-primary btn-sm py-0 px-2';
                arr.innerHTML = currentSort.dir === 'asc' ? '&darr;' : '&uarr;';
            } else {
                btn.className = 'btn btn-outline-secondary btn-sm py-0 px-2';
                arr.innerHTML = '';
            }
        });

        // Re-sort comps array in place
        var fn = sortFields[field];
        comps.sort(function(a, b) {
            var av = fn(a), bv = fn(b);
            if (av < bv) return currentSort.dir === 'asc' ? -1 :  1;
            if (av > bv) return currentSort.dir === 'asc' ?  1 : -1;
            return 0;
        });

        renderCompList();
    };

    // ── Render comp list ──────────────────────────────────
    function renderCompList() {
        var list = document.getElementById('compList');

        // Preserve checked state across re-renders
        var checkedClips = {};
        list.querySelectorAll('input[type=checkbox]').forEach(function(chk) {
            var item = chk.closest('.comp-item');
            if (item) checkedClips[item.dataset.clip] = chk.checked;
        });
        var firstRender = Object.keys(checkedClips).length === 0;

        list.innerHTML = '';

        comps.forEach(function (comp, i) {
            var item = document.createElement('div');
            item.className    = 'comp-item';
            item.id           = 'compItem_' + i;
            item.dataset.clip = comp.clip;

            // Always show address — it is the primary identifier.
            // Then surface the fields that make it a comparable.
            var address = esc(comp.streetAddress || '(Address unavailable)');
            var cityLine = [comp.city, comp.state, comp.zip]
                .filter(function(v){ return v; }).join(', ');

            // Comparability signals — show what we have, graceful fallback per field
            var tags = [];

            if (comp.distance != null)
                tags.push({ label: (Math.round(comp.distance * 100) / 100) + ' mi away', cls: 'tag-distance' });

            if (comp.saleDate)
                tags.push({ label: 'Sold ' + formatYearMonth(comp.saleDate), cls: 'tag-sale' });

            if (comp.salePrice)
                tags.push({ label: '$' + Math.round(comp.salePrice).toLocaleString(), cls: 'tag-price' });

            if (comp.buildingSquareFeet)
                tags.push({ label: comp.buildingSquareFeet.toLocaleString() + ' sqft', cls: 'tag-size' });

            if (comp.pricePerSquareFoot)
                tags.push({ label: '$' + Math.round(comp.pricePerSquareFoot).toLocaleString() + '/sqft', cls: 'tag-ppsf' });

            if (comp.yearBuilt)
                tags.push({ label: 'Built ' + comp.yearBuilt, cls: '' });

            var tagsHtml = tags.length
                ? tags.map(function(t){
                    return '<span class="comp-tag ' + t.cls + '">' + esc(t.label) + '</span>';
                  }).join('')
                : '<span class="comp-tag tag-muted">No sale data in UAT</span>';

            // First render: all checked. Re-render: restore previous state.
            var isChecked = firstRender
                ? true
                : (checkedClips[comp.clip] !== undefined ? checkedClips[comp.clip] : true);

            // Street View thumbnail — only render if we have coordinates
            var thumbHtml = '';
            if (comp.latitude && comp.longitude) {
                var svUrl = streetViewUrl(comp.latitude, comp.longitude, 380, 110);
                thumbHtml =
                    '<img class="comp-thumb" ' +
                        'src="' + svUrl + '" ' +
                        'alt="" ' +
                        'onerror="this.style.display=\'none\'" ' +
                    '/>';
            }

            item.innerHTML =
                thumbHtml +
                '<div class="comp-item-inner">' +
                    '<input type="checkbox" id="chk_' + i + '" ' + (isChecked ? 'checked' : '') + ' ' +
                           'onchange="onCompCheck()" ' +
                           'onclick="event.stopPropagation()" />' +
                    '<div class="comp-item-body">' +
                        '<div class="comp-address">' + address + '</div>' +
                        (cityLine ? '<div class="comp-city">' + esc(cityLine) + '</div>' : '') +
                        '<div class="comp-tags">' + tagsHtml + '</div>' +
                        (comp.latitude && comp.longitude
                            ? '<a class="comp-gmaps-link" href="' + googleMapsUrl(comp.latitude, comp.longitude, comp.streetAddress) + '" ' +
                              'target="_blank" rel="noopener" onclick="event.stopPropagation()">' +
                              '<svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M18 13v6a2 2 0 01-2 2H5a2 2 0 01-2-2V8a2 2 0 012-2h6"/><polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/></svg>' +
                              ' Street View</a>'
                            : '') +
                        (comp.clip
                            ? '<a class="comp-gmaps-link" href="/Secure/Prospects/PropertyDetail.aspx?clip=' +
                              encodeURIComponent(comp.clip) + '" ' +
                              'onclick="event.stopPropagation()">' +
                              '<svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>' +
                              ' Detail</a>'
                            : '') +
                    '</div>' +
                '</div>';

            item.addEventListener('click', function (e) {
                if (e.target.type === 'checkbox') return;
                highlightComp(i, comp.clip);
            });

            list.appendChild(item);
        });

        document.getElementById('compListLabel').textContent =
            comps.length + ' comparable' + (comps.length !== 1 ? 's' : '');

        updateSelectedCount();
    }

    // ── Init Leaflet map ──────────────────────────────────
    function initMap(subjectLat, subjectLng, subjectAddr, radiusMiles) {
        var center = (subjectLat && subjectLng)
            ? [subjectLat, subjectLng]
            : [29.95, -90.07]; // New Orleans fallback

        map = L.map('compsMap').setView(center, 15);

        L.tileLayer('https://{s}.tile.osm.org/{z}/{x}/{y}.png', {
            attribution : 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a>',
            maxZoom     : 18
        }).addTo(map);

        // Subject marker
        if (subjectLat && subjectLng) {
            var svKey2 = fld('hdnStreetViewKey');
            var subjectSvThumb = (svKey2)
                ? '<img src="' + streetViewUrl(subjectLat, subjectLng, 220, 120) +
                  '" style="width:220px;height:120px;object-fit:cover;display:block;border-radius:4px;margin-bottom:6px;" ' +
                  'onerror="this.style.display=\'none\'" />' : '';

            var subjectClip = fld('hdnSubjectClip');
            var subjectDetailLink = subjectClip
                ? '<br/><a href="/Secure/Prospects/PropertyDetail.aspx?clip=' +
                    encodeURIComponent(subjectClip) + '&prospectId=' +
                    encodeURIComponent(fld('hdnProspectId')) +
                  '" style="font-size:12px;color:#1e40af;">View Detail &rarr;</a>' : '';

            // Compute ppsf from price / sqft
            var mapSubPpsf = '';
            var mapPrice = fld('hdnSubjectSalePrice');
            var mapSqFt  = fld('hdnSubjectSqFt');
            if (mapPrice && mapSqFt && parseFloat(mapSqFt) > 0)
                mapSubPpsf = Math.round(parseFloat(mapPrice) / parseFloat(mapSqFt)).toString();

            subjectMarker = L.marker([subjectLat, subjectLng], { icon: subjectIcon })
                .addTo(map)
                .bindPopup(
                    subjectSvThumb +
                    '<div style="font-size:13px;">' +
                        '<strong>Subject Property</strong><br/>' +
                        esc(subjectAddr) + '<br/>' +
                        (fld('hdnSubjectSaleDate')  ? '<span style="font-size:11px;color:#374151;">Sold ' + formatYearMonth(fld('hdnSubjectSaleDate')) + '</span><br/>' : '') +
                        (mapPrice                   ? '<span style="font-size:11px;color:#374151;">$' + Math.round(parseFloat(mapPrice)).toLocaleString() + '</span><br/>' : '') +
                        (mapSqFt                    ? '<span style="font-size:11px;color:#374151;">' + parseInt(mapSqFt).toLocaleString() + ' sqft</span><br/>' : '') +
                        (mapSubPpsf                 ? '<span style="font-size:11px;color:#374151;">$' + mapSubPpsf + '/sqft</span><br/>' : '') +
                        (fld('hdnSubjectYearBuilt') ? '<span style="font-size:11px;color:#374151;">Built ' + fld('hdnSubjectYearBuilt') + '</span><br/>' : '') +
                        subjectDetailLink +
                    '</div>',
                    { maxWidth: 240 }
                );

            // Radius circle
            var radiusMeters = radiusMiles * 1609.34;
            L.circle([subjectLat, subjectLng], {
                color       : '#1e40af',
                fillColor   : '#3b82f6',
                fillOpacity : 0.08,
                radius      : radiusMeters,
                weight      : 1.5
            }).addTo(map);
        }

        // Comp markers
        comps.forEach(function (comp, i) {
            if (!comp.latitude || !comp.longitude) return;

            (function(comp, i) {
                var svThumb = comp.latitude && comp.longitude
                    ? '<img src="' + streetViewUrl(comp.latitude, comp.longitude, 220, 120) +
                      '" style="width:220px;height:120px;object-fit:cover;display:block;border-radius:4px;margin-bottom:6px;" ' +
                      'onerror="this.style.display=\'none\'" />' : '';

                var popup =
                    svThumb +
                    '<div style="font-size:13px;">' +
                        '<strong>' + esc(comp.streetAddress || '') + '</strong>' +
                        (comp.salePrice         ? '<br/>Sale: $' + Math.round(comp.salePrice).toLocaleString() : '') +
                        (comp.pricePerSquareFoot ? '<br/>$' + Math.round(comp.pricePerSquareFoot).toLocaleString() + '/sqft' : '') +
                        (comp.buildingSquareFeet ? '<br/>' + comp.buildingSquareFeet.toLocaleString() + ' sqft' : '') +
                        (comp.distance          ? '<br/>' + (Math.round(comp.distance * 100) / 100) + ' mi away' : '') +
                        '<br/><a href="' + googleMapsUrl(comp.latitude, comp.longitude, comp.streetAddress) + '" ' +
                           'target="_blank" rel="noopener" style="font-size:12px;color:#1e40af;">' +
                           'View in Google Maps &rarr;</a>' +
                    '</div>';

                var marker = L.marker([comp.latitude, comp.longitude], { icon: compIcon })
                    .addTo(map)
                    .bindPopup(popup, { maxWidth: 240 });

                marker.on('click', function () { highlightComp(i, comp.clip); });
                markers[comp.clip] = marker;
            })(comp, i);
        });

        // Fit map to show all markers
        fitMapBounds(subjectLat, subjectLng);
    }

    function fitMapBounds(subjectLat, subjectLng) {
        var bounds = [];
        if (subjectLat && subjectLng) bounds.push([subjectLat, subjectLng]);
        comps.forEach(function (c) {
            if (c.latitude && c.longitude) bounds.push([c.latitude, c.longitude]);
        });
        if (bounds.length > 1) map.fitBounds(bounds, { padding: [30, 30] });
    }

    // ── Highlight comp (list <-> map sync) ────────────────
    function highlightComp(index, clip) {
        // Clear previous highlight
        document.querySelectorAll('.comp-item').forEach(function (el) {
            el.classList.remove('highlighted');
        });

        var item = document.getElementById('compItem_' + index);
        if (item) {
            item.classList.add('highlighted');
            item.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }

        // Open marker popup
        if (markers[clip]) {
            markers[clip].openPopup();
            map.panTo(markers[clip].getLatLng());
        }
    }

    // ── Checkbox helpers ──────────────────────────────────
    window.onCompCheck = function () { updateSelectedCount(); };

    window.toggleSelectAll = function (chk) {
        document.querySelectorAll('.comp-list input[type=checkbox]').forEach(function (c) {
            c.checked = chk.checked;
        });
        updateSelectedCount();
    };

    function updateSelectedCount() {
        var checked = document.querySelectorAll('.comp-list input[type=checkbox]:checked').length;
        document.getElementById('selectedCountLabel').textContent = checked;
        document.getElementById('btnSaveComps').disabled = (checked === 0);

        // Sync select-all checkbox state
        var total = document.querySelectorAll('.comp-list input[type=checkbox]').length;
        var chkAll = document.getElementById('chkSelectAll');
        chkAll.checked       = (checked === total && total > 0);
        chkAll.indeterminate = (checked > 0 && checked < total);
    }

    // ── Save selected comps ───────────────────────────────
    window.saveComps = function () {
        var selectedClips = [];
        comps.forEach(function (comp, i) {
            var chk = document.getElementById('chk_' + i);
            if (chk && chk.checked) selectedClips.push(comp.clip);
        });

        if (selectedClips.length === 0) return;

        setFld('hdnSelectedClips', JSON.stringify(selectedClips));

        var btn = document.getElementById('btnSaveComps');
        btn.disabled  = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';

        __doPostBack('SaveComps', '');
    };

    // ── Refresh comps — clears cache then reloads ────────
    window.refreshComps = function () {
        var btn = document.getElementById('btnRefreshComps');
        btn.disabled     = true;
        btn.style.color  = '#9ca3af';
        __doPostBack('RefreshComps', '');
    };

    // ── Skip comps ────────────────────────────────────────
    window.skipComps = function () {
        var clip = fld('hdnSubjectClip');
        var prospectId = fld('hdnProspectId');
        if (clip) {
            window.location.href = '/Secure/Prospects/PropertyDetail.aspx?clip=' +
                encodeURIComponent(clip) + '&prospectId=' + encodeURIComponent(prospectId);
        } else {
            window.location.href = '/Secure/Prospects/Prospects';
        }
    };

    // ── Helpers ───────────────────────────────────────────
    function showAlert(msg, type) {
        var el = document.getElementById('pageAlert');
        el.className   = 'alert alert-' + type;
        el.textContent = msg;
    }

    // ── Google Maps URL builder ──────────────────────────
    function googleMapsUrl(lat, lng, address) {
        if (lat && lng) {
            return 'https://www.google.com/maps/@?api=1&map_action=pano&viewpoint=' + lat + ',' + lng;
        }
        return 'https://www.google.com/maps/search/?api=1&query=' + encodeURIComponent(address || '');
    }

    // ── Street View URL builder ───────────────────────────
    function streetViewUrl(lat, lng, width, height) {
        var key = fld('hdnStreetViewKey');
        return 'https://maps.googleapis.com/maps/api/streetview' +
            '?size=' + width + 'x' + height +
            '&location=' + lat + ',' + lng +
            '&fov=90&pitch=0&source=outdoor' +
            '&key=' + key;
    }

    // Format YYYYMMDD or YYYY-MM-DD to "Mon YYYY"
    function formatYearMonth(val) {
        if (!val) return '';
        var s = String(val).replace(/-/g, '');
        if (s.length < 6) return s;
        var months = ['Jan','Feb','Mar','Apr','May','Jun',
                      'Jul','Aug','Sep','Oct','Nov','Dec'];
        var year  = s.substring(0, 4);
        var month = parseInt(s.substring(4, 6), 10);
        var mon   = (month >= 1 && month <= 12) ? months[month - 1] : '';
        return mon ? mon + ' ' + year : year;
    }

    function esc(str) {
        return String(str || '')
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

})();
</script>

</asp:Content>
