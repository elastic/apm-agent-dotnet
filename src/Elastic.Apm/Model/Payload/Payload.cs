using System.Collections.Generic;

namespace Elastic.Apm.Model.Payload
{
    public class Payload
    {
        public Service Service { get; set; }

        public List<Transaction> Transactions { get; set; }
    }
}
