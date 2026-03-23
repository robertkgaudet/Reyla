<%@ Page Title="Parcel Map" Language="C#" MasterPageFile="~/Reyla.Master"
         AutoEventWireup="true" CodeBehind="ProspectMap.aspx.cs"
         Inherits="Reyla.Secure.Prospects.ProspectMap" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <link href="/Theme/assets/plugins/leaflet/leaflet.css" rel="stylesheet">

    <style>
        #parcelMap {
            height: calc(100vh - 200px);
            min-height: 480px;
        }
        @media (max-width: 768px) {
            #parcelMap { height: calc(100vh - 160px); }
        }

        /* ── Parcel popup ──────────────────────────────────────────── */
        .parcel-popup { font-size: 13px; min-width: 240px; }
        .parcel-popup .pp-address  { font-weight: 700; font-size: 14px; margin-bottom: 2px; }
        .parcel-popup .pp-owner    { color: #1e40af; font-weight: 600; margin-bottom: 6px; font-size: 13px; }
        .parcel-popup .pp-facts    { display: flex; flex-wrap: wrap; gap: 4px; margin-bottom: 8px; }
        .parcel-popup .pp-fact     { background: #f1f5f9; border-radius: 4px; padding: 2px 7px;
                                      font-size: 11px; color: #374151; }
        .parcel-popup .pp-fact.zoning  { background: #eff6ff; color: #1d4ed8; }
        .parcel-popup .pp-fact.value   { background: #f0fdf4; color: #166534; }
        .parcel-popup .pp-fact.sale    { background: #fef9c3; color: #854d0e; }
        .parcel-popup .pp-fact.vacant  { background: #fee2e2; color: #991b1b; }
        .parcel-popup .pp-mail     { font-size: 11px; color: #6b7280; margin-bottom: 8px; }
        .parcel-popup .pp-actions  { display: flex; gap: 6px; }
        .parcel-popup .pp-btn      { flex: 1; padding: 5px 8px; border-radius: 5px; border: none;
                                      font-size: 12px; font-weight: 600; cursor: pointer; text-align: center;
                                      text-decoration: none; display: inline-block; }
        .parcel-popup .pp-btn-primary  { background: #1e40af; color: #fff; }
        .parcel-popup .pp-btn-primary:hover { background: #1e3a8a; color: #fff; }

        /* ── Filter pill bar ───────────────────────────────────────── */
        #filterBar {
            position: absolute; bottom: 36px; left: 50%; transform: translateX(-50%);
            z-index: 1001; display: flex; gap: 6px; flex-wrap: wrap; justify-content: center;
        }
        .filter-pill {
            background: rgba(255,255,255,.92); border: 1.5px solid #d1d5db;
            border-radius: 20px; padding: 4px 14px; font-size: 12px; font-weight: 600;
            cursor: pointer; color: #374151; transition: all .15s; white-space: nowrap;
            backdrop-filter: blur(4px);
        }
        .filter-pill:hover  { border-color: #1e40af; color: #1e40af; }
        .filter-pill.active { background: #1e40af; border-color: #1e40af; color: #fff; }

        /* ── Status bar ────────────────────────────────────────────── */
        #statusBar {
            position: absolute; top: 10px; right: 10px; z-index: 1001;
            background: rgba(255,255,255,.92); border-radius: 6px;
            padding: 5px 12px; font-size: 12px; color: #374151;
            box-shadow: 0 1px 6px rgba(0,0,0,.12); backdrop-filter: blur(4px);
            display: none;
        }

    </style>

    <div class="container-fluid">

        <div class="page-title-head d-flex align-items-center" id="page-head-box">
            <div class="flex-grow-1">
                <h4 class="fs-lg fw-bold mb-2 lh-1">Parcel Prospecting Map</h4>
                <p class="text-muted mb-0 fs-xs lh-1">
                    Zoom to street level (17+) to see parcel outlines. Click any parcel to search in CoreLogic.
                </p>
            </div>
            <div class="text-end d-flex align-items-center gap-2">
                <ol class="breadcrumb m-0 py-0 fs-xs">
                    <li class="breadcrumb-item"><a href="javascript:void(0);">Reyla</a></li>
                    <li class="breadcrumb-item active">Parcel Map</li>
                </ol>
            </div>
        </div>

        <div class="row">
            <div class="col-12">
                <div class="card" style="position:relative; overflow:hidden;">

                    <div id="parcelMap"></div>

                    <div id="statusBar"></div>

                    <div id="filterBar">
                        <span class="filter-pill active" data-filter="all"         onclick="setFilter('all')">All</span>
                        <span class="filter-pill"        data-filter="office"      onclick="setFilter('office')">Office</span>
                        <span class="filter-pill"        data-filter="retail"      onclick="setFilter('retail')">Retail</span>
                        <span class="filter-pill"        data-filter="industrial"  onclick="setFilter('industrial')">Industrial</span>
                        <span class="filter-pill"        data-filter="multifamily" onclick="setFilter('multifamily')">Multi-family</span>
                        <span class="filter-pill"        data-filter="land"        onclick="setFilter('land')">Vacant Land</span>
                    </div>

                </div>
            </div>
        </div>

        <div class="row mt-3">
            <div class="col-12">
                <div class="card">
                    <div class="card-header d-flex align-items-center justify-content-between">
                        <h5 class="card-title mb-0">
                            <i class="ti ti-map-pin me-2 text-primary"></i>
                            Parcel Clicks
                            <span id="clickCountBadge" class="badge text-bg-secondary ms-2">0</span>
                        </h5>
                        <button class="btn btn-outline-danger btn-sm" onclick="clearClickedParcels()">
                            <i class="ti ti-trash me-1"></i>Clear
                        </button>
                    </div>
                    <div class="card-body p-0">
                        <div id="clickedParcelsList" class="p-3 text-muted fs-sm">
                            Click any parcel on the map to track it here.
                        </div>
                    </div>
                </div>
            </div>
        </div>

    </div>

    <script src="/Theme/assets/plugins/leaflet/leaflet.js"></script>

    <script>
    // ── Config ────────────────────────────────────────────────────────────────
    var HAS_REGRID = <%=HasRegridToken.ToString().ToLower()%>;
    var SV_KEY     = '<%=StreetViewKey%>';
    var PROSPECTS  = <%=ProspectsJson%>;

    // ── State ─────────────────────────────────────────────────────────────────
    var currentFilter  = 'all';
    var clickedParcels = [];
    var hoveredLayer   = null;

    // ── LBCS → CRE use-type filter mapping ────────────────────────────────────
    var LBCS_FILTERS = {
        office:      [2100,2200,2300,2400],
        retail:      [2500,2600,2700,2800,3000,3100,3200,3300,3400,3500,3600,3700,3800,3900],
        industrial:  [4000,4100,4200,4300,4400,4500,4600,4700,4800,4900,5000,5100,5200,5300],
        multifamily: [1100,1200,1300,1400,1500,1600,1700,1800,1900],
        land:        [9000,9100,9200,9300,9400,9500,9600,9700,9800,9900]
    };

    // ── Map init ──────────────────────────────────────────────────────────────
    // NOTE: Regrid trial tokens are restricted to specific counties.
    // Spec examples use Dallas County TX (geoid 48113, ~-96.77, 32.82).
    // Change defaultLat/Lng back to New Orleans (29.9511, -90.0715) once on paid plan.
    var defaultLat = 32.8277, defaultLng = -96.7773; // Dallas TX — trial coverage area

    var initLat = defaultLat, initLng = defaultLng, initZoom = 17;
    if (PROSPECTS && PROSPECTS.length > 0) {
        initLat = PROSPECTS.reduce(function(s,p){ return s+p.lat; },0) / PROSPECTS.length;
        initLng = PROSPECTS.reduce(function(s,p){ return s+p.lng; },0) / PROSPECTS.length;
    }

    var map = L.map('parcelMap').setView([initLat, initLng], initZoom);

    var street = L.tileLayer(
        'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
        { attribution: '© OpenStreetMap', maxZoom: 19 }
    );
    var satellite = L.tileLayer(
        'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
        { attribution: 'Tiles © Esri', maxZoom: 19 }
    );
    satellite.addTo(map);
    L.control.layers({ 'Street': street, 'Satellite': satellite }, null,
        { position: 'topright', collapsed: false }).addTo(map);

    // Regrid parcel tile layer — fetched server-side via proxy (token never on client)
    if (HAS_REGRID) {
        fetch('/Secure/Prospects/RegridProxy.ashx?endpoint=tileurl', { credentials: 'same-origin' })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (!data.tileUrl) { console.warn('[Regrid] No tile URL'); return; }
            L.tileLayer(data.tileUrl, {
                attribution: 'Parcels &copy; <a href="https://regrid.com">Regrid</a>',
                maxZoom: 21, minZoom: 13, opacity: 1.0
            }).addTo(map);
        })
        .catch(function(err) { console.error('[Regrid tileurl]', err); });
    }

    // Existing Reyla prospect markers
    var blueIcon = L.icon({
        iconUrl:   '/Theme/assets/images/leaflet/marker-icon.png',
        shadowUrl: '/Theme/assets/images/leaflet/marker-shadow.png',
        iconSize: [25,41], iconAnchor: [12,41], popupAnchor: [1,-34]
    });
    PROSPECTS.forEach(function(p) {
        L.marker([p.lat, p.lng], { icon: blueIcon })
         .bindTooltip(p.address, { direction: 'top', offset: [0,-38] })
         .addTo(map);
    });

    // ── Map click → parcel lookup ─────────────────────────────────────────────
    map.on('click', function(e) {
        if (!HAS_REGRID) return;
        fetchParcelByPoint(e.latlng.lat, e.latlng.lng, e.latlng);
    });

    function fetchParcelByPoint(lat, lng, latlng) {
        showStatus('Loading parcel...');
        fetch('/Secure/Prospects/RegridProxy.ashx?endpoint=point&lat=' + lat + '&lon=' + lng, {
            credentials: 'same-origin'
        })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            hideStatus();
            var features = data && data.parcels && data.parcels.features;
            if (!features || features.length === 0) {
                showStatus('No parcel data at this location', 3000);
                return;
            }
            var f = features[0];
            if (!passesFilter(f)) { showStatus('Parcel does not match current filter', 2000); return; }

            // Orange highlight polygon
            try {
                if (hoveredLayer) { map.removeLayer(hoveredLayer); hoveredLayer = null; }
                if (f.geometry) {
                    hoveredLayer = L.geoJSON(f, {
                        style: { color:'#f97316', weight:3, opacity:1, fillColor:'#f97316', fillOpacity:0.25 }
                    }).addTo(map);
                }
            } catch(e) { console.warn('[Regrid] geometry error', e); }

            showParcelPopup(f, latlng);
            trackParcel(f);
        })
        .catch(function(err) {
            hideStatus();
            console.error('[Regrid point]', err);
            showStatus('Error loading parcel', 3000);
        });
    }

    // ── Parcel popup ──────────────────────────────────────────────────────────
    function showParcelPopup(feature, latlng) {
        var f  = feature.properties && feature.properties.fields;
        if (!f) { console.error('[popup] no fields', feature.properties); return; }
        var hn = feature.properties.headline || f.address || 'Unknown address';

        var owner    = f.owner    || 'Owner unknown';
        var mailadd  = [f.mailadd, f.mail_city, f.mail_state2, f.mail_zip].filter(Boolean).join(', ');
        var parval   = f.parval   ? '$' + Number(f.parval).toLocaleString() : null;
        var saleprice = f.saleprice && f.saleprice > 0 ? '$' + Number(f.saleprice).toLocaleString() : null;
        var saledate  = f.saledate ? f.saledate.substring(0,4) : null;
        var zoning    = f.zoning   || null;
        var usedesc   = f.usedesc  || null;
        var sqft      = f.ll_gissqft ? Number(f.ll_gissqft).toLocaleString() + ' sqft' : null;
        var acres     = f.ll_gisacre ? Number(f.ll_gisacre).toFixed(2) + ' ac' : null;
        var yearbuilt = f.yearbuilt || null;
        var vacancy   = f.usps_vacancy === 'Y' ? 'Vacant' : null;
        var qoz       = f.qoz === 'Yes' ? 'OZ' : null;
        var absentee  = isAbsenteeOwner(f) ? 'Absentee' : null;

        // "Search in Reyla" → Search.aspx with each address field pre-filled, auto-execute.
        // Pass parts individually so Search.aspx.cs can fill txtStreetAddress/City/State/Zip
        // directly — no comma-splitting guesswork needed.
        var addr  = f.address    || f.saddno && (f.saddno + ' ' + f.saddstr + ' ' + (f.saddsttyp||'')).trim() || hn;
        var city  = f.scity      || f.mail_city   || '';
        var state = f.state2     || f.mail_state2  || '';
        var zip   = (f.szip || f.mail_zip || '').replace(/-.*$/, ''); // strip ZIP+4 (e.g. 75227-1612 → 75227)
        var searchUrl = '/Secure/Prospects/Search.aspx'
            + '?addr='  + encodeURIComponent(addr.trim())
            + '&city='  + encodeURIComponent(city.trim())
            + '&state=' + encodeURIComponent(state.trim())
            + '&zip='   + encodeURIComponent(zip.trim())
            + '&autoSearch=1';

        // Street View thumbnail
        var svHtml = '';
        if (SV_KEY && f.lat && f.lon) {
            var svUrl = 'https://maps.googleapis.com/maps/api/streetview?size=260x130'
                + '&location=' + f.lat + ',' + f.lon
                + '&fov=90&pitch=0&source=outdoor&key=' + SV_KEY;
            svHtml = '<img src="' + svUrl + '" style="width:100%;height:130px;'
                   + 'object-fit:cover;border-radius:5px 5px 0 0;display:block;margin-bottom:8px;"'
                   + ' onerror="this.style.display=\'none\'">';
        }

        var pillsHtml = '';
        if (usedesc) pillsHtml += '<span class="pp-fact">'        + esc(truncate(usedesc,28)) + '</span>';
        if (zoning)  pillsHtml += '<span class="pp-fact zoning">' + esc(zoning) + '</span>';
        if (parval)  pillsHtml += '<span class="pp-fact value">'  + parval + ' assessed</span>';
        if (saleprice && saledate) pillsHtml += '<span class="pp-fact sale">' + saleprice + ' (' + saledate + ')</span>';
        else if (saleprice)        pillsHtml += '<span class="pp-fact sale">' + saleprice + ' last sale</span>';
        if (sqft)      pillsHtml += '<span class="pp-fact">'      + sqft + '</span>';
        if (acres)     pillsHtml += '<span class="pp-fact">'      + acres + '</span>';
        if (yearbuilt) pillsHtml += '<span class="pp-fact">Built ' + yearbuilt + '</span>';
        if (vacancy)   pillsHtml += '<span class="pp-fact vacant">Vacant</span>';
        if (qoz)       pillsHtml += '<span class="pp-fact" style="background:#f3e8ff;color:#6d28d9;">OZ</span>';
        if (absentee)  pillsHtml += '<span class="pp-fact" style="background:#fff7ed;color:#c2410c;">Absentee</span>';

        var mailHtml = '';
        if (mailadd) {
            var mailLabel = absentee
                ? '<span style="color:#c2410c;font-weight:600;">Mailing: </span>'
                : '<span style="color:#6b7280;">Mailing: </span>';
            mailHtml = '<div class="pp-mail">' + mailLabel + esc(mailadd) + '</div>';
        }

        var html = '<div class="parcel-popup">'
            + svHtml
            + '<div class="pp-address">' + esc(hn) + '</div>'
            + '<div class="pp-owner"><i class="ti ti-user" style="font-size:12px;margin-right:4px;"></i>' + esc(owner) + '</div>'
            + (pillsHtml ? '<div class="pp-facts">' + pillsHtml + '</div>' : '')
            + mailHtml
            + '<div class="pp-actions">'
            +   '<a class="pp-btn pp-btn-primary" href="' + searchUrl + '">'
            +     '<i class="ti ti-building-skyscraper" style="font-size:12px;margin-right:4px;"></i>Search in Reyla'
            +   '</a>'
            + '</div>'
            + '</div>';

        L.popup({ maxWidth: 280, minWidth: 260 })
         .setLatLng(latlng)
         .setContent(html)
         .openOn(map);
    }

    // ── Use-type filter ───────────────────────────────────────────────────────
    function setFilter(filter) {
        currentFilter = filter;
        document.querySelectorAll('.filter-pill').forEach(function(p) {
            p.classList.toggle('active', p.dataset.filter === filter);
        });
        map.closePopup();
        if (hoveredLayer) { map.removeLayer(hoveredLayer); hoveredLayer = null; }
    }

    function passesFilter(feature) {
        if (currentFilter === 'all') return true;
        var codes = LBCS_FILTERS[currentFilter];
        if (!codes) return true;
        var act = feature.properties.fields.lbcs_activity;
        if (!act) return false;
        return codes.some(function(base) { return act >= base && act < base + 100; });
    }

    // ── Parcel click tracking ─────────────────────────────────────────────────
    function trackParcel(feature) {
        var f   = feature.properties.fields;
        var key = f.ll_uuid || f.parcelnumb || (f.lat + ',' + f.lon);
        if (clickedParcels.some(function(p) { return p.key === key; })) return;

        clickedParcels.push({
            key:       key,
            address:   feature.properties.headline || f.address || 'Unknown',
            streetAddr: f.address    || '',
            city:       f.scity      || f.mail_city   || '',
            state:      f.state2     || f.mail_state2  || '',
            zip:        (f.szip || f.mail_zip || '').replace(/-.*$/, ''),
            owner:     f.owner    || 'Unknown',
            usedesc:   f.usedesc  || '',
            parval:    f.parval   || 0,
            saleprice: f.saleprice || 0,
            saledate:  f.saledate || '',
            lat:       parseFloat(f.lat),
            lng:       parseFloat(f.lon)
        });
        renderClickedParcels();
    }

    function renderClickedParcels() {
        var list  = document.getElementById('clickedParcelsList');
        var badge = document.getElementById('clickCountBadge');
        badge.textContent = clickedParcels.length;

        if (clickedParcels.length === 0) {
            list.innerHTML = '<p class="text-muted fs-sm p-3 mb-0">Click any parcel on the map to track it here.</p>';
            return;
        }

        var html = '<div class="table-responsive"><table class="table table-sm table-hover mb-0">'
            + '<thead><tr>'
            + '<th style="font-size:11px;">Address</th>'
            + '<th style="font-size:11px;">Owner</th>'
            + '<th style="font-size:11px;">Use</th>'
            + '<th style="font-size:11px;">Assessed</th>'
            + '<th style="font-size:11px;">Last Sale</th>'
            + '<th></th>'
            + '</tr></thead><tbody>';

        clickedParcels.forEach(function(p) {
            var parval   = p.parval > 0 ? '$' + Number(p.parval).toLocaleString() : '--';
            var saleInfo = p.saleprice > 0
                ? '$' + Number(p.saleprice).toLocaleString() + (p.saledate ? ' (' + p.saledate.substring(0,4) + ')' : '')
                : '--';
            var searchUrl = '/Secure/Prospects/Search.aspx'
                + '?addr='  + encodeURIComponent(p.streetAddr || p.address)
                + '&city='  + encodeURIComponent(p.city)
                + '&state=' + encodeURIComponent(p.state)
                + '&zip='   + encodeURIComponent(p.zip)
                + '&autoSearch=1';

            html += '<tr style="cursor:pointer" onclick="panToParcel(' + p.lat + ',' + p.lng + ')">'
                + '<td style="font-size:12px;font-weight:600;">' + esc(p.address) + '</td>'
                + '<td style="font-size:12px;color:#1e40af;">'   + esc(p.owner)   + '</td>'
                + '<td style="font-size:11px;color:#6b7280;">'   + esc(truncate(p.usedesc,22)) + '</td>'
                + '<td style="font-size:12px;">' + parval   + '</td>'
                + '<td style="font-size:12px;">' + saleInfo + '</td>'
                + '<td><a href="' + searchUrl + '" class="btn btn-primary btn-sm py-0 px-2" style="font-size:11px;" onclick="event.stopPropagation()">Search</a></td>'
                + '</tr>';
        });

        html += '</tbody></table></div>';
        list.innerHTML = html;
    }

    function clearClickedParcels() { clickedParcels = []; renderClickedParcels(); }
    function panToParcel(lat, lng)  { map.setView([lat, lng], 17); }

    // ── Helpers ───────────────────────────────────────────────────────────────
    function isAbsenteeOwner(f) {
        if (!f.mail_city || !f.scity) return false;
        return f.mail_city.toLowerCase() !== f.scity.toLowerCase();
    }
    function showStatus(msg, ms) {
        var el = document.getElementById('statusBar');
        el.textContent = msg; el.style.display = '';
        if (ms) setTimeout(function() { el.style.display = 'none'; }, ms);
    }
    function hideStatus() { document.getElementById('statusBar').style.display = 'none'; }
    function esc(s) {
        if (!s) return '';
        return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }
    function truncate(s, n) { return (!s) ? '' : s.length > n ? s.substring(0,n) + '…' : s; }
    </script>

</asp:Content>
