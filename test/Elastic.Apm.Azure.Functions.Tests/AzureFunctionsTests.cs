// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.MockApmServer;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.Functions.Tests;

[Collection("AzureFunctions")]
public class AzureFunctionsTests : IClassFixture<AzureFunctionsTestFixture>, IDisposable
{
	private readonly AzureFunctionsTestFixture _azureFunctionsTestFixture;
	private readonly ITestOutputHelper _output;
	private static bool _isFirst = true;

	public AzureFunctionsTests(ITestOutputHelper output, AzureFunctionsTestFixture azureFunctionsTestFixture)
	{
		_output = output;
		_azureFunctionsTestFixture = azureFunctionsTestFixture;
		_output.WriteLine("=== START SUT Log ===");
		foreach (var line in _azureFunctionsTestFixture.LogLines)
			_output.WriteLine(line);
		_output.WriteLine("=== END SUT Log ===");
	}

	public void Dispose() => _azureFunctionsTestFixture.ClearTransaction();

	private async Task<TransactionDto> InvokeFunction(string url, string transactionName)
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
		var transaction = _azureFunctionsTestFixture.WaitForTransaction(transactionName);
		Assert_ColdStart(transaction);
		return transaction;
	}

	[Fact]
	public async Task Invoke_Http_Ok()
	{
		var transaction =
			await InvokeFunction("http://localhost:7071/api/SampleHttpTrigger", "GET /api/SampleHttpTrigger");

		transaction.Should().NotBeNull();
		transaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		transaction.FaaS.Id.Should()
			.Be(
				"/subscriptions/abcd1234-abcd-acdc-1234-112233445566/resourceGroups/testfaas_group/providers/Microsoft.Web/sites/testfaas/functions/SampleHttpTrigger");
		transaction.FaaS.Name.Should().Be("testfaas/SampleHttpTrigger");
		transaction.FaaS.Trigger.Type.Should().Be("http");
		transaction.Outcome.Should().Be(Outcome.Success);
		transaction.Result.Should().Be("HTTP 2xx");
		transaction.Context.Request.Method.Should().Be("GET");
		transaction.Context.Request.Url.Full.Should().Be("http://localhost:7071/api/SampleHttpTrigger");
		transaction.Context.Response.StatusCode.Should().Be(200);
	}

	private static void Assert_ColdStart(TransactionDto transaction)
	{
		transaction.FaaS.ColdStart.Should().Be(_isFirst);
		_isFirst = false;
	}

	[Fact]
	public async Task Invoke_Http_InternalServerError()
	{
		var transaction = await InvokeFunction("http://localhost:7071/api/HttpTriggerWithInternalServerError",
			"GET /api/HttpTriggerWithInternalServerError");

		transaction.Should().NotBeNull();
		transaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		transaction.FaaS.Id.Should()
			.Be(
				"/subscriptions/abcd1234-abcd-acdc-1234-112233445566/resourceGroups/testfaas_group/providers/Microsoft.Web/sites/testfaas/functions/HttpTriggerWithInternalServerError");
		transaction.FaaS.Name.Should().Be("testfaas/HttpTriggerWithInternalServerError");
		transaction.FaaS.Trigger.Type.Should().Be("http");
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
		var transaction = await InvokeFunction("http://localhost:7071/api/HttpTriggerWithException",
			"GET /api/HttpTriggerWithException");

		transaction.Should().NotBeNull();
		transaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		transaction.FaaS.Id.Should()
			.Be(
				"/subscriptions/abcd1234-abcd-acdc-1234-112233445566/resourceGroups/testfaas_group/providers/Microsoft.Web/sites/testfaas/functions/HttpTriggerWithException");
		transaction.FaaS.Name.Should().Be("testfaas/HttpTriggerWithException");
		transaction.FaaS.Trigger.Type.Should().Be("http");
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
		var transaction = await InvokeFunction("http://localhost:7071/api/HttpTriggerWithNotFound",
			"GET /api/HttpTriggerWithNotFound");

		transaction.Should().NotBeNull();
		transaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
		transaction.FaaS.Id.Should()
			.Be(
				"/subscriptions/abcd1234-abcd-acdc-1234-112233445566/resourceGroups/testfaas_group/providers/Microsoft.Web/sites/testfaas/functions/HttpTriggerWithNotFound");
		transaction.FaaS.Name.Should().Be("testfaas/HttpTriggerWithNotFound");
		transaction.FaaS.Trigger.Type.Should().Be("http");
		transaction.Outcome.Should().Be(Outcome.Success);
		transaction.Result.Should().Be("HTTP 4xx");
		transaction.Context.Request.Method.Should().Be("GET");
		transaction.Context.Request.Url.Full.Should()
			.Be("http://localhost:7071/api/HttpTriggerWithNotFound");
		transaction.Context.Response.StatusCode.Should().Be(404);
	}
}
