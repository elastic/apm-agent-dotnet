// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.Api.Outcome;
using static Elastic.Apm.AzureFunctionApp.Core.FunctionName;

namespace Elastic.Apm.Azure.Functions.Tests;

[Collection("AzureFunctions")]
public class AzureFunctionsInProcessTests : AzureFunctionsTestBase, IClassFixture<InProcessContext>
{
	public AzureFunctionsInProcessTests(ITestOutputHelper output, InProcessContext context)
		: base(output, context) { }


	[Fact]
	public async Task Invoke_Http_Ok()
	{
		var transaction = await InvokeAndAssertFunction(SampleHttpTrigger);

		transaction.Outcome.Should().Be(Success);
		transaction.Result.Should().Be("HTTP 2xx");
		transaction.Context.Response.StatusCode.Should().Be(200);
	}

	[Fact]
	public async Task Invoke_Http_InternalServerError()
	{
		var transaction = await InvokeAndAssertFunction(HttpTriggerWithInternalServerError);

		transaction.Outcome.Should().Be(Failure);
		transaction.Result.Should().Be("HTTP 5xx");
		transaction.Context.Response.StatusCode.Should().Be(500);
	}

	[Fact]
	public async Task Invoke_Http_FunctionThrowsException()
	{
		var transaction = await InvokeAndAssertFunction(HttpTriggerWithException);

		transaction.Outcome.Should().Be(Failure);
		transaction.Result.Should().Be("HTTP 5xx");
		transaction.Context.Response.Should().BeNull();
	}

	[Fact]
	public async Task Invoke_Http_NotFound()
	{
		var transaction = await InvokeAndAssertFunction(HttpTriggerWithNotFound);

		transaction.Outcome.Should().Be(Success);
		transaction.Result.Should().Be("HTTP 4xx");
		transaction.Context.Response.StatusCode.Should().Be(404);
	}
}
