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
public class AzureFunctionsIsolatedNotOverwriteTests : AzureFunctionsTestBase, IClassFixture<IsolatedContextNotOverwite>
{
	public AzureFunctionsIsolatedNotOverwriteTests(ITestOutputHelper output, IsolatedContextNotOverwite context)
		: base(output, context) { }

	[Fact]
	public async Task OverwriteDiscoverDefaultServiceName_False()
	{
		var transaction = await InvokeAndAssertFunction(SampleHttpTrigger);

		transaction.Outcome.Should().Be(Success);
		transaction.Result.Should().Be("HTTP 2xx");
		transaction.Context.Response.StatusCode.Should().Be(200);
	}
}
