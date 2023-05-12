using System;
using System.Configuration;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework.Helper
{
	public class ConfigHelper
	{
		/// <summary>
		/// Instantiate a custom ConfigurationReader
		/// </summary>
		/// <param name="logger"></param>
		/// <returns>The custom <see cref="IConfigurationReader"/> implementation if it can be initialized, <code>null</code> otherwise</returns>
		public static IConfigurationReader CreateReader(IApmLogger logger)
		{
			var configKey = ConfigurationOption.FullFrameworkConfigurationReaderType.ToConfigKey();
			var expectedTypeName = ConfigurationManager.AppSettings[configKey];
			var isInEnvVarConfigured = false;

			var envVariable = ConfigurationOption.FullFrameworkConfigurationReaderType.ToEnvironmentVariable();
			//fall back to read from env.var.
			if (string.IsNullOrEmpty(expectedTypeName))
			{
				expectedTypeName = Environment.GetEnvironmentVariable(envVariable);
				if (string.IsNullOrEmpty(expectedTypeName))
					return null;

				isInEnvVarConfigured = true;
			}

			try
			{
				var type = Type.GetType(expectedTypeName);
				if (type == null)
				{
					var configName = isInEnvVarConfigured ? envVariable : configKey;
					logger.Warning()?.Log("Failed loading type for configuration reader, {configName}: {configValue}",
						configName, expectedTypeName);
					return null;
				}

				if (Activator.CreateInstance(type, logger) is IConfigurationReader reader) return reader;
			}
			catch (Exception ex)
			{
				logger.Error()?.LogException(ex, "GetConfigReader exception");
			}

			return null;
		}
	}
}
