<%@ Page Title="Reyla Property Search" Language="C#" MasterPageFile="~/Reyla.Master" AutoEventWireup="true" CodeBehind="Search.aspx.cs" Inherits="Reyla.Secure.Prospects.Search" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

<div class="row">
    <div class="col-12">

        <%-- Page header --%>
        <div class="page-title-head d-flex align-items-sm-center flex-sm-row flex-column gap-2 mb-3">
            <div class="flex-grow-1">
                <h4 class="fs-lg fw-bold mb-1">Property Search</h4>
                <p class="text-muted mb-0 fs-xs">Search for a property to add as a prospect.</p>
            </div>
            <div class="text-end">
                <ol class="breadcrumb m-0 py-0 fs-xs">
                    <li class="breadcrumb-item"><a href="/Secure/Index">Home</a></li>
                    <li class="breadcrumb-item"><a href="/Secure/Prospects/Prospects">Prospects</a></li>
                    <li class="breadcrumb-item active">Search</li>
                </ol>
            </div>
        </div>

        <%-- Search card --%>
        <div class="card mb-3">
            <div class="card-body">
                <div class="row g-2 align-items-end">
                    <div class="col-12 col-md">
                        <asp:TextBox ID="txtStreetAddress" runat="server"
                            CssClass="form-control"
                            placeholder="Street address" MaxLength="200" />
                    </div>
                    <div class="col-12 col-sm-5 col-md-3">
                        <asp:TextBox ID="txtCity" runat="server"
                            CssClass="form-control"
                            placeholder="City" MaxLength="100" />
                    </div>
                    <div class="col-4 col-sm-2 col-md-1">
                        <asp:TextBox ID="txtState" runat="server"
                            CssClass="form-control"
                            placeholder="ST" MaxLength="2" />
                    </div>
                    <div class="col-8 col-sm-3 col-md-2">
                        <asp:TextBox ID="txtZip" runat="server"
                            CssClass="form-control"
                            placeholder="Zip code" MaxLength="5" />
                    </div>
                    <div class="col-12 col-sm-auto">
                        <button type="button" class="btn btn-primary w-100" id="btnSearch" onclick="runSearch()">
                            <i class="ti ti-search me-1"></i> Search
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <%-- Alert --%>
        <div class="alert d-none" id="pageAlert" role="alert"></div>

        <%-- Results --%>
        <div id="resultsList" class="d-none">
            <p class="text-muted fs-xs fw-semibold text-uppercase mb-2" id="resultsLabel"></p>
            <div id="resultsContainer" class="row g-3"></div>
        </div>

    </div>
</div>

<%-- Hidden fields --%>
<asp:HiddenField ID="hdnPropertyId"    runat="server" />
<asp:HiddenField ID="hdnStreetViewKey" runat="server" />
<asp:HiddenField ID="hdnClip"          runat="server" />
<asp:HiddenField ID="hdnDetailClip"    runat="server" />
<asp:HiddenField ID="hdnLat"           runat="server" />
<asp:HiddenField ID="hdnLng"           runat="server" />
<asp:HiddenField ID="hdnAddress"       runat="server" />
<asp:HiddenField ID="hdnResultsJson"   runat="server" />
<asp:HiddenField ID="hdnMessage"       runat="server" />
<asp:HiddenField ID="hdnMessageType"   runat="server" />

