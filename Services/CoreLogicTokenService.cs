using System;
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
    ///   CoreLogic.TokenUrl       — token endpoint
    /// </summary>
    public static class CoreLogicTokenService
    {
        // Static constructor — forces TLS 1.2+ the moment this class is first referenced,
        // before any token fetch can attempt an outbound connection with TLS 1.0 defaults.
        static CoreLogicTokenService()
        {
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls13;
        }

        private const string CacheKey     = "CoreLogic_BearerToken";
        private const int    BufferSeconds = 60;

        private static readonly string TokenUrl =
            System.Configuration.ConfigurationManager.AppSettings["CoreLogic.TokenUrl"]
            ?? "https://prod.corelogicapi.com/oauth/token";

        private static readonly string ClientId =
            System.Configuration.ConfigurationManager.AppSettings["CoreLogic.ClientId"]
            ?? "PLACEHOLDER_CLIENT_ID";

        private static readonly string ClientSecret =
            System.Configuration.ConfigurationManager.AppSettings["CoreLogic.ClientSecret"]
            ?? "PLACEHOLDER_CLIENT_SECRET";

        /// <summary>
        /// Returns a valid bearer token. Fetches a new one if the cache is empty or expired.
        /// </summary>
        public static string GetToken()
        {
            var cached = HttpRuntime.Cache[CacheKey] as string;
            if (!string.IsNullOrEmpty(cached))
                return cached;

            return FetchAndCacheToken();
        }

        private static string FetchAndCacheToken()
        {
            try
            {
                // .NET 4.x defaults to TLS 1.0 which CoreLogic rejects — force 1.2+
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

				using (var client = new WebClient())
				{
					string credentials = Convert.ToBase64String(
						System.Text.Encoding.ASCII.GetBytes(ClientId + ":" + ClientSecret));

					client.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
					client.Headers[HttpRequestHeader.ContentType]   = "application/json";

					// grant_type goes on the query string, body is empty JSON
					string url = TokenUrl + "?grant_type=client_credentials";
					string responseJson = client.UploadString(url, "POST", "{}");

					var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseJson);
					if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
						throw new Exception("CoreLogic token response contained no access_token.");

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
			catch (WebException wex)
			{
				string responseBody = "(no response body)";
				if (wex.Response != null)
				{
					using (var stream = wex.Response.GetResponseStream())
					using (var reader = new System.IO.StreamReader(stream))
					{
						responseBody = reader.ReadToEnd();
					}
				}
				System.Diagnostics.Trace.TraceError(
					"[CoreLogicTokenService] Token fetch failed. Status={0} | URL={1} | ClientId={2} | Body={3}",
					wex.Status, TokenUrl, ClientId, responseBody);
				throw;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Trace.TraceError("[CoreLogicTokenService] Token fetch failed: {0}", ex);
				throw;
			}
        }

        private class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}
