using Elastic.Apm.BackendComm;

namespace Elastic.Apm.Tests.Mocks
{
	public class NoopCentralConfigFetcher : ICentralConfigFetcher
	{
		public void Dispose() { }
	}
}
