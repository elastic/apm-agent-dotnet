using System;
using System.Collections.Generic;

namespace Elastic.Apm.Model.Payload
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public long Duration { get; set; } //TODO datatype?

        public String Type { get; set; }

        public String Name { get; set; }

        public String Result { get; set; }

        public String Timestamp => TimestampInDateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");

        //TODO: not part of intake, hide from serialization
        public DateTime TimestampInDateTime { get; set; }

        public Context Context { get; set; }

        public List<Span> Spans { get; set; } = new List<Span>(); //TODO: make lazy
    }
}
