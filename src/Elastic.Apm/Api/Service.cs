using Elastic.Apm.Config;

namespace Elastic.Apm.Api
{
	public class Service
	{
		private Service() { }

		public AgentC Agent { get; set; }
		public Framework Framework { get; set; }
		public Language Language { get; set; }
		public string Name { get; set; }

		internal static Service GetDefaultService(IConfigurationReader configurationReader)
			=> new Service
			{
				Name = configurationReader.ServiceName,
				Agent = new AgentC
				{
					Name = Consts.AgentName,
					Version = typeof(Agent).Assembly.GetName().Version?.ToString() ?? "n/a"
				}
			};

		public class AgentC
		{
			public string Name { get; set; }
			public string Version { get; set; }
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
