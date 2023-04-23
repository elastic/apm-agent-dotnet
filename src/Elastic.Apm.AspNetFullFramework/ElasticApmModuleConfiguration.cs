// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Configuration;
using System.Text;
using System.Web.Hosting;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework
{
	internal class AppSettingsConfigurationKeyValueProvider : IConfigurationKeyValueProvider
	{
		private const string Origin = "System.Configuration.ConfigurationManager.AppSettings";

		private readonly IApmLogger _logger;

		public AppSettingsConfigurationKeyValueProvider(IApmLogger logger) =>
			_logger = logger?.Scoped(nameof(AppSettingsConfigurationKeyValueProvider));

		public string Description => Origin;

		public ApplicationKeyValue Read(ConfigurationOption option)
		{
			try
			{
				var key = option.ToConfigKey();
				var value = ConfigurationManager.AppSettings[key];
				if (value != null) return new ApplicationKeyValue(option, value, Origin);
			}
			catch (ConfigurationErrorsException ex)
			{
				_logger.Error()?.LogException(ex, "Exception thrown from ConfigurationManager.AppSettings - falling back on environment variables");
			}
			return null;
		}
	}

	internal class ElasticApmModuleConfiguration : FallbackToEnvironmentConfigurationBase
	{
		public ElasticApmModuleConfiguration(IApmLogger logger = null)
			: base(logger,
				new ConfigurationDefaults
				{
						DebugName = nameof(ElasticApmModuleConfiguration),
						ServiceName = DiscoverFullFrameworkServiceName(logger?.Scoped(nameof(ElasticApmModuleConfiguration)))
				},
				new AppSettingsConfigurationKeyValueProvider(logger)
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
				return System.Environment.GetEnvironmentVariable("APP_POOL_ID")?.Trim();
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
