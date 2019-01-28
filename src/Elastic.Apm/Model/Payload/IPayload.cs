using System.Collections.Generic;
using Elastic.Apm.Api;

namespace Elastic.Apm.Model.Payload
{
	public interface IPayload
	{
		Service Service { get; set; }
		List<ITransaction> Transactions { get; set; }
	}
}
