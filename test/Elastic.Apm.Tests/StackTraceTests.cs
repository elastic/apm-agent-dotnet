using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Helpers;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Contains tests related to stack traces
	/// </summary>
	public class StackTraceTests
	{
		private readonly XunitOutputLogger _xUnitOutputLogger;

		public StackTraceTests(ITestOutputHelper xUnitOutputHelper) => _xUnitOutputLogger = new XunitOutputLogger(xUnitOutputHelper);

		/// <summary>
		/// Captures an HTTP request
		/// and makes sure that we have at least 1 stack frame with LineNo != 0
		/// This test assumes that LineNo capturing is enabled.
		/// </summary>
		[Fact]
		public async Task HttpClientStackTrace()
		{
			var (listener, payloadSender, _) = HttpDiagnosticListenerTest.RegisterListenerAndStartTransaction(_xUnitOutputLogger);

			using (listener)
			using (var localServer = new LocalServer(uri: "http://localhost:8083/"))
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
			}

			var stackFrames = payloadSender.FirstSpan?.StackTrace;

			stackFrames.Should().NotBeEmpty().And.Contain(frame => frame.LineNo != 0);
		}
	}
}
