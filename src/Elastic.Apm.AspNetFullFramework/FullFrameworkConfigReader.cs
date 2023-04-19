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
	internal class AppSettingsConfigurationProvider : IConfigurationKeyValueProvider
	{
		private readonly IApmLogger _logger;

		public AppSettingsConfigurationProvider(IApmLogger logger) => _logger = logger?.Scoped(nameof(AppSettingsConfigurationProvider));

		private const string Origin = "System.Configuration.ConfigurationManager.AppSettings";

		public ConfigurationKeyValue Read(string key)
		{
			try
			{
				var value = ConfigurationManager.AppSettings[key];
				if (value != null) return new ConfigurationKeyValue(key, value, Origin);
			}
			catch (ConfigurationErrorsException ex)
			{
				_logger.Error()?.LogException(ex, "Exception thrown from ConfigurationManager.AppSettings - falling back on environment variables");
			}
			return null;
		}
	}

	internal class FullFrameworkConfigReader : FallbackToEnvironmentConfigurationBase
	{
		private const string ThisClassName = nameof(FullFrameworkConfigReader);

		private readonly IApmLogger _logger;

		public FullFrameworkConfigReader(IApmLogger logger = null)
			: base(logger, /* defaultEnvironmentName: */ null, ThisClassName, new AppSettingsConfigurationProvider(logger)) => _logger = logger?.Scoped(ThisClassName);

		protected override string DiscoverServiceName() => DiscoverFullFrameworkServiceName() ?? base.DiscoverServiceName();

		private string DiscoverFullFrameworkServiceName()
		{
			var retVal = new StringBuilder();
			AppendIfNotNull(FindSiteName());
			AppendIfNotNull(FindAppPoolName());
			return retVal.ToString();

			void AppendIfNotNull(string nameToAppend)
			{
				if (nameToAppend is null)
					return;

				if (retVal.Length != 0)
					retVal.Append('_');

				retVal.Append(nameToAppend);
			}
		}

		private string FindAppPoolName()
		{
			try
			{
				return EnvironmentConfiguration.Read("APP_POOL_ID")?.Value;
			}
			catch (Exception ex)
			{
				_logger.Error()?.Log("Failed to get app pool name: {Exception}", ex);
				return null;
			}
		}

		private string FindSiteName()
		{
			try
			{
				return HostingEnvironment.SiteName;
			}
			catch (Exception ex)
			{
				_logger.Error()?.Log("Failed to get site name: {Exception}", ex);
				return null;
			}
		}
	}
}
