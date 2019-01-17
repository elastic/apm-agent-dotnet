using System.Collections.Generic;
using Elastic.Apm.Api;

namespace Elastic.Apm.Model.Payload
{
	public class Payload
	{
		public Service Service { get; set; }

		public List<ITransaction> Transactions { get; set; }
	}
}
