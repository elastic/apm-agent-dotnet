namespace Elastic.Apm.Tests.MockApmServer
{
	internal class MockApmServerSingleton
	{
		private readonly MockApmServer _mockApmServer = new MockApmServer();

		internal MockApmServer EnsureServerIsRunning() => _mockApmServer;

		internal void StopServer() { }
	}
}
