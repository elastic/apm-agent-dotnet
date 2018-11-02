using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core.Model.Payload
{
    class Context
    {
		public Request Request { get; set; }

		public Response Response { get; set; }
	}
}
