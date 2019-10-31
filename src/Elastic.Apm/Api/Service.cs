﻿using System.Reflection;
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
			{ nameof(Language), Language }
		}.ToString();

		internal static Service GetDefaultService(IConfigurationReader configurationReader, IApmLogger loggerArg)
		{
			IApmLogger logger = loggerArg.Scoped(nameof(Service));
			return new Service
			{
				Name = configurationReader.ServiceName,
				Version = configurationReader.ServiceVersion,
				Agent = new AgentC
				{
					Name = Consts.AgentName,
					Version = typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
				},
				Runtime = PlatformDetection.GetServiceRuntime(logger),
				Environment = configurationReader.Environment
			};
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
		internal const string DotNetCoreName = ".NET Core";

		internal const string DotNetFullFrameworkName = ".NET Framework";

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Version { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Runtime)) { { nameof(Name), Name }, { nameof(Version), Version } }.ToString();
	}
}
