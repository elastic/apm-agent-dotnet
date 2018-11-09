using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core.Model.Payload
{
    public class Response
    {
        public bool Finished { get; set; }
        public int Status_code { get; set; }
    }
}
