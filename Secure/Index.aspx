<%@ Page Title="" Language="C#" MasterPageFile="~/Reyla.Master" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="Reyla.Secure.Index" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <!-- Leaflet CSS -->
    <link href="/Theme/assets/plugins/leaflet/leaflet.css" rel="stylesheet" type="text/css">

    <style>
        #shapeMap {
            height: calc(100vh - 200px);
            min-height: 400px;
        }
        @media (max-width: 768px) {
            #shapeMap { height: calc(100vh - 150px); }
        }
        /* Map legend */
        .map-legend { position: absolute; bottom: 30px; left: 10px; z-index: 1000;
                      background: white; border-radius: 8px; padding: 10px 14px;
                      box-shadow: 0 2px 8px rgba(0,0,0,.15); font-size: 12px; }
        .map-legend .leg-row { display: flex; align-items: center; gap: 6px; margin-bottom: 4px; }
        .map-legend .leg-row:last-child { margin-bottom: 0; }
        .leg-dot { width: 12px; height: 12px; border-radius: 50%; flex-shrink: 0; }
        /* Contacted marker tint — matches Comps page .marker-red approach */
        .marker-green { filter: hue-rotate(100deg) saturate(2); }
    </style>

    <div class="container-fluid">

        <div class="page-title-head d-flex align-items-center" id="page-head-box">
            <div class="flex-grow-1">
                <h4 class="fs-lg fw-bold mb-2 lh-1">My Prospects</h4>
                <p class="text-muted mb-0 fs-xs lh-1">All active prospect properties for your account.</p>
            </div>
            <div class="text-end d-flex align-items-center gap-2">
                <span id="prospectCountBadge" class="badge text-bg-primary fs-xs" style="display:none"></span>
                <ol class="breadcrumb m-0 py-0 fs-xs">
                    <li class="breadcrumb-item"><a href="javascript:void(0);">Reyla</a></li>
                    <li class="breadcrumb-item active">My Prospects</li>
                </ol>
            </div>
        </div>

        <div class="row">
            <div class="col-lg-12">
                <div class="card" style="position:relative;">
                    <div class="row g-0">
                        <div class="col-lg-12">
                            <div id="shapeMap"></div>
                        </div>
                    </div>
                    <!-- Legend -->
                    <div class="map-legend">
                        <div class="leg-row"><div class="leg-dot" style="background:#2563eb"></div> Prospect</div>
                        <div class="leg-row"><div class="leg-dot" style="background:#16a34a"></div> Contacted</div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Empty state — shown when no prospects -->
        <div id="noProspectsMsg" class="row mt-3" style="display:none">
            <div class="col-12">
                <div class="card">
                    <div class="card-body text-center py-5">
                        <i class="ti ti-building-estate text-muted" style="font-size:3rem"></i>
                        <h5 class="mt-3 mb-1">No Prospects Yet</h5>
                        <p class="text-muted mb-3">Search for properties and add them as prospects to see them here.</p>
                        <a href="/Secure/Prospects/Prospects.aspx" class="btn btn-primary">
                            <i class="ti ti-search me-1"></i>Find Properties
                        </a>
                    </div>
                </div>
            </div>
        </div>

    </div>

    <!-- Leaflet JS -->
    <script src="/Theme/assets/plugins/leaflet/leaflet.js"></script>

    <script>
    // Prospect data from server
    var PROSPECTS = <%=ProspectsJson%>;

    document.addEventListener("DOMContentLoaded", function () {
        var mapEl = document.getElementById("shapeMap");
        if (!mapEl) return;

        // Show/hide empty state
        var badge = document.getElementById('prospectCountBadge');
        var emptyMsg = document.getElementById('noProspectsMsg');
        if (PROSPECTS.length === 0) {
            emptyMsg.style.display = '';
            badge.style.display    = 'none';
        } else {
            badge.textContent      = PROSPECTS.length + ' prospect' + (PROSPECTS.length !== 1 ? 's' : '');
            badge.style.display    = '';
            emptyMsg.style.display = 'none';
        }

        // ── Build map ────────────────────────────────────────────────────
        var defaultLat = 29.9511, defaultLng = -90.0715; // New Orleans fallback

        // Centre on mean of prospects if any, else geolocation
        var map;
        if (PROSPECTS.length > 0) {
            var avgLat = PROSPECTS.reduce(function(s,p){ return s + p.lat; }, 0) / PROSPECTS.length;
            var avgLng = PROSPECTS.reduce(function(s,p){ return s + p.lng; }, 0) / PROSPECTS.length;
            map = L.map(mapEl).setView([avgLat, avgLng], 13);
        } else {
            map = L.map(mapEl).setView([defaultLat, defaultLng], 12);
        }

        // Layer switcher — street (default) + Esri satellite (no API key required)
        var street = L.tileLayer(
            'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
            {
                attribution: 'Map data &copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a>',
                maxZoom: 19
            }
        );

        var satellite = L.tileLayer(
            'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
            {
                attribution: 'Tiles &copy; Esri &mdash; Esri, Maxar, Earthstar Geographics',
                maxZoom: 19
            }
        );

        satellite.addTo(map);

        L.control.layers(
            { 'Street': street, 'Satellite': satellite },
            null,
            { position: 'topright', collapsed: false }
        ).addTo(map);

        // ── Icons ────────────────────────────────────────────────────────
        var blueIcon = L.icon({
            iconUrl:    '/Theme/assets/images/leaflet/marker-icon.png',
            shadowUrl:  '/Theme/assets/images/leaflet/marker-shadow.png',
            iconSize:   [25, 41],
            iconAnchor: [12, 41],
            popupAnchor:[1, -34]
        });

        var greenIcon = L.icon({
            iconUrl:    '/Theme/assets/images/leaflet/marker-icon.png',
            shadowUrl:  '/Theme/assets/images/leaflet/marker-shadow.png',
            iconSize:   [25, 41],
            iconAnchor: [12, 41],
            popupAnchor:[1, -34],
            className:  'marker-green'
        });

        // Fallback if green marker file not present — use default blue
        function getIcon(p) {
            return p.contacted ? greenIcon : blueIcon;
        }

        // ── Markers ──────────────────────────────────────────────────────
        var bounds = [];

        PROSPECTS.forEach(function (p) {
            var marker = L.marker([p.lat, p.lng], { icon: p.contacted ? greenIcon : blueIcon }).addTo(map);

            // Street view thumbnail — same size as Comps page (220x120)
            var svThumb = p.streetViewUrl
                ? '<img src="' + escHtml(p.streetViewUrl) + '" ' +
                  'style="width:220px;height:120px;object-fit:cover;display:block;border-radius:4px;margin-bottom:6px;" ' +
                  'onerror="this.style.display=\'none\'" />'
                : '';

            // Build popup exactly matching Comps page popup structure
            var popup =
                svThumb +
                '<div style="font-size:13px;">' +
                    '<strong>' + escHtml(p.address) + '</strong>' +
                    (p.cityState   ? '<br/><span style="font-size:11px;color:#374151;">' + escHtml(p.cityState) + '</span>' : '') +
                    (p.avm         ? '<br/><span style="font-size:11px;color:#374151;">Est. Value: $' + p.avm.toLocaleString() + '</span>' : '') +
                    (p.sqft        ? '<br/><span style="font-size:11px;color:#374151;">' + p.sqft.toLocaleString() + ' sqft</span>' : '') +
                    (p.yearBuilt   ? '<br/><span style="font-size:11px;color:#374151;">Built ' + p.yearBuilt + '</span>' : '') +
                    (p.compCount   ? '<br/><span style="font-size:11px;color:#374151;">' + p.compCount + ' saved comp' + (p.compCount !== 1 ? 's' : '') + '</span>' : '') +
                    (p.contacted   ? '<br/><span style="font-size:11px;color:#16a34a;">&#10003; Contacted</span>' : '') +
                    '<br/><span style="font-size:11px;color:#6b7280;">Added ' + escHtml(p.addedDate) + '</span>' +
                    '<br/>' +
                    '<a href="' + escHtml(p.detailUrl) + '" style="font-size:12px;color:#1e40af;margin-right:10px;">Property Detail &rarr;</a>' +
                    '<a href="' + escHtml(p.compsUrl)  + '" style="font-size:12px;color:#1e40af;">View Comps &rarr;</a>' +
                '</div>';

            marker.bindPopup(popup, { maxWidth: 260 });

            bounds.push([p.lat, p.lng]);
        });

        // Fit map to all markers
        if (bounds.length > 1) {
            map.fitBounds(bounds, { padding: [40, 40] });
        } else if (bounds.length === 1) {
            map.setView(bounds[0], 15);
        }

        function escHtml(s) {
            if (!s) return '';
            return String(s)
                .replace(/&/g,'&amp;')
                .replace(/</g,'&lt;')
                .replace(/>/g,'&gt;')
                .replace(/"/g,'&quot;');
        }
    });
    </script>

</asp:Content>
