namespace Elastic.Apm.Model.Payload
{
	public class Service
	{
		public AgentC Agent { get; set; }
		public Framework Framework { get; set; }
		public Language Language { get; set; }
		public string Name { get; set; }

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
