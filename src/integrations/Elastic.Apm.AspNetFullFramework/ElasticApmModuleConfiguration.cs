// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Text;
using System.Web.Hosting;
using Elastic.Apm.Config;
using Elastic.Apm.Config.Net4FullFramework;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework
{
	internal class ElasticApmModuleConfiguration : AppSettingsConfiguration
	{
		public ElasticApmModuleConfiguration(IApmLogger logger = null)
			: base(logger,
				new ConfigurationDefaults
				{
					DebugName = nameof(ElasticApmModuleConfiguration),
					ServiceName = DiscoverFullFrameworkServiceName(logger?.Scoped(nameof(ElasticApmModuleConfiguration)))
				}
			)
		{ }

		private static string DiscoverFullFrameworkServiceName(IApmLogger logger)
		{
			var retVal = new StringBuilder();
			try
			{
				AppendIfNotNull(FindSiteName(logger));
				AppendIfNotNull(FindAppPoolName(logger));
				return retVal.ToString();
			}
			catch (Exception ex)
			{
				logger?.Error()?.Log("Failed to find default site and app_pool name, falling back to default service name detection: {Exception}", ex);
				return null;
			}

			void AppendIfNotNull(string nameToAppend)
			{
				if (nameToAppend is null)
					return;

				if (retVal.Length != 0)
					retVal.Append('_');

				retVal.Append(nameToAppend);
			}
		}

		private static string FindAppPoolName(IApmLogger logger)
		{
			try
			{
				var appPoolName = System.Environment.GetEnvironmentVariable("APP_POOL_ID")?.Trim();
				if (!string.IsNullOrEmpty(appPoolName))
					return appPoolName;

				// With IIS Application Initialization and preloadEnabled="true", the application is started
				// before APP_POOL_ID is set in the worker process environment. WAS passes the app pool name
				// to w3wp as "-ap <name>", so use it as a fallback. APP_POOL_ID stays the first choice
				// because IIS Express sets it but does not pass -ap.
				appPoolName = AppPoolNameParser.FromCommandLineArgs(System.Environment.GetCommandLineArgs());
				if (appPoolName != null)
					logger?.Debug()?.Log("APP_POOL_ID is not set, using app pool name from the w3wp command line: {AppPoolName}", appPoolName);

				return appPoolName;
			}
			catch (Exception ex)
			{
				logger?.Error()?.Log("Failed to get app pool name: {Exception}", ex);
				return null;
			}
		}

		private static string FindSiteName(IApmLogger logger)
		{
			try
			{
				return HostingEnvironment.SiteName;
			}
			catch (Exception ex)
			{
				logger?.Error()?.Log("Failed to get site name: {Exception}", ex);
				return null;
			}
		}
	}
}
