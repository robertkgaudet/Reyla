using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Reyla.Services.Models;

namespace Reyla.Services
{
    /// <summary>
    /// Wraps the CoreLogic SDP Property API v2.0.
    /// All endpoint URLs verified against the official OpenAPI 3 swagger spec.
    ///
    /// CORRECTED URLS vs prior version (all were wrong camelCase/wrong paths):
    ///   building              → /v2/properties/{clip}/buildings
    ///   buildingPermits       → /v2/properties/{clip}/building-permits
    ///   taxAssessment         → /v2/properties/{clip}/tax-assessments/latest
    ///   ownershipTransfers    → /v2/properties/{clip}/ownership-transfers/all/latest
    ///   avm                   → /v2/properties/{clip}/avm/thv/RVM/summary
    ///   hoa                   → /v2/properties/{clip}/home-owners-association
    ///   climateRiskAnalytics  → /v2/properties/{clip}/climate-risk-analytics/ar6/comprehensive
    ///   enrichedVoluntaryLiens→ /v2/properties/liens/enriched/{clip}
    ///   involuntaryLiens      → /v2/properties/liens/involuntary-liens/{clip}
    ///   propensity            → /v2/properties/propensity-scores/{clip}/sale-score
    /// </summary>
    public class CoreLogicPropertyService
    {
        // Static constructor — runs once when the class is first loaded by the CLR.
        // This guarantees TLS 1.2+ is set before ANY outbound call, including token fetches
        // that happen inside instance constructors or early method calls on IIS startup.
        static CoreLogicPropertyService()
        {
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls13;
        }

        private readonly string _baseUrl;

        public CoreLogicPropertyService()
        {
            _baseUrl = System.Configuration.ConfigurationManager.AppSettings["CoreLogic.BaseUrl"]
                       ?? "https://property-uat.corelogicapi.com";
        }

        // ---------------------------------------------------------------
        // 1. Property Search — resolve address to Clip + coordinates
        //
        // Two sequential calls (geocode endpoint not entitled on UAT key):
        //   a) /v2/properties/search              — address → Clip + address
        //   b) /v2/properties/{clip}/site-location — parcel centroid lat/lng
        // ---------------------------------------------------------------

        public GeocodeProduct SearchByAddressWithGeocode(
            string streetAddress,
            string city,
            string state,
            string zipCode = null)
        {
            var qs = new StringBuilder();
            qs.Append($"streetAddress={Uri.EscapeDataString(streetAddress)}");
            if (!string.IsNullOrEmpty(city))    qs.Append($"&city={Uri.EscapeDataString(city)}");
            if (!string.IsNullOrEmpty(state))   qs.Append($"&state={Uri.EscapeDataString(state)}");
            if (!string.IsNullOrEmpty(zipCode)) qs.Append($"&zipCode={Uri.EscapeDataString(zipCode)}");
            qs.Append("&bestMatch=true");

            string searchJson = Get($"/v2/properties/search?{qs}");
            var searchResult  = JsonConvert.DeserializeObject<PropertySearchProductV2>(searchJson);

            if (searchResult?.Items == null || searchResult.Items.Count == 0)
                return new GeocodeProduct { Items = new List<GeocodeDetail>() };

            var geocodeItems = new List<GeocodeDetail>();

            foreach (var item in searchResult.Items)
            {
                if (string.IsNullOrEmpty(item.Clip)) continue;

                Geocode coords = null;

                try
                {
                    string siteJson  = Get($"/v2/properties/{item.Clip}/site-location");
                    var siteResponse = JsonConvert.DeserializeObject<SingleApiResponseSiteLocationData>(siteJson);
                    var parcel       = siteResponse?.Data?.CoordinatesParcel;

                    if (parcel != null)
                        coords = new Geocode { Latitude = parcel.Lat, Longitude = parcel.Lng };
                }
                catch (CoreLogicApiException) { /* Non-fatal */ }

                geocodeItems.Add(new GeocodeDetail
                {
                    Clip            = item.Clip,
                    V1PropertyId    = item.V1PropertyId,
                    PropertyAddress = item.PropertyAddress == null ? null : new PropertySearchAddress
                    {
                        StreetAddress = item.PropertyAddress.StreetAddress,
                        City          = item.PropertyAddress.City,
                        State         = item.PropertyAddress.State,
                        ZipCode       = item.PropertyAddress.ZipCode,
                        County        = item.PropertyAddress.County
                    },
                    PropertyApn = item.PropertyApn,   // carries fipsCode + APN
                    Geocode = coords
                });
            }

            return new GeocodeProduct { Items = geocodeItems };
        }

        // ---------------------------------------------------------------
        // 2. Ownership  —  GET /v2/properties/{clip}/ownership
        // ---------------------------------------------------------------

        public OwnershipResult GetOwnership(string clip)
        {
            string json = Get($"/v2/properties/{clip}/ownership");
            return JsonConvert.DeserializeObject<OwnershipResult>(json);
        }

