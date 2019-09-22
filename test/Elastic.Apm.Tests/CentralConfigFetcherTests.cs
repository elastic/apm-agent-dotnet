using Elastic.Apm.BackendComm;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests
{
	public class CentralConfigFetcherTests : LoggingTestBase
	{
		public CentralConfigFetcherTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[Fact]
		public void Dispose_stops_the_thread()
		{
			CentralConfigFetcher lastCentralConfigFetcher;
			using (var agent = new ApmAgent(new AgentComponents()))
			{
				lastCentralConfigFetcher = (CentralConfigFetcher)agent.CentralConfigFetcher;
				lastCentralConfigFetcher.IsRunning.Should().BeTrue();
			}
			lastCentralConfigFetcher.IsRunning.Should().BeFalse();
		}
	}
}
