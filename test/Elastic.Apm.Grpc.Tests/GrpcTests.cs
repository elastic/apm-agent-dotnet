using System;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Grpc.Net.Client;
using GrpcServiceSample;
using Xunit;

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

			var channel = GrpcChannel.ForAddress("https://localhost:5001");
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

			await sampleAppHost.StopAsync();
		}
	}
}
