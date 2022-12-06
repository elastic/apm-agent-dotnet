using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Grpc.Net.Client;
using GrpcServiceSample;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Elastic.Apm.Grpc.Tests
{
	public class GrpcTests : IAsyncLifetime
	{
		private IHost _sampleAppHost;

		public Task InitializeAsync()
		{
			AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
			return Task.CompletedTask;
		}

		public async Task DisposeAsync()
		{
			await _sampleAppHost.StopAsync();
			_sampleAppHost.Dispose();
		}

		/// <summary>
		/// Creates a call to a simple gRPC service and asserts that all transactions, spans are captured
		/// and gRPC related fields are filled.
		/// </summary>
		/// <param name="withDiagnosticSource"></param>
		/// <returns></returns>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task BasicGrpcTest(bool withDiagnosticSource)
		{
			var payloadSender = new MockPayloadSender { IsStrictSpanCheckEnabled = true };
			using var apmAgent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));

			if (withDiagnosticSource)
			{
				_sampleAppHost = new SampleAppHostBuilder().BuildHost();
				apmAgent.Subscribe(new AspNetCoreDiagnosticSubscriber());
			}
			else
				_sampleAppHost = new SampleAppHostBuilder().BuildHostWithMiddleware(apmAgent);

			var grpcClientDiagnosticSubscriber = new GrpcClientDiagnosticSubscriber();
			apmAgent.Subscribe(grpcClientDiagnosticSubscriber, new HttpDiagnosticsSubscriber());

			await _sampleAppHost.StartAsync();

			var channel = GrpcChannel.ForAddress(SampleAppHostBuilder.SampleAppUrl);
			var client = new Greeter.GreeterClient(channel);

			await apmAgent.Tracer.CaptureTransaction("SampleCall", "test", async () =>
			{
				var response = await client.SayHelloAsync(
					new HelloRequest { Name = "World" });

				Debug.WriteLine(response.Message);
			});

			payloadSender.Transactions.Should().HaveCount(2);

			payloadSender.Transactions.Should()
				.Contain(transaction => transaction.Result == "OK"
					&& transaction.Name == $"/{Greeter.Descriptor.FullName}/{nameof(client.SayHello)}"
					&& transaction.Outcome == Outcome.Success);

			//Make sure all spans are collected
			Thread.Sleep(500);

			payloadSender.Spans.Should().HaveCountGreaterOrEqualTo(1);
			payloadSender.Spans.Should()
				.Contain(span => span.Subtype == ApiConstants.SubTypeGrpc
					&& span.Outcome == Outcome.Success
					&& span.Name == $"/{Greeter.Descriptor.FullName}/{nameof(client.SayHello)}"
					&& span.Context.Destination.Address == "localhost"
					&& span.Context.Destination.Port == SampleAppHostBuilder.SampleAppPort
					&& span.Context.Destination.Service.Resource == $"localhost:{SampleAppHostBuilder.SampleAppPort}"
				);

			grpcClientDiagnosticSubscriber.Listener.ProcessingRequests.Should().BeEmpty();
		}

		/// <summary>
		/// Calls a gRPC service that throws an exception.
		/// Makes sure that <see cref="IExecutionSegment.Outcome" /> is set correctly both on spans and on the gRPC transaction.
		/// </summary>
		/// <param name="withDiagnosticSource"></param>
		/// <returns></returns>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task FailingGrpcCallTest(bool withDiagnosticSource)
		{
			var payloadSender = new MockPayloadSender();
			using var apmAgent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			var grpcClientDiagnosticSubscriber = new GrpcClientDiagnosticSubscriber();
			_sampleAppHost = withDiagnosticSource
				? new SampleAppHostBuilder().BuildHost()
				: new SampleAppHostBuilder().BuildHostWithMiddleware(apmAgent);
			apmAgent.Subscribe(new AspNetCoreDiagnosticSubscriber(), grpcClientDiagnosticSubscriber, new HttpDiagnosticsSubscriber());

			await _sampleAppHost.StartAsync();

			var channel = GrpcChannel.ForAddress(SampleAppHostBuilder.SampleAppUrl);
			var client = new Greeter.GreeterClient(channel);

			await apmAgent.Tracer.CaptureTransaction("SampleCall", "test", async () =>
			{
				try
				{
					var response = await client.ThrowAnExceptionAsync(
						new HelloRequest { Name = "World" });

					Debug.WriteLine(response.Message);
				}
				catch (Exception e)
				{
					Debug.WriteLine(e);
				}
			});

			payloadSender.Transactions.Should()
				.Contain(transaction => transaction.Result == "UNKNOWN"
					&& transaction.Name == $"/{Greeter.Descriptor.FullName}/{nameof(client.ThrowAnException)}"
					&& transaction.Outcome == Outcome.Failure);

			//Make sure all spans are collected
			Thread.Sleep(500);

			payloadSender.Spans.Should().HaveCountGreaterOrEqualTo(1);
			payloadSender.Spans.Should()
				.Contain(span => span.Subtype == ApiConstants.SubTypeGrpc
					&& span.Outcome == Outcome.Failure
					&& span.Name == $"/{Greeter.Descriptor.FullName}/{nameof(client.ThrowAnException)}"
					&& span.Context.Destination.Address == "localhost"
					&& span.Context.Destination.Port == SampleAppHostBuilder.SampleAppPort
					&& span.Context.Destination.Service.Resource == $"localhost:{SampleAppHostBuilder.SampleAppPort}"
				);

			grpcClientDiagnosticSubscriber.Listener.ProcessingRequests.Should().BeEmpty();
		}
	}
}
