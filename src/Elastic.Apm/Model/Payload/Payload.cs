using System;
using System.Collections.Generic;
using Elastic.Apm.Api;

namespace Elastic.Apm.Model.Payload
{
	public interface IPayload
	{
		 Service Service { get; set; }

		 List<ITransaction> Transactions { get; set; }
	}

	internal class Payload : IPayload
	{
		public Service Service { get; set; }

		public List<ITransaction> Transactions { get; set; }
	}
}
