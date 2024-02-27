// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Elastic.Apm.Tests.MockApmServer;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.Functions.Tests;

public class AzureFunctionsTestBase : IDisposable
{
	private readonly ITestOutputHelper _output;
	private AzureFunctionTestContextBase Context { get; }

	protected AzureFunctionsTestBase(ITestOutputHelper output, AzureFunctionTestContextBase context)
	{
		_output = output;
		Context = context;

		_output.WriteLine("=== START SUT Log ===");
		foreach (var line in Context.LogLines)
			_output.WriteLine(line);
		_output.WriteLine("=== END SUT Log ===");
	}

	public void Dispose() => Context.ClearTransaction();

	internal async Task<TransactionDto> InvokeAndAssertFunction(string functionName)
	{
		var uri = Context.CreateUri($"/api/{functionName}");
		var transaction = await Context.InvokeFunction(_output, uri);
		transaction.Should().NotBeNull();
		AssertMetaData(Context.GetMetaData());
		AssertColdStart(transaction);
		AssertTracing(transaction);
		AssertFaas(transaction, functionName);
		AssertUrl(transaction, uri);
		return transaction;
	}

	private static void AssertUrl(TransactionDto transaction, Uri uri)
	{
		transaction.Context.Request.Method.Should().Be("GET");
		transaction.Context.Request.Url.Full.Should().Be(uri.ToString());
	}

	private void AssertColdStart(TransactionDto transaction)
	{
		transaction.FaaS.ColdStart.Should().Be(Context.IsFirst);
		Context.IsFirst = false;
	}

	private void AssertMetaData(MetadataDto metaData)
	{
		metaData.Service.Agent.ActivationMethod.Should().Be(Consts.ActivationMethodNuGet);
		metaData.Cloud.Provider.Should().Be("azure");
		metaData.Cloud.Service.Name.Should().Be("functions");
		metaData.Service.Name.Should().Be(Context.WebsiteName);
		metaData.Service.Runtime.Name.Should().Be(Context.RuntimeName);
		metaData.Service.Framework.Name.Should().Be("Azure Functions");
		metaData.Service.Framework.Version.Should().Be("4");
		// TODO - removing this assertion as we can no longer seem to set this value without causing a host error
		//metaData.Service.Node.ConfiguredName.Should().Be("20367ea8-70b9-41b4-a552-b2a826b3aa0b");
	}

	private static void AssertTracing(TransactionDto transaction) =>
		transaction.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");

	private void AssertFaas(TransactionDto transaction, string functionName)
	{
		var subscription = $"abcd1234-abcd-acdc-1234-112233445566/resourceGroups/{Context.WebsiteName}_group";
		var provider = $"Microsoft.Web/sites/{Context.WebsiteName}";

		transaction.FaaS.Id.Should()
			.Be($"/subscriptions/{subscription}/providers/{provider}/functions/{functionName}");

		transaction.FaaS.Name.Should().Be($"{Context.WebsiteName}/{functionName}");

		transaction.FaaS.Trigger.Type.Should().Be("http");
	}
}
