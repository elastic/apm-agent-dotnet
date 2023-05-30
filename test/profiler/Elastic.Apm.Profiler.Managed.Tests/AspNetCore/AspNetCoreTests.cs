using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
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
		[InlineData("net7.0")]
		public async void AspNetCoreTest(string framework)
		{
			var apmLogger = new InMemoryBlockingLogger(Logging.LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(AspNetCoreTests));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using (var profiledApplication = new ProfiledApplication("SampleAspNetCoreApp", genericSampleApp: true))
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

				var seenSentItems = false;
				profiledApplication.Start(
					framework,
					TimeSpan.FromMinutes(4),
					environmentVariables,
					null,
					line =>
					{
						_output.WriteLine($"[SampleAspNetCoreApp] {line.Line}");
						if (line.Line.ToLower().Contains("application started"))
							waitForAppStart.Set();
						if (line.Line.ToLower().Contains("sent items to server:"))
							seenSentItems = true;
						if (seenSentItems && line.Line.ToLower().Contains($"{TextUtils.Indentation}transaction"))
							waitForEventsSentToServer.Set();
					},
					exception => _output.WriteLine($"[SampleAspNetCoreApp] Exception: {exception}"),
					true,
					true);

				if (!waitForAppStart.WaitOne(TimeSpan.FromSeconds(30)))
					throw new Exception($"SampleAspNetCoreApp did not start within 30seconds, unable to profile");

				var httpClient = new HttpClient();
				var result = await httpClient.GetAsync("http://localhost:5000/home/index");
				result.IsSuccessStatusCode.Should().BeTrue();
				await result.Content.ReadAsStringAsync();

				if (!waitForEventsSentToServer.WaitOne(TimeSpan.FromSeconds(30)))
					throw new Exception($"Waiting for events to be sent to the server took longer then 30 seconds");
			}

			apmServer.ReceivedData.Metadata.Should().HaveCountGreaterOrEqualTo(1);
			apmServer.ReceivedData.Metadata.First()
				.Service.Agent.ActivationMethod.Should()
				.Be(Consts.ActivationMethodProfiler);

			apmServer.ReceivedData.Transactions.Should().HaveCountGreaterOrEqualTo(1);
			apmServer.ReceivedData.Spans.Should().HaveCountGreaterOrEqualTo(1);

			apmServer.ReceivedData.Spans.Any(span => span.Context.Db != null).Should().BeTrue();
			apmServer.ReceivedData.Spans.Any(span => span.Context.Http != null).Should().BeTrue();
		}
	}
}