        // ---------------------------------------------------------------
        // 3. Ownership Transfers  —  GET /v2/properties/{clip}/ownership-transfers/{saleType}/{latest}
        //    saleType: "all"  |  latest path segment: "latest"
        // ---------------------------------------------------------------

        public OwnershipTransfersResult GetOwnershipTransfers(string clip)
        {
            string json = Get($"/v2/properties/{clip}/ownership-transfers/all/latest");
            return JsonConvert.DeserializeObject<OwnershipTransfersResult>(json);
        }

        // ---------------------------------------------------------------
        // 4. Mortgage  —  GET /v2/properties/{clip}/mortgage
        // ---------------------------------------------------------------

        public MortgageResult GetMortgage(string clip)
        {
            string json = Get($"/v2/properties/{clip}/mortgage");
            return JsonConvert.DeserializeObject<MortgageResult>(json);
        }

        // ---------------------------------------------------------------
        // 5. Enriched Voluntary Liens  —  GET /v2/properties/liens/enriched/{clip}
        //    NOTE: clip is at the END of the path (different pattern)
        // ---------------------------------------------------------------

        public EnrichedVoluntaryLiensResult GetEnrichedVoluntaryLiens(string clip)
        {
            string json = Get($"/v2/properties/liens/enriched/{clip}");
            return JsonConvert.DeserializeObject<EnrichedVoluntaryLiensResult>(json);
        }

        // ---------------------------------------------------------------
        // 6. Involuntary Liens  —  GET /v2/properties/liens/involuntary-liens/{clipId}
        //    NOTE: clip is at the END of the path (different pattern)
        // ---------------------------------------------------------------

        public InvoluntaryLiensResult GetInvoluntaryLiens(string clip)
        {
            string json = Get($"/v2/properties/liens/involuntary-liens/{clip}");
            return JsonConvert.DeserializeObject<InvoluntaryLiensResult>(json);
        }

        // ---------------------------------------------------------------
        // 7. Tax Assessment  —  GET /v2/properties/{clip}/tax-assessments/latest
        // ---------------------------------------------------------------

        public TaxAssessmentResult GetTaxAssessment(string clip)
        {
            string json = Get($"/v2/properties/{clip}/tax-assessments/latest");
            return JsonConvert.DeserializeObject<TaxAssessmentResult>(json);
        }

        // ---------------------------------------------------------------
        // 8. Buildings  —  GET /v2/properties/{clip}/buildings
        //    Returns: { data: { allBuildingsSummary: {...}, Buildings: [...] } }
        // ---------------------------------------------------------------

        public BuildingResult GetBuilding(string clip)
        {
            string json = Get($"/v2/properties/{clip}/buildings");
            return JsonConvert.DeserializeObject<BuildingResult>(json);
        }

        // ---------------------------------------------------------------
        // 9. Building Permits  —  GET /v2/properties/{clip}/building-permits
        // ---------------------------------------------------------------

        public BuildingPermitsResult GetBuildingPermits(string clip)
        {
            string json = Get($"/v2/properties/{clip}/building-permits");
            return JsonConvert.DeserializeObject<BuildingPermitsResult>(json);
        }

        // ---------------------------------------------------------------
        // 10. AVM  —  GET /v2/properties/{clip}/avm/thv/{model}/{summary}
        //     model: "RVM" | summary: "summary"
        // ---------------------------------------------------------------

        public AvmResult GetAvm(string clip)
        {
            string json = Get($"/v2/properties/{clip}/avm/thv/RVM/summary");
            return JsonConvert.DeserializeObject<AvmResult>(json);
        }

        // ---------------------------------------------------------------
        // 11. Propensity (Sale Score)  —  GET /v2/properties/propensity-scores/{clip}/sale-score
        //     NOTE: clip is in the MIDDLE of the path
        // ---------------------------------------------------------------

        public PropensityResult GetPropensity(string clip)
        {
            string json = Get($"/v2/properties/propensity-scores/{clip}/sale-score");
            return JsonConvert.DeserializeObject<PropensityResult>(json);
        }

        // ---------------------------------------------------------------
        // 12. HOA  —  GET /v2/properties/{clip}/home-owners-association
        // ---------------------------------------------------------------

        public HoaResult GetHoa(string clip)
        {
            string json = Get($"/v2/properties/{clip}/home-owners-association");
            return JsonConvert.DeserializeObject<HoaResult>(json);
        }

        // ---------------------------------------------------------------
        // 13. Climate Risk  —  GET /v2/properties/{clip}/climate-risk-analytics/ar6/comprehensive
        // ---------------------------------------------------------------

        public ClimateRiskResult GetClimateRisk(string clip)
        {
            string json = Get($"/v2/properties/{clip}/climate-risk-analytics/ar6/comprehensive");
            return JsonConvert.DeserializeObject<ClimateRiskResult>(json);
        }

        // ---------------------------------------------------------------
        // 14. Property Comparables  —  GET /v2/properties/{clipId}/comparables
        // ---------------------------------------------------------------

