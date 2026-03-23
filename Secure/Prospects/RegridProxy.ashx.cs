using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace Reyla.Secure.Prospects
{
    public class RegridProxy : IHttpHandler
    {
        private const string BaseUrl = "https://app.regrid.com/api/v2/parcels";

        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            if (!context.User.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = 401;
                return;
            }

            var token = ConfigurationManager.AppSettings["Regrid.ApiToken"];
            if (string.IsNullOrWhiteSpace(token))
            {
                context.Response.StatusCode = 503;
                context.Response.Write("{\"error\":\"Regrid token not configured\"}");
                return;
            }

            var endpoint = context.Request.QueryString["endpoint"] ?? string.Empty;
            context.Response.ContentType = "application/json";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            try
            {
                switch (endpoint.ToLower())
                {
                    case "tileurl": HandleTileUrl(context, token); break;
                    case "area":    HandleArea(context, token);    break;
                    case "owner":   HandleOwner(context, token);   break;
                    case "point":   HandlePoint(context, token);   break;
                    default:
                        context.Response.StatusCode = 400;
                        context.Response.Write("{\"error\":\"Unknown endpoint\"}");
                        break;
                }
            }
            catch (WebException wex)
            {
                context.Response.StatusCode = 502;
                var msg = wex.Message;
                if (wex.Response != null)
                    using (var sr = new StreamReader(wex.Response.GetResponseStream()))
                        msg = sr.ReadToEnd();
                System.Diagnostics.Trace.TraceError("[RegridProxy] WebException: " + msg);
                context.Response.Write("{\"error\":" + Newtonsoft.Json.JsonConvert.SerializeObject(msg) + "}");
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                System.Diagnostics.Trace.TraceError("[RegridProxy] Exception: " + ex.Message);
                context.Response.Write("{\"error\":" + Newtonsoft.Json.JsonConvert.SerializeObject(ex.Message) + "}");
            }
        }

        private void HandleTileUrl(HttpContext ctx, string token)
        {
            var body = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                query  = new { parcel = true },
                styles = "Map { background-color: rgba(0,0,0,0); } " +
                         "#loveland { line-color: #facc15; line-width: 0.5; line-opacity: 0.5; " +
                         "[zoom >= 14] { line-width: 1; line-opacity: 0.8; } " +
                         "[zoom >= 16] { line-width: 1.5; line-opacity: 1; } " +
                         "[zoom >= 18] { line-width: 2.5; } }"
            });

            var url      = "https://tiles.regrid.com/api/v1/sources?token=" + Uri.EscapeDataString(token);
            var response = PostJson(url, body);

            try
            {
                dynamic tileJson = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
                string tileUrl   = tileJson?.tiles?[0];
                ctx.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(new { tileUrl = tileUrl }));
            }
            catch { ctx.Response.Write(response); }
        }

        private void HandleArea(HttpContext ctx, string token)
        {
            var limit   = ctx.Request.QueryString["limit"] ?? "50";
            var geojson = ctx.Request.QueryString["geojson"];
            if (string.IsNullOrWhiteSpace(geojson))
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Write("{\"error\":\"geojson param required\"}");
                return;
            }

            dynamic geoObj;
            try
            {
                var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(geojson);
                geoObj = parsed?.geometry ?? parsed;
            }
            catch
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Write("{\"error\":\"Invalid geojson\"}");
                return;
            }

            var body = Newtonsoft.Json.JsonConvert.SerializeObject(new { geojson = geoObj });
            var url  = BaseUrl + "/area?token=" + Uri.EscapeDataString(token)
                     + "&limit=" + limit + "&return_geometry=true";
            ctx.Response.Write(PostJson(url, body));
        }

        private void HandleOwner(HttpContext ctx, string token)
        {
            var owner = ctx.Request.QueryString["owner"] ?? string.Empty;
            var limit = ctx.Request.QueryString["limit"] ?? "50";
            var url   = BaseUrl + "/owner"
                      + "?owner="  + Uri.EscapeDataString(owner)
                      + "&limit="  + limit
                      + "&return_geometry=true"
                      + "&token="  + Uri.EscapeDataString(token);
            ctx.Response.Write(GetJson(url));
        }

        private void HandlePoint(HttpContext ctx, string token)
        {
            var lat = ctx.Request.QueryString["lat"] ?? string.Empty;
            var lon = ctx.Request.QueryString["lon"] ?? string.Empty;
            var url = BaseUrl + "/point"
                    + "?lat="   + Uri.EscapeDataString(lat)
                    + "&lon="   + Uri.EscapeDataString(lon)
                    + "&limit=1&return_geometry=true"
                    + "&token=" + Uri.EscapeDataString(token);
            ctx.Response.Write(GetJson(url));
        }

        private static string GetJson(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET"; req.Accept = "application/json"; req.Timeout = 15000;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr   = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        private static string PostJson(string url, string jsonBody)
        {
            var bytes = Encoding.UTF8.GetBytes(jsonBody);
            var req   = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST"; req.ContentType = "application/json";
            req.Accept = "application/json"; req.Timeout = 15000;
            req.ContentLength = bytes.Length;
            using (var s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr   = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return sr.ReadToEnd();
        }
    }
}
