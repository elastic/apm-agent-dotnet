using System;
using System.Collections.Generic;

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

			public Context Context { get; set; }
			public string Culprit { get; set; }
			public CapturedException Exception { get; set; }
			public Guid Id { get; private set; }
			public string Timestamp { get; private set; }
			public Trans Transaction { get; set; }

			public class Trans
			{
				public Guid Id { get; set; }
			}
		}
	}

	public class CapturedException
	{
		public string Code { get; set; } //TODO

		public bool Handled { get; set; }

		public string Message { get; set; }

		public string Module { get; set; }

		public List<Stacktrace> Stacktrace { get; set; }

		public string Type { get; set; }
	}
}
