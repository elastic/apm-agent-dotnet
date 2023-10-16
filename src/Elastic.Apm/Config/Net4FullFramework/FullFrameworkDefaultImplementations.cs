// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
#if NETFRAMEWORK
#nullable enable
using System;
using System.Configuration;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config.Net4FullFramework;

internal static class FullFrameworkDefaultImplementations
{
	internal static IApmLogger CreateDefaultLogger(LogLevel? configuredDefault)
	{
		var logLevel = ConfigurationManager.AppSettings[ConfigurationOption.LogLevel.ToConfigKey()];
		if (string.IsNullOrEmpty(logLevel))
			logLevel = Environment.GetEnvironmentVariable(ConfigurationOption.LogLevel.ToEnvironmentVariable());

		var level = configuredDefault ?? ConfigConsts.DefaultValues.LogLevel;
		if (!string.IsNullOrEmpty(logLevel))
			Enum.TryParse(logLevel, true, out level);

		return AgentComponents.CheckForProfilerLogger(new TraceLogger(level), level);
	}

	/// <summary>
	/// Optionally instantiate a custom ConfigurationReader as configured through <see cref="ConfigurationOption.FullFrameworkConfigurationReaderType"/>
	/// </summary>
	/// <param name="logger"></param>
	/// <returns>The custom <see cref="IConfigurationReader"/> implementation if it can be initialized, <code>null</code> otherwise</returns>
	internal static IConfigurationReader? CreateConfigurationReaderFromConfiguredType(IApmLogger logger)
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
#endif
