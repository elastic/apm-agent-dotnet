using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// Contains tests related to stack traces
	/// </summary>
	public class StackTraceTests
	{
		/// <summary>
		/// Captures an HTTP request
		/// and makes sure that we have at least 1 stack frame with LineNo != 0
		/// This test assumes that LineNo capturing is enabled.
		/// </summary>
		[Fact]
		public async Task HttpClientStackTrace()
		{
			var (listener, _, _) = HttpDiagnosticListenerTest.RegisterListenerAndStartTransaction();

			using (listener)
			using (var localServer = new LocalServer(uri: "http://localhost:8083/"))
			{
				var httpClient = new HttpClient();
				var res = await httpClient.GetAsync(localServer.Uri);

				res.IsSuccessStatusCode.Should().BeTrue();
			}

			var stackFrames = (Agent.TransactionContainer.Transactions.Value.Spans[0] as Span)?.StackTrace;

			stackFrames.Should().NotBeEmpty().And.Contain(frame => frame.LineNo != 0);
		}
	}
}
