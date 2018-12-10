using System;
using System.Collections.Generic;

namespace Elastic.Apm.Model.Payload
{
    public class Error
    {
        public Service Service { get; set; }
        public List<Err> Errors { get; set; }

        public class Err
        {
            public Context Context { get; set; }
            public CapturedException Exception { get; set; }
            public Guid Id { get; set; }
            public Trans Transaction { get; set; }
            public String TimeStamp { get; set; }
            public String Culprit { get; set; }

            public class Trans
            {
                public Guid Id { get; set; }
            }
        }
    }

    public class CapturedException
    {
        public String Code { get; set; } //TODO

        public String Message { get; set; }

        public String Module { get; set; }

        public List<Stacktrace> Stacktrace { get; set; }

        public bool Handled { get; set; }

        public String Type { get; set; }
    }
}
