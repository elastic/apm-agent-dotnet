// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using static Elastic.Apm.Config.ConfigurationOption;

namespace Elastic.Apm.Config
{
	public class ConfigurationKeyValue
	{
		public ConfigurationKeyValue(ConfigurationOption option, ConfigurationType type, string value, string readFrom)
		{
			(Option, Type, Key, Value, ReadFrom) = (option, type, option.ToConfigurationName(type), value, readFrom);
			NeedsMasking = Option switch
			{
				ApiKey => true,
				SecretToken => true,
				_ => false
			};
		}

		public ConfigurationType Type { get; }
		public ConfigurationOption Option { get; }
		public string Key { get; }
		public string ReadFrom { get; }
		public string Value { get; }
		public bool NeedsMasking { get; }

		private string ValueForLogging =>
			string.IsNullOrWhiteSpace(Value) ? Consts.NotProvided : (NeedsMasking ? Consts.Redacted : Value);

		public override string ToString() => $"{Type,13}->{Option.ToNormalizedName()}: '{ValueForLogging}' ({ReadFrom})";
	}

	public class ApplicationKeyValue : ConfigurationKeyValue
	{
		public ApplicationKeyValue(ConfigurationOption option, string value, string readFrom)
			: base(option, ConfigurationType.Application, value, readFrom) { }
	}

	public class EnvironmentKeyValue : ConfigurationKeyValue
	{
		public EnvironmentKeyValue(ConfigurationOption option, string value, string readFrom)
			: base(option, ConfigurationType.Environment, value, readFrom) { }
	}

	public class DefaultKeyValue : ConfigurationKeyValue
	{
		public DefaultKeyValue(ConfigurationOption option, string value, string readFrom)
			: base(option, ConfigurationType.Default, value, readFrom) { }
	}
}
