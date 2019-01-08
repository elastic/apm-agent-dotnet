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
			public String Culprit { get; set; }
			public CapturedException Exception { get; set; }
			public Guid Id { get; private set; }
			public String Timestamp { get; private set; }
			public Trans Transaction { get; set; }

			public class Trans
			{
				public Guid Id { get; set; }
			}
		}
	}

	public class CapturedException
	{
		public String Code { get; set; } //TODO

		public bool Handled { get; set; }

		public String Message { get; set; }

		public String Module { get; set; }

		public List<Stacktrace> Stacktrace { get; set; }

		public String Type { get; set; }
	}
}
