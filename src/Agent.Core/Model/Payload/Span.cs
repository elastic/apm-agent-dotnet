using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core.Model.Payload
{
    public class Span
    {
        public ContextC Context { get; set; }

        public double Duration { get; set; }

        public String Name { get; set; }

        public String Type { get; set; }

        public Decimal Start { get; set; }

        public int Id { get; set; }

        public List<Stacktrace> Stacktrace { get; set; }

        public Span()
        {
            //TODO: just a test
            Random rnd = new Random();
            Id = rnd.Next();
        }

        public class ContextC
        {
            public Db Db { get; set; }
            public Http Http { get; set; }
        }
    }

    public class Db
    {
        public String Instance { get; set; }

        public String Statement { get; set; }

        public String Type { get; set; }
    }

    public class Http 
    {
        public String Url { get; set; }
        public int Status_code { get; set; }
        public String Method { get; set; }
    }

    public class Stacktrace
    {
        public int Lineno { get; set; }
        public string Filename { get; set; }
        public string Function { get; set; }
        public String Module { get; set; }
    }
}
