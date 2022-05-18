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
	public class AspNetCoreTests : IClassFixture<MySqlFixture>
	{
		private readonly MySqlFixture _fixture;
		private readonly ITestOutputHelper _output;

		public AspNetCoreTests(MySqlFixture fixture, ITestOutputHelper output)
		{
			_fixture = fixture;
			_output = output;
		}

		[DockerTheory]
		[InlineData("net5.0")]
		public async void AspNetCoreTest(string framework)
		{
			var apmLogger = new InMemoryBlockingLogger(Elastic.Apm.Logging.LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(AspNetCoreTests));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			var ignoreTopic = "ignore-topic";
			using (var profiledApplication = new ProfiledApplication("SampleAspNetCoreApp"))
			{
				IDictionary<string, string> environmentVariables = new Dictionary<string, string>
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["SKIP_AGENT_REGISTRATION"] = "true"
				};

				var waitForAppStart = new AutoResetEvent(false);


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
					},
					exception => _output.WriteLine($"{exception}"),
					true);

				waitForAppStart.WaitOne(TimeSpan.FromSeconds(30));

				var httpClient = new HttpClient();
				var result = await httpClient.GetAsync("http://localhost:5000/home/index");
				result.IsSuccessStatusCode.Should().BeTrue();
				await result.Content.ReadAsStringAsync();
			}

			apmServer.ReceivedData.Transactions.Should().HaveCount(1);
		}
	}
}
