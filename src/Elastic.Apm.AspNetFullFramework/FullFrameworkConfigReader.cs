// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Configuration;
using System.Text;
using System.Web.Hosting;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework
{
	internal class FullFrameworkConfigReader : AbstractConfigurationWithEnvFallbackReader
	{
		private const string Origin = "System.Configuration.ConfigurationManager.AppSettings";
		private const string ThisClassName = nameof(FullFrameworkConfigReader);

		private readonly IApmLogger _logger;

		public FullFrameworkConfigReader(IApmLogger logger = null)
			: base(logger, /* defaultEnvironmentName: */ null, ThisClassName) => _logger = logger?.Scoped(ThisClassName);

		protected override string DiscoverServiceName() => DiscoverFullFrameworkServiceName() ?? base.DiscoverServiceName();

		protected override ConfigurationKeyValue Read(string key, string fallBackEnvVarName)
		{
			try
			{
				var value = ConfigurationManager.AppSettings[key];
				if (value != null) return Kv(key, value, Origin);
			}
			catch (ConfigurationErrorsException ex)
			{
				_logger.Error()?.LogException(ex, "Exception thrown from ConfigurationManager.AppSettings - falling back on environment variables");
			}

			return Kv(fallBackEnvVarName, ReadEnvVarValue(fallBackEnvVarName), EnvironmentConfigurationReader.Origin);
		}

		private string DiscoverFullFrameworkServiceName()
		{
			var retVal = new StringBuilder();
			AppendIfNotNull(FindSiteName());
			AppendIfNotNull(FindAppPoolName());
			return retVal.ToString();

			void AppendIfNotNull(string nameToAppend)
			{
				const string separator = "_";
				if (nameToAppend != null) retVal.AppendSeparatedIfNotEmpty(separator, nameToAppend);
			}
		}

		private string FindAppPoolName()
		{
			try
			{
				return ReadEnvVarValue("APP_POOL_ID");
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
