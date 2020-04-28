using Elastic.Apm.BackendComm;
using Elastic.Apm.BackendComm.CentralConfig;

namespace Elastic.Apm.Tests.Mocks
{
	public class NoopCentralConfigFetcher : ICentralConfigFetcher
	{
		public void Dispose() { }
	}
}
