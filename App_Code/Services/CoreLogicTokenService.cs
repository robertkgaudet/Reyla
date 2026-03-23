using System;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using Newtonsoft.Json;

namespace Reyla.Services
{
    /// <summary>
    /// Acquires and caches a CoreLogic SDP API OAuth2 bearer token using
    /// the client_credentials grant. Token is cached in HttpRuntime.Cache
    /// and refreshed automatically 60 seconds before expiry.
    ///
    /// Configuration (web.config appSettings):
    ///   CoreLogic.ClientId       — your OAuth2 client_id
    ///   CoreLogic.ClientSecret   — your OAuth2 client_secret
    ///   CoreLogic.TokenUrl       — token endpoint (defaults below if missing)
    /// </summary>
    public static class CoreLogicTokenService
    {
        // ---------------------------------------------------------------
        // Constants
        // ---------------------------------------------------------------
        private const string CacheKey      = "CoreLogic_BearerToken";
        private const int    BufferSeconds  = 60;   // refresh this many seconds before expiry

        private static readonly string TokenUrl =
            System.Configuration.ConfigurationManager.AppSettings["CoreLogic.TokenUrl"]
            ?? "https://prod.corelogicapi.com/oauth/token";

        private static readonly string ClientId =
            System.Configuration.ConfigurationManager.AppSettings["CoreLogic.ClientId"]
            ?? "PLACEHOLDER_CLIENT_ID";

        private static readonly string ClientSecret =
            System.Configuration.ConfigurationManager.AppSettings["CoreLogic.ClientSecret"]
            ?? "PLACEHOLDER_CLIENT_SECRET";

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// Returns a valid bearer token, fetching a new one if necessary.
        /// </summary>
        public static string GetToken()
        {
            // Return cached token if still valid
            var cached = HttpRuntime.Cache[CacheKey] as string;
            if (!string.IsNullOrEmpty(cached))
                return cached;

            return FetchAndCacheToken();
        }

        // ---------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------

        private static string FetchAndCacheToken()
        {
            try
            {
                using (var client = new WebClient())
                {
                    // CoreLogic uses client_credentials with Basic auth header
                    string credentials = Convert.ToBase64String(
                        System.Text.Encoding.ASCII.GetBytes($"{ClientId}:{ClientSecret}"));

                    client.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";
                    client.Headers[HttpRequestHeader.ContentType]   = "application/x-www-form-urlencoded";

                    var postData = new NameValueCollection
                    {
                        ["grant_type"] = "client_credentials"
                    };

                    byte[] responseBytes = client.UploadValues(TokenUrl, "POST", postData);
                    string responseJson  = System.Text.Encoding.UTF8.GetString(responseBytes);

                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseJson);

                    if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                        throw new InvalidOperationException("CoreLogic token response was empty or malformed.");

                    // Cache for (expires_in - buffer) seconds
                    int cacheDuration = Math.Max(tokenResponse.ExpiresIn - BufferSeconds, 30);
                    HttpRuntime.Cache.Insert(
                        CacheKey,
                        tokenResponse.AccessToken,
                        null,
                        DateTime.UtcNow.AddSeconds(cacheDuration),
                        System.Web.Caching.Cache.NoSlidingExpiration);

                    return tokenResponse.AccessToken;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[CoreLogicTokenService] Token fetch failed: {0}", ex);
                throw;
            }
        }

        // ---------------------------------------------------------------
        // Response model (internal use only)
        // ---------------------------------------------------------------

        private class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }   // seconds
        }
    }
}
