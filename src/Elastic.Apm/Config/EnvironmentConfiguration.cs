// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Config
{

	internal class NullConfigurationKeyValueProvider : IConfigurationKeyValueProvider
	{
		public ConfigurationKeyValue Read(string key) => null;
	}

	internal class EnvironmentConfiguration : FallbackToEnvironmentConfigurationBase, IConfiguration, IConfigurationSnapshotDescription
	{
		private static readonly ConfigurationDefaults ConfigurationDefaults = new () { DebugName = nameof(EnvironmentConfiguration) };
		// We force base.KeyValueProvider to always return null so the base class always falls back to environment variables
		private static readonly NullConfigurationKeyValueProvider NullProvider = new ();

		public EnvironmentConfiguration(IApmLogger logger = null) : base(logger, ConfigurationDefaults, NullProvider) =>
			Description = EnvironmentKeyValueProvider.Origin;

		public string Description { get; }
	}
}
