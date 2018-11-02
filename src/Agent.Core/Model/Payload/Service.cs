using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core.Model.Payload
{
    class Service
    {
		public Agent Agent { get; set; }
		public String Name { get; set; }
		public Framework Framework { get; set; }
		public Language Language { get; set; }
	}


	class Framework
	{
		public String Name { get; set; }
		public String Version { get; set; }
	}

	class Language
	{
		public String Name { get; set; }
	}
}
