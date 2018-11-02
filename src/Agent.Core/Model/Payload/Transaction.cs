using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core.Model.Payload
{
    class Transaction
    {
		public Guid Id { get; set; }
		public long Duration { get; set; } //TODO datatype?

		public String Type { get; set; }

		public String Name { get; set; }

		public String Result { get; set; }

		public String Timestamp { get; set; }

		public Context Context { get; set; }

		public List<Span> Spans { get; set; }
	}
}
