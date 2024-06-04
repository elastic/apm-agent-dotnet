// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using Elastic.Apm.Helpers;
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

		private string ValueForLogging
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Value))
					return Consts.NotProvided;

				string UrlString(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri.Sanitize().ToString() : value;

				return Option switch
				{
					ServerUrl => UrlString(Value),
					ServerUrls => string.Join(",", Value.Split(',').Select(UrlString)),
					_ => NeedsMasking ? Consts.Redacted : Value
				};
			}
		}



		public void Log<T>(T state, Action<T, string, string, string, string> logCallback) =>
			logCallback(state, $"{Type,13}", Option.ToNormalizedName(), ValueForLogging, ReadFrom);

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
