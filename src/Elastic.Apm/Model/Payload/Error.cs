using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	public class Error
	{
		public List<Err> Errors { get; set; }
		public Service Service { get; set; }

		public class Err
		{
			public Err()
			{
				Timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
				Id = Guid.NewGuid();
			}

			internal Context Context { get; set; }
			public string Culprit { get; set; }
			public CapturedException Exception { get; set; }
			internal Guid Id { get; }
			private string Timestamp { get; }
			internal Trans Transaction { get; set; }

			public class Trans
			{
				public Guid Id { get; set; }
			}
		}
	}

	public class CapturedException
	{
		internal string Code { get; set; } //TODO

		internal bool Handled { get; set; }

		/// <summary>
		/// The exception message, see: <see cref="Exception.Message"/>
		/// </summary>
		public string Message { get; set; }

		internal string Module { get; set; }

		[JsonProperty("Stacktrace")]
		internal List<Stacktrace> StacktTrace { get; set; }

		/// <summary>
		/// The type of the exception class
		/// </summary>
		public string Type { get; set; }
	}
}
