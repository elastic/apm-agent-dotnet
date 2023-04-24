// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Config
{
	public class ConfigurationKeyValue
	{
		public ConfigurationKeyValue(ConfigurationOption option, ConfigurationType type, string value, string readFrom) =>
			(Option, Type, Key, Value, ReadFrom) = (option, type, option.ToConfigurationName(type), value, readFrom);

		internal ConfigurationType Type { get; }
		public ConfigurationOption Option { get; }
		public string Key { get; }
		public string ReadFrom { get; }
		public string Value { get; }

		public override string ToString() => $"{Key} : {Value} ({ReadFrom})";
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
}
