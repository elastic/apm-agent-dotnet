// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
#if NETFRAMEWORK
#nullable enable

using Elastic.Apm.Logging;

namespace Elastic.Apm.Config.Net4FullFramework;

internal class AppSettingsConfiguration : FallbackToEnvironmentConfigurationBase
{
	public AppSettingsConfiguration(IApmLogger? logger = null)
		: this(logger,
			new ConfigurationDefaults { DebugName = nameof(AppSettingsConfiguration) }
		) { }

	internal AppSettingsConfiguration(IApmLogger? logger, ConfigurationDefaults defaults)
		: base(logger, defaults, new AppSettingsConfigurationKeyValueProvider(logger)) { }
}
#endif