<style>
    .result-sv-thumb      { width:100%; height:180px; object-fit:cover;
                            border-radius:6px 6px 0 0; display:block; }
    .result-card-body     { padding:12px 14px; }
    .result-address       { font-size:15px; font-weight:600; color:#111827; margin-bottom:2px; }
    .result-meta          { font-size:13px; color:#6b7280; margin-bottom:10px; }
    .spinner-inline       { display:inline-block; width:11px; height:11px;
                            border:2px solid rgba(255,255,255,.4); border-top-color:#fff;
                            border-radius:50%; animation:spin .7s linear infinite; }
    @keyframes spin        { to { transform:rotate(360deg); } }
</style>

<script>
(function () {
    var results = [];

    var flds = {
        hdnPropertyId  : '<%= hdnPropertyId.ClientID %>',
        hdnClip        : '<%= hdnClip.ClientID %>',
        hdnLat         : '<%= hdnLat.ClientID %>',
        hdnLng         : '<%= hdnLng.ClientID %>',
        hdnAddress     : '<%= hdnAddress.ClientID %>',
        hdnResultsJson : '<%= hdnResultsJson.ClientID %>',
        hdnMessage     : '<%= hdnMessage.ClientID %>',
        hdnMessageType : '<%= hdnMessageType.ClientID %>'
    };

    function fld(key)         { var el = document.getElementById(flds[key]); return el ? el.value : ''; }
    function setFld(key, val) { var el = document.getElementById(flds[key]); if (el) el.value = val; }

    window.addEventListener('DOMContentLoaded', function () {
        var raw = fld('hdnResultsJson');
        if (raw) { try { results = JSON.parse(raw); } catch(e) {} }
        if (results.length > 0) renderResults(results);

        var msg  = fld('hdnMessage');
        var type = fld('hdnMessageType') || 'info';
        if (msg) showAlert(msg, type);

        document.getElementById('btnSearch').disabled = false;
    });

    function renderResults(items) {
        var container = document.getElementById('resultsContainer');
        container.innerHTML = '';

        items.forEach(function (item, i) {
            var col = document.createElement('div');
            col.className = 'col-12 col-md-6 col-xl-4';

            var svKey  = document.getElementById('<%= hdnStreetViewKey.ClientID %>').value;
            var svHtml = '';
            if (item.Lat && item.Lng && svKey) {
                svHtml = '<img class="result-sv-thumb" ' +
                    'src="https://maps.googleapis.com/maps/api/streetview' +
                    '?size=600x180&location=' + item.Lat + ',' + item.Lng +
                    '&fov=90&pitch=0&source=outdoor&key=' + svKey + '" ' +
                    'alt="" onerror="this.style.display=\'none\'"/>';
            }

            var badge = item.IsProspect
                ? '<span class="badge text-bg-success me-1"><i class="ti ti-check me-1"></i>Prospect</span>'
                : '';

            var btns = '';
            if (item.IsProspect) {
                btns = '<a href="/Secure/Prospects/Comps.aspx?prospectId=' + item.ProspectId + '" ' +
                       'class="btn btn-outline-primary btn-sm">Review Comps</a> ';
            } else {
                btns = '<button type="button" class="btn btn-success btn-sm" id="btnAdd_' + i + '" ' +
                       'onclick="addProspect(' + i + ', this)">' +
                       '<i class="ti ti-plus me-1"></i>Add Prospect</button> ';
            }
            btns += '<button type="button" class="btn btn-outline-secondary btn-sm" ' +
                    'onclick="viewDetail(\'' + (item.Clip || '') + '\', this)">' +
                    '<i class="ti ti-zoom-in me-1"></i>Detail</button>';

            col.innerHTML =
                '<div class="card h-100">' +
                    svHtml +
                    '<div class="result-card-body">' +
                        '<div class="result-address">' + esc(item.StreetAddress) + '</div>' +
                        '<div class="result-meta">' +
                            esc(item.City || '') + ', ' + esc(item.State || '') +
                            ' ' + esc(item.ZipCode || '') +
                        '</div>' +
                        badge +
                        '<div class="mt-2 d-flex gap-1 flex-wrap">' + btns + '</div>' +
                    '</div>' +
                '</div>';

            container.appendChild(col);
        });

        document.getElementById('resultsLabel').textContent =
            items.length + ' result' + (items.length !== 1 ? 's' : '') + ' found';
        document.getElementById('resultsList').classList.remove('d-none');
        hideAlert();
    }

    window.viewDetail = function (clip, btn) {
        if (!clip) return;
        btn.disabled  = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>';
        document.getElementById('<%= hdnDetailClip.ClientID %>').value = clip;
        __doPostBack('ViewDetail', '');
    };

    window.addProspect = function (index, btn) {
        var item = results[index];
        if (!item) return;
        setFld('hdnPropertyId', item.PropertyId || '');
        setFld('hdnClip',       item.Clip       || '');
        setFld('hdnLat',        item.Lat  != null ? String(item.Lat)  : '');
        setFld('hdnLng',        item.Lng  != null ? String(item.Lng)  : '');
        setFld('hdnAddress',    item.StreetAddress + ', ' + (item.City || '') + ' ' + (item.ZipCode || ''));
        btn.disabled  = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';
        __doPostBack('CreateProspect', '');
    };

    window.runSearch = function () {
        var street = document.getElementById('<%= txtStreetAddress.ClientID %>').value.trim();
        var city   = document.getElementById('<%= txtCity.ClientID %>').value.trim();
        var zip    = document.getElementById('<%= txtZip.ClientID %>').value.trim();

        if (!street) { showAlert('Please enter a street address.', 'danger'); return; }
        if (!city && !zip) { showAlert('Please enter a city or zip code.', 'danger'); return; }

        results = [];
        document.getElementById('resultsList').classList.add('d-none');
        document.getElementById('resultsContainer').innerHTML = '';
        document.getElementById('btnSearch').disabled = true;
        showAlert('Searching...', 'info');
        __doPostBack('SearchProperty', '');
    };

    function showAlert(msg, type) {
        var el = document.getElementById('pageAlert');
        el.className   = 'alert alert-' + type;
        el.textContent = msg;
    }
    function hideAlert() {
        var el = document.getElementById('pageAlert');
        el.className = 'alert d-none';
    }
    function esc(str) {
        return String(str || '')
            .replace(/&/g,'&amp;').replace(/</g,'&lt;')
            .replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }
})();
</script>

</asp:Content>
