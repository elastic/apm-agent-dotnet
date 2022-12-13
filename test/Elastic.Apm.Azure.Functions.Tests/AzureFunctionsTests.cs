// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.Functions.Tests;


[Collection("AzureFunctions")]
public class AzureFunctionsTests : IAsyncLifetime
{
	private readonly Process _funcProcess;
	private readonly AutoResetEvent _waitForTransactionDataEvent = new(false);
	private readonly MockApmServer _apmServer;
	private readonly ITestOutputHelper _output;

	public AzureFunctionsTests(ITestOutputHelper output)
	{
		_output = output;
		_apmServer = new MockApmServer(new InMemoryBlockingLogger(LogLevel.Warning), nameof(AzureFunctionsTests));
		_apmServer.OnReceive += o =>
		{
			if (!_apmServer.ReceivedData.Transactions.IsEmpty)
				_waitForTransactionDataEvent.Set();
		};
		var port = _apmServer.FindAvailablePortToListen();
		_apmServer.RunInBackground(port);

		var workingDir = Path.Combine(Directory.GetCurrentDirectory(),
			"../../../../../sample/Elastic.AzureFunctionApp.Isolated");
		_output.WriteLine($"func working directory: {workingDir}");
		Directory.Exists(workingDir).Should().BeTrue();

		_funcProcess = new Process
		{
			StartInfo =
			{
				FileName = "func",
				Arguments = "start",
				WorkingDirectory = workingDir,
				EnvironmentVariables =
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ELASTIC_APM_FLUSH_INTERVAL"] = "0",
					["FUNCTIONS_EXTENSION_VERSION"] = "<dummy>",
					["WEBSITE_OWNER_NAME"] = "abcd1234-abcd-acdc-1234-112233445566+testfaas_group-CentralUSwebspace-Linux",
					["WEBSITE_SITE_NAME"] = "unit_test",
				},
				UseShellExecute = false
			}
		};

		_output.WriteLine($"{DateTime.Now}: starting func tool");
		var isStarted = _funcProcess.Start();
		isStarted.Should().BeTrue("Could not start Azure Functions Core Tools");
	}

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync()
	{
		_funcProcess?.Kill();
		if (_apmServer != null)
			await _apmServer?.StopAsync();
	}

	private async Task InvokeFunction(string url)
	{
		var attempt = 0;
		const int maxAttempts = 60;
		using var httpClient = new HttpClient();
		while (++attempt < maxAttempts)
		{
			try
			{
				var result = await httpClient.GetAsync(url);
				if (result.IsSuccessStatusCode)
					break;
			}
			catch
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
			}
		}

		attempt.Should().BeLessThan(maxAttempts, $"Could not connect to function running on {url}");

		_output.WriteLine($"{DateTime.Now}: func tool ready");
	}

	[Fact]
	public async Task Invoke_Http_Ok()
	{
		await InvokeFunction("http://localhost:7071/api/SampleHttpTrigger");

		_waitForTransactionDataEvent.WaitOne(TimeSpan.FromSeconds(30));

		_apmServer.ReceivedData.Transactions.Should().HaveCountGreaterOrEqualTo(1);
		var transaction =
			_apmServer.ReceivedData.Transactions.SingleOrDefault(t => t.Name == "GET /api/SampleHttpTrigger");
		transaction.Should().NotBeNull();
		transaction.FaaS.Id.Should().Be("/subscriptions/abcd1234-abcd-acdc-1234-112233445566/resourceGroups/testfaas_group/providers/Microsoft.Web/sites/unit_test/functions/SampleHttpTrigger");
		transaction.FaaS.Name.Should().Be("unit_test/SampleHttpTrigger");
		transaction.FaaS.Trigger.Type.Should().Be("http");
		transaction.FaaS.ColdStart.Should().BeTrue();
		transaction.Outcome.Should().Be(Outcome.Success);
		transaction.Result.Should().Be("HTTP 2xx");
	}
}
