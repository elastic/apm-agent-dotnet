using System;

namespace Elastic.Apm.Model.Payload
{
    public class Stacktrace
    {
        public int Lineno { get; set; }
        public string Filename { get; set; }
        public string Function { get; set; }
        public String Module { get; set; }
    }
}
