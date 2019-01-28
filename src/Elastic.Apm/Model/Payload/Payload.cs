using System.Collections.Generic;
using Elastic.Apm.Api;

namespace Elastic.Apm.Model.Payload
{
	internal class Payload : IPayload
	{
		public Service Service { get; set; }

		public List<ITransaction> Transactions { get; set; }
	}
}
