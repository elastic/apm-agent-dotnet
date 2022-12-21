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
	private readonly MockApmServer _apmServer;
	private readonly Process _funcProcess;
	private readonly ITestOutputHelper _output;
	private readonly AutoResetEvent _waitForTransactionDataEvent = new(false);
	private readonly bool _logFuncOutput;

	public AzureFunctionsTests(ITestOutputHelper output)
	{
		_logFuncOutput = true;
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
					["ELASTIC_APM_LOG_LEVEL"] = "Trace",
					["ELASTIC_APM_FLUSH_INTERVAL"] = "0"
				},
				UseShellExecute = false
			}
		};
		if (_logFuncOutput)
		{
			_funcProcess.StartInfo.RedirectStandardOutput = true;
			_funcProcess.OutputDataReceived += (sender, args) => _output.WriteLine("[func] " + args.Data);
		}
		_output.WriteLine($"{DateTime.Now}: Starting func tool");
		var isStarted = _funcProcess.Start();
		isStarted.Should().BeTrue("Could not start Azure Functions Core Tools");
		if (_logFuncOutput)
		{
			_funcProcess.BeginOutputReadLine();
		}
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
		_output.WriteLine($"Invoking {url} ...");
		var attempt = 0;
		const int maxAttempts = 60;
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Add("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
		while (++attempt < maxAttempts)
		{
			try
			{
				var result = await httpClient.GetAsync(url);
				var s = await result.Content.ReadAsStringAsync();
				_output.WriteLine(s);
				break;
			}
			catch
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
			}
		}

		attempt.Should().BeLessThan(maxAttempts, $"Could not connect to function running on {url}");

		_waitForTransactionDataEvent.WaitOne(TimeSpan.FromSeconds(60));
	}

	[Fact]
	public async Task Invoke_Http_Ok()
	{
		await InvokeFunction("http://localhost:7071/api/SampleHttpTrigger");

		_apmServer.ReceivedData.Transactions.Should().HaveCountGreaterOrEqualTo(1);
		var transaction =
			_apmServer.ReceivedData.Transactions.SingleOrDefault(t => t.Name == "GET /api/SampleHttpTrigger");
		transaction.Should().NotBeNull();
		transaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		transaction.FaaS.Id.Should()
			.Be(
				"/subscriptions/abcd1234-abcd-acdc-1234-112233445566/resourceGroups/testfaas_group/providers/Microsoft.Web/sites/testfaas/functions/SampleHttpTrigger");
		transaction.FaaS.Name.Should().Be("testfaas/SampleHttpTrigger");
		transaction.FaaS.Trigger.Type.Should().Be("http");
		transaction.FaaS.ColdStart.Should().BeTrue();
		transaction.Outcome.Should().Be(Outcome.Success);
		transaction.Result.Should().Be("HTTP 2xx");
		transaction.Context.Request.Method.Should().Be("GET");
		transaction.Context.Request.Url.Full.Should().Be("http://localhost:7071/api/SampleHttpTrigger");
		transaction.Context.Response.StatusCode.Should().Be(200);
	}

	[Fact]
	public async Task Invoke_Http_InternalServerError()
	{
		await InvokeFunction("http://localhost:7071/api/HttpTriggerWithInternalServerError");

		_apmServer.ReceivedData.Transactions.Should().HaveCountGreaterOrEqualTo(1);
		var transaction =
			_apmServer.ReceivedData.Transactions.SingleOrDefault(t =>
				t.Name == "GET /api/HttpTriggerWithInternalServerError");
		transaction.Should().NotBeNull();
		transaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		transaction.FaaS.Id.Should()
			.Be(
				"/subscriptions/abcd1234-abcd-acdc-1234-112233445566/resourceGroups/testfaas_group/providers/Microsoft.Web/sites/testfaas/functions/HttpTriggerWithInternalServerError");
		transaction.FaaS.Name.Should().Be("testfaas/HttpTriggerWithInternalServerError");
		transaction.FaaS.Trigger.Type.Should().Be("http");
		transaction.FaaS.ColdStart.Should().BeTrue();
		transaction.Outcome.Should().Be(Outcome.Failure);
		transaction.Result.Should().Be("HTTP 5xx");
		transaction.Context.Request.Method.Should().Be("GET");
		transaction.Context.Request.Url.Full.Should()
			.Be("http://localhost:7071/api/HttpTriggerWithInternalServerError");
		transaction.Context.Response.StatusCode.Should().Be(500);
	}

	[Fact]
	public async Task Invoke_Http_FunctionThrowsException()
	{
		await InvokeFunction("http://localhost:7071/api/HttpTriggerWithException");

		_apmServer.ReceivedData.Transactions.Should().HaveCountGreaterOrEqualTo(1);
		var transaction =
			_apmServer.ReceivedData.Transactions.SingleOrDefault(t =>
				t.Name == "GET /api/HttpTriggerWithException");
		transaction.Should().NotBeNull();
		transaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		transaction.FaaS.Id.Should()
			.Be(
				"/subscriptions/abcd1234-abcd-acdc-1234-112233445566/resourceGroups/testfaas_group/providers/Microsoft.Web/sites/testfaas/functions/HttpTriggerWithException");
		transaction.FaaS.Name.Should().Be("testfaas/HttpTriggerWithException");
		transaction.FaaS.Trigger.Type.Should().Be("http");
		transaction.FaaS.ColdStart.Should().BeTrue();
		transaction.Outcome.Should().Be(Outcome.Failure);
		transaction.Result.Should().Be("failure");
		transaction.Context.Request.Method.Should().Be("GET");
		transaction.Context.Request.Url.Full.Should()
			.Be("http://localhost:7071/api/HttpTriggerWithException");
		transaction.Context.Response.Should().BeNull();
	}

	[Fact]
	public async Task Invoke_Http_NotFound()
	{
		await InvokeFunction("http://localhost:7071/api/HttpTriggerWithNotFound");

		_apmServer.ReceivedData.Transactions.Should().HaveCountGreaterOrEqualTo(1);
		var transaction =
			_apmServer.ReceivedData.Transactions.SingleOrDefault(t =>
				t.Name == "GET /api/HttpTriggerWithNotFound");
		transaction.Should().NotBeNull();
		transaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		transaction.FaaS.Id.Should()
			.Be(
				"/subscriptions/abcd1234-abcd-acdc-1234-112233445566/resourceGroups/testfaas_group/providers/Microsoft.Web/sites/testfaas/functions/HttpTriggerWithNotFound");
		transaction.FaaS.Name.Should().Be("testfaas/HttpTriggerWithNotFound");
		transaction.FaaS.Trigger.Type.Should().Be("http");
		transaction.FaaS.ColdStart.Should().BeTrue();
		transaction.Outcome.Should().Be(Outcome.Success);
		transaction.Result.Should().Be("HTTP 4xx");
		transaction.Context.Request.Method.Should().Be("GET");
		transaction.Context.Request.Url.Full.Should()
			.Be("http://localhost:7071/api/HttpTriggerWithNotFound");
		transaction.Context.Response.StatusCode.Should().Be(404);
	}
}
