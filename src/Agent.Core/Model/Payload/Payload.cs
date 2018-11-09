using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core.Model.Payload
{
    public class Payload
    {
        public Service Service { get; set; }

        public List<Transaction> Transactions { get; set; }
    }
}
