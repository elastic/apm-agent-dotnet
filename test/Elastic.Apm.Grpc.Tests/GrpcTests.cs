using System;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Grpc.Net.Client;
using GrpcServiceSample;
using Xunit;

#pragma warning disable CS0436 // Type conflicts with imported type

namespace Elastic.Apm.Grpc.Tests
{
	public class GrpcTests
	{
		[Fact]
		public async Task BasicGrpcTest()
		{
			var sampleAppHost = new SampleAppHostBuilder().BuildHost();

			var payloadSender = new MockPayloadSender();

			using var apmAgent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			apmAgent.Subscribe(new IDiagnosticsSubscriber[]
			{
				new AspNetCoreDiagnosticSubscriber(),
				new GrpcClientDiagnosticSubscriber(),
				new HttpDiagnosticsSubscriber()
			});

			await sampleAppHost.StartAsync();

			// This switch must be set before creating the GrpcChannel/HttpClient.
			AppContext.SetSwitch(
				"System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

			var channel = GrpcChannel.ForAddress(SampleAppHostBuilder.SampleAppUrl);
			var client = new Greeter.GreeterClient(channel);

			await apmAgent.Tracer.CaptureTransaction("SampleCall", "test", async () =>
			{
				var response = await client.SayHelloAsync(
					new HelloRequest { Name = "World" });

				Console.WriteLine(response.Message);
				Console.WriteLine("Hello World!");
			});

			payloadSender.Transactions.Should()
				.Contain(transaction => transaction.Result == "OK"
						&& transaction.Name == $"/{Greeter.Descriptor.FullName}/{nameof(client.SayHello)}");

			var vv = (payloadSender.Spans[1]).Context.Destination;
			payloadSender.Spans.Should().Contain(span => span.Subtype == ApiConstants.SubTypeGrpc
					&& span.Name == $"/{Greeter.Descriptor.FullName}/{nameof(client.SayHello)}"
					&& span.Context.Destination.Address == "localhost"
					&& span.Context.Destination.Port == SampleAppHostBuilder.SampleAppPort
					&& span.Context.Destination.Service.Type == "external"
					&& span.Context.Destination.Service.Name == SampleAppHostBuilder.SampleAppUrl
					&& span.Context.Destination.Service.Resource == $"localhost:{SampleAppHostBuilder.SampleAppPort}"
					);

			await sampleAppHost.StopAsync();
		}
	}
}
