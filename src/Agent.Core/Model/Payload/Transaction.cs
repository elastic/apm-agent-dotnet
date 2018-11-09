using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core.Model.Payload
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public long Duration { get; set; } //TODO datatype?

        public String Type { get; set; }

        public String Name { get; set; }

        public String Result { get; set; }

        public String Timestamp { get; set; } //Should be DateTime, since we calculate with this. 

        public Context Context { get; set; }

        public List<Span> Spans { get; set; } = new List<Span>(); //TODO: make lazy
    }
}
