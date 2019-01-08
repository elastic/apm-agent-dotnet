using System;

namespace Elastic.Apm.Model.Payload
{
	public class Stacktrace
	{
		public string Filename { get; set; }
		public string Function { get; set; }
		public int Lineno { get; set; }
		public string Module { get; set; }
	}
}
