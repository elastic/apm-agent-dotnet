using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core.Model.Payload
{
    public class Service
    {
        public Agent Agent { get; set; }
        public String Name { get; set; }
        public Framework Framework { get; set; }
        public Language Language { get; set; }
    }


    public class Framework
    {
        public String Name { get; set; }
        public String Version { get; set; }
    }

    public class Language
    {
        public String Name { get; set; }
    }
}
