// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Batch;
using System.Web.Http.Hosting;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using AspNetFullFrameworkSampleApp.Mvc;
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

			// Web API setup
			HttpBatchHandler batchHandler = new DefaultHttpBatchHandler(GlobalConfiguration.DefaultServer)
			{
				ExecutionOrder = BatchExecutionOrder.NonSequential
			};
			var configuration = GlobalConfiguration.Configuration;
			RouteConfig.RegisterWebApiRoutes(configuration, batchHandler);
			GlobalConfiguration.Configure(WebApiConfig.Register);

			// MVC setup
			AreaRegistration.RegisterAllAreas();
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);

			ValueProviderFactories.Factories.Remove(ValueProviderFactories.Factories.OfType<JsonValueProviderFactory>().FirstOrDefault());
			ValueProviderFactories.Factories.Add(new JsonNetValueProviderFactory());

			GlobalConfiguration.Configuration.Services.Replace(typeof(IHostBufferPolicySelector), new CustomWebHostBufferPolicySelector());
		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{
			if (Response.HeadersWritten) return;

			Response.AddOnSendingHeaders(httpContext =>
			{
				// ReSharper disable once ConstantConditionalAccessQualifier
				var responseHeaders = httpContext?.Response?.Headers;
				if (responseHeaders == null) return;

				AddHeaderIfNotPresent(responseHeaders, Consts.ElasticApmServerUrlsResponseHeaderName, Agent.Config.ServerUrl.ToString());
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
