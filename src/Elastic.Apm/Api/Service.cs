using System.Reflection;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class Service
	{
		private Service() { }

		public AgentC Agent { get; set; }
		public Framework Framework { get; set; }
		public Language Language { get; set; }
		public string Name { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Service))
		{
			{ "Name", Name }, { "Agent", Agent }, { "Framework", Framework }, { "Language", Language },
		}.ToString();

		internal static Service GetDefaultService(IConfigurationReader configurationReader)
			=> new Service
			{
				Name = configurationReader.ServiceName,
				Agent = new AgentC
				{
					Name = Consts.AgentName,
					Version = typeof(Agent).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
				}
			};

		public class AgentC
		{
			[JsonConverter(typeof(TrimmedStringJsonConverter))]
			public string Name { get; set; }

			[JsonConverter(typeof(TrimmedStringJsonConverter))]
			public string Version { get; set; }

			public override string ToString() => new ToStringBuilder(nameof(AgentC)) { { "Name", Name }, { "Version", Version } }.ToString();
		}
	}

	public class Framework
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Version { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Framework)) { { "Name", Name }, { "Version", Version } }.ToString();
	}

	public class Language
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Language)) { { "Name", Name } }.ToString();
	}
}
