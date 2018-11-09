using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core.Model.Payload
{
    public class Context
    {
        public Request Request { get; set; }

        public Response Response { get; set; }
    }
}
