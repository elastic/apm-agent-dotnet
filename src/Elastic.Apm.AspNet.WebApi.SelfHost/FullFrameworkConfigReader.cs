using System.Configuration;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNet.WebApi.SelfHost
{
	internal class FullFrameworkConfigReader : AbstractConfigurationWithEnvFallbackReader
	{
		private const string Origin = "System.Configuration.ConfigurationManager.AppSettings";
		private const string ThisClassName = nameof(FullFrameworkConfigReader);

		private readonly IApmLogger _logger;

		public FullFrameworkConfigReader(IApmLogger logger = null)
			: base(logger, /* defaultEnvironmentName: */ null, ThisClassName) => _logger = logger?.Scoped(ThisClassName);

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
	}
}
