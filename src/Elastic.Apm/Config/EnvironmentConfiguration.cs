// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{

	internal class NullConfigurationKeyValueProvider : IConfigurationKeyValueProvider
	{
		public string Description => null;

		public ApplicationKeyValue Read(ConfigurationOption key) => null;
	}

	internal class EnvironmentConfiguration : FallbackToEnvironmentConfigurationBase, IConfiguration
	{
		private static readonly ConfigurationDefaults ConfigurationDefaults = new () { DebugName = nameof(EnvironmentConfiguration) };
		// We force base.KeyValueProvider to always return null so the base class always falls back to environment variables
		private static readonly NullConfigurationKeyValueProvider NullProvider = new ();

		public EnvironmentConfiguration(IApmLogger logger = null) : base(logger, ConfigurationDefaults, NullProvider) { }
	}
}
