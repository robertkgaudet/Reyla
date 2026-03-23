using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace Reyla
{
	public class Global : HttpApplication
	{
		void Application_Start(object sender, EventArgs e)
		{

			// Force TLS 1.2+ for all outbound HTTPS — required for CoreLogic API on .NET 4.x
			System.Net.ServicePointManager.SecurityProtocol =
				System.Net.SecurityProtocolType.Tls12 |
				System.Net.SecurityProtocolType.Tls13;

			// Code that runs on application startup
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
		}
	}
}