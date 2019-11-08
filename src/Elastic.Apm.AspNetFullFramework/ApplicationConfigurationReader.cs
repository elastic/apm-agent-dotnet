using System;
using System.Configuration;
using System.Security;
using System.Text;
using System.Web.Hosting;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework
{
	internal class ApplicationConfigurationReader : AbstractConfigurationWithEnvFallbackReader
	{
		private const string Origin = "System.Configuration.ConfigurationManager.AppSettings";
		private readonly IApmLogger _logger;

		public FullFrameworApplicationConfigurationReaderkConfigReader(IApmLogger logger = null)
			: base(logger, null, nameof(ApplicationConfigurationReader)) => _logger = logger?.Scoped(nameof(ApplicationConfigurationReader));

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
				if (nameToAppend != null) retVal.AppendSeparatedIfNotEmpty("_", nameToAppend);
			}
		}

		private string FindAppPoolName()
		{
			try
			{
				return ReadEnvVarValue("APP_POOL_ID");
			}
			catch (SecurityException ex)
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
			catch (SecurityException ex)
			{
				_logger.Error()?.Log("Failed to get site name: {Exception}", ex);
				return null;
			}
		}

		protected override ConfigurationKeyValue Read(string key)
		{
			var value = ConfigurationManager.AppSettings[key];
			return value != null ? new ConfigurationKeyValue(key, value, Origin) : base.Read(key);
		}
	}
}
