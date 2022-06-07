using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Profiler.Managed.Tests.AdoNet;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Docker;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests.AspNetCore
{
	public class AspNetCoreTests 
	{
		private readonly ITestOutputHelper _output;

		public AspNetCoreTests(ITestOutputHelper output) => _output = output;

		/// <summary>
		/// Runs SampleAspNetCoreApp by injecting the agent purely based on the profiler
		/// It calls /home/index which generates: 1) a transaction 2) db spans 3) http span
		/// Makes sure the expected transaction and spans are sent to APM Server
		/// </summary>
		/// <param name="framework"></param>
		[Theory]
		[InlineData("net5.0")]
		public async void AspNetCoreTest(string framework)
		{
			var apmLogger = new InMemoryBlockingLogger(Elastic.Apm.Logging.LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(AspNetCoreTests));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using (var profiledApplication = new ProfiledApplication("SampleAspNetCoreApp"))
			{
				IDictionary<string, string> environmentVariables = new Dictionary<string, string>
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ELASTIC_APM_LOG_LEVEL"] = "Trace",
					["SKIP_AGENT_REGISTRATION"] = "true"
				};

				// waiting for the "application started" log and make sure we send the http request after the app is running
				var waitForAppStart = new AutoResetEvent(false);
				// wait for the log that events are sent to server
				var waitForEventsSentToServer = new AutoResetEvent(false);

				profiledApplication.Start(
					framework,
					TimeSpan.FromMinutes(2),
					environmentVariables,
					null,
					line =>
					{
						_output.WriteLine(line.Line);
						if(line.Line.ToLower().Contains("application started"))
							waitForAppStart.Set();
						if(line.Line.ToLower().Contains("sent items to server:") && line.Line.ToLower().Contains("transaction"))
							waitForEventsSentToServer.Set();
					},
					exception => _output.WriteLine($"{exception}"),
					true, true);

				waitForAppStart.WaitOne(TimeSpan.FromSeconds(30));

				var httpClient = new HttpClient();
				var result = await httpClient.GetAsync("http://localhost:5000/home/index");
				result.IsSuccessStatusCode.Should().BeTrue();
				await result.Content.ReadAsStringAsync();

				waitForEventsSentToServer.WaitOne(TimeSpan.FromSeconds(30));
			}

			apmServer.ReceivedData.Transactions.Should().HaveCountGreaterOrEqualTo(1);
			apmServer.ReceivedData.Spans.Should().HaveCountGreaterOrEqualTo(1);

			apmServer.ReceivedData.Spans.Any(span => span.Context.Db != null).Should().BeTrue();
			apmServer.ReceivedData.Spans.Any(span => span.Context.Http != null).Should().BeTrue();

		}
	}
}
