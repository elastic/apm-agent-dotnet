using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Elastic.Apm;
using NLog;

namespace AspNetFullFrameworkSampleApp
{
	public class MvcApplication : HttpApplication
	{
		protected void Application_Start()
		{
			LoggingConfig.SetupLogging();

			var logger = LogManager.GetCurrentClassLogger();
			logger.Info("Current process ID: {ProcessID}, ELASTIC_APM_SERVER_URLS: {ELASTIC_APM_SERVER_URLS}",
				Process.GetCurrentProcess().Id, Environment.GetEnvironmentVariable("ELASTIC_APM_SERVER_URLS"));

			AreaRegistration.RegisterAllAreas();
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{
			if (Response.HeadersWritten) return;

			Response.AddOnSendingHeaders(httpContext =>
			{
				// ReSharper disable once ConstantConditionalAccessQualifier
				var responseHeaders = httpContext?.Response?.Headers;
				if (responseHeaders == null) return;

				AddHeaderIfNotPresent(responseHeaders, Consts.ElasticApmServerUrlsResponseHeaderName, string.Join(", ", Agent.Config.ServerUrls));
				AddHeaderIfNotPresent(responseHeaders, Consts.ProcessIdResponseHeaderName, Process.GetCurrentProcess().Id.ToString());
			});

			void AddHeaderIfNotPresent(NameValueCollection responseHeaders, string headerName, string headerValue)
			{
				var alreadyExistingValues = responseHeaders.GetValues(headerName);
				if (alreadyExistingValues != null && alreadyExistingValues.Length > 0) return;
				responseHeaders.Add(headerName, headerValue);
			}
		}
	}
}