        public PropertyComparablesResult GetComparables(
            string  clip,
            decimal searchDistance = 0.5m,
            int     maxComps       = 100,
            int     monthsBack     = 36)
        {
            var qs = new StringBuilder();
            qs.Append($"searchDistance={searchDistance}");
            qs.Append($"&maxComps={maxComps}");
            qs.Append($"&monthsBack={monthsBack}");
            qs.Append("&sortBy=Distance");

            string json = Get($"/v2/properties/{clip}/comparables?{qs}");
            return JsonConvert.DeserializeObject<PropertyComparablesResult>(json);
        }

        // ---------------------------------------------------------------
        // 14b. Site Location  —  GET /v2/properties/{clip}/site-location
        // ---------------------------------------------------------------

        public SiteLocationResponse GetSiteLocation(string clip)
        {
            var json = Get($"/v2/properties/{clip}/site-location");
            return JsonConvert.DeserializeObject<SiteLocationResponse>(json)
                   ?? new SiteLocationResponse();
        }

        // ---------------------------------------------------------------
        // 14c. Property Detail (composite)  —  GET /v2/properties/{clip}/property-detail
        //      Returns buildings, ownership, siteLocation, taxAssessment,
        //      mostRecentOwnerTransfer, and lastMarketSale in a single call.
        //      Use as primary/fallback source when individual endpoints return
        //      no data (common for less-covered counties).
        // ---------------------------------------------------------------

        public PropertyDetailResponse GetPropertyDetail(string clip)
        {
            var json = Get($"/v2/properties/{clip}/property-detail");
            return JsonConvert.DeserializeObject<PropertyDetailResponse>(json)
                   ?? new PropertyDetailResponse();
        }

        // ---------------------------------------------------------------
        // 15. Transaction History  —  GET /v2/properties/{clip}/transaction-history
        // ---------------------------------------------------------------

        public TransactionHistoryResponse GetTransactionHistory(string clip)
        {
            var json = Get($"/v2/properties/{clip}/transaction-history");
            return JsonConvert.DeserializeObject<TransactionHistoryResponse>(json)
                   ?? new TransactionHistoryResponse();
        }

        // ---------------------------------------------------------------
        // 16. Document Images  —  GET /v2/properties/document-images/{product}
        // ---------------------------------------------------------------

        public DocumentImageResponse GetDocumentImage(
            string fipsCode,
            string recordingDate,
            string documentNumber,
            string product = "Cascade")
        {
            // Guard: EscapeDataString throws ArgumentNullException on null inputs
            if (string.IsNullOrWhiteSpace(fipsCode))
                throw new ArgumentException("fipsCode is required for document image retrieval.", nameof(fipsCode));
            if (string.IsNullOrWhiteSpace(recordingDate))
                throw new ArgumentException("recordingDate is required for document image retrieval.", nameof(recordingDate));
            if (string.IsNullOrWhiteSpace(documentNumber))
                throw new ArgumentException("documentNumber is required for document image retrieval.", nameof(documentNumber));

            var url = $"/v2/properties/document-images/{product}" +
                      $"?fipsCode={Uri.EscapeDataString(fipsCode)}" +
                      $"&recordingDate={Uri.EscapeDataString(recordingDate)}" +
                      $"&documentNumber={Uri.EscapeDataString(documentNumber)}" +
                      $"&outputType=PDF";

            var json = Get(url);
            return JsonConvert.DeserializeObject<DocumentImageResponse>(json)
                   ?? new DocumentImageResponse();
        }

        // ---------------------------------------------------------------
        // Private HTTP helper — injects bearer token on every request
        // ---------------------------------------------------------------

        internal string Get(string relativeUrl)
        {
            // Force TLS 1.2+ BEFORE any outbound call — must be first, including token fetch
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            string url   = _baseUrl.TrimEnd('/') + relativeUrl;
            string token = CoreLogicTokenService.GetToken();

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.Authorization] = $"Bearer {token}";
                client.Headers[HttpRequestHeader.Accept]        = "application/json";

                try
                {
                    return client.DownloadString(url);
                }
                catch (WebException ex) when (ex.Response != null)
                {
                    var response = (HttpWebResponse)ex.Response;
                    string body  = string.Empty;

                    using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream()))
                        body = reader.ReadToEnd();

                    throw new CoreLogicApiException(
                        (int)response.StatusCode,
                        relativeUrl,
                        body,
                        ex);
                }
            }
        }
    }

    // ---------------------------------------------------------------
    // Exception type for CoreLogic API errors
    // ---------------------------------------------------------------

    public class CoreLogicApiException : Exception
    {
        public int    HttpStatusCode { get; }
        public string Endpoint      { get; }
        public string ResponseBody  { get; }

        public CoreLogicApiException(
            int statusCode,
            string endpoint,
            string responseBody,
            Exception inner)
            : base($"CoreLogic API error {statusCode} on {endpoint}: {responseBody}", inner)
        {
            HttpStatusCode = statusCode;
            Endpoint       = endpoint;
            ResponseBody   = responseBody;
        }
    }
}
