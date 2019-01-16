using System.Reflection;
using Elastic.Apm.Config;

namespace Elastic.Apm.Model.Payload
{
	public class Service
	{
		public AgentC Agent { get; set; }
		public Framework Framework { get; set; }
		public Language Language { get; set; }
		public string Name { get; set; }

		private Service() { }

		public class AgentC
		{
			public string Name { get; set; }
			public string Version { get; set; }
		}

		internal static Service GetDefaultService(IConfigurationReader configurationReader)
		{
			var n = new Service
			{
				Name = configurationReader.ServiceName,
				Agent = new AgentC
				{
					Name = Consts.AgentName,
					Version = Consts.AgentVersion
				}
			};

			return n;
		}
	}

	public class Framework
	{
		public string Name { get; set; }
		public string Version { get; set; }
	}

	public class Language
	{
		public string Name { get; set; }
	}
}
