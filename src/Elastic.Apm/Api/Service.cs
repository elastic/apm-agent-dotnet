// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Reflection;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class Service
	{
		private Service() { }

		public AgentC Agent { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Environment { get; set; }

		public Framework Framework { get; set; }
		public Language Language { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		public Node Node { get; set; }

		public Runtime Runtime { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Version { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Service))
		{
			{ nameof(Name), Name },
			{ nameof(Version), Version },
			{ nameof(Environment), Environment },
			{ nameof(Runtime), Runtime },
			{ nameof(Framework), Framework },
			{ nameof(Agent), Agent },
			{ nameof(Language), Language },
			{ nameof(Node), Node }
		}.ToString();

		internal static Service GetDefaultService(IConfigurationReader configurationReader, IApmLogger loggerArg)
		{
			IApmLogger logger = loggerArg.Scoped(nameof(Service));
			var service = new Service
			{
				Name = configurationReader.ServiceName,
				Version = configurationReader.ServiceVersion,
				Agent = new AgentC
				{
					Name = Consts.AgentName,
					Version = typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
				},
				Environment = configurationReader.Environment,
				Node = new Node { ConfiguredName = configurationReader.ServiceNodeName }
			};

			//see https://github.com/elastic/apm-agent-dotnet/issues/859
			try
			{
				service.Runtime = PlatformDetection.GetServiceRuntime(logger);
			}
			catch (Exception e)
			{
				logger.Warning()?.LogException(e, "Failed detecting runtime - no runtime name and version will be reported");
			}

			return service;
		}

		public class AgentC
		{
			[JsonConverter(typeof(TrimmedStringJsonConverter))]
			public string Name { get; set; }

			[JsonConverter(typeof(TrimmedStringJsonConverter))]
			public string Version { get; set; }

			public override string ToString() =>
				new ToStringBuilder(nameof(AgentC)) { { nameof(Name), Name }, { nameof(Version), Version } }.ToString();
		}
	}

	public class Framework
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Version { get; set; }

		public override string ToString() =>
			new ToStringBuilder(nameof(Framework)) { { nameof(Name), Name }, { nameof(Version), Version } }.ToString();
	}

	public class Language
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Language)) { { nameof(Name), Name } }.ToString();
	}

	/// <summary>
	/// Name and version of the language runtime running this service
	/// </summary>
	public class Runtime
	{
		internal const string DotNet5Name = ".NET 5";
		internal const string DotNetCoreName = ".NET Core";

		internal const string DotNetFullFrameworkName = ".NET Framework";

		internal const string MonoName = "Mono";

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Version { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Runtime)) { { nameof(Name), Name }, { nameof(Version), Version } }.ToString();
	}
}
