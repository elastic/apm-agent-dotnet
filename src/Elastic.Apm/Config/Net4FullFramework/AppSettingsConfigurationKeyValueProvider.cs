// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#nullable enable
#if NETFRAMEWORK
using System.Configuration;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config.Net4FullFramework;

internal class AppSettingsConfigurationKeyValueProvider : IConfigurationKeyValueProvider
{
	private static readonly ScopeName LoggingScopeName = (ScopeName)nameof(AppSettingsConfigurationKeyValueProvider);

	private readonly IApmLogger? _logger;

	public AppSettingsConfigurationKeyValueProvider(IApmLogger? logger) => _logger = logger;

	public string Description => nameof(AppSettingsConfigurationKeyValueProvider);

	public ApplicationKeyValue? Read(ConfigurationOption option)
	{
		try
		{
			var key = option.ToConfigKey();
			var value = ConfigurationManager.AppSettings[key];
			if (value != null) return new ApplicationKeyValue(option, value, Description);
		}
		catch (ConfigurationErrorsException ex)
		{
			_logger.LogScopedErrorWithException(LoggingScopeName, ex, "Exception thrown from ConfigurationManager.AppSettings - falling back on environment variables");
		}
		return null;
	}
}
#endif
#nullable restore
