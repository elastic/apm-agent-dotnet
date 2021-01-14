using System;
using System.Threading.Tasks;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleConsoleNetCoreApp;
using Xunit;

namespace Elastic.Apm.Extensions.Hosting.Tests
{
	public class HostBuilderExtensionTests
	{
		/// <summary>
		/// Makes sure in case of 2 IHostBuilder insatnces when both call UseElasticApm no exception is thrown
		/// </summary>
		/// <returns></returns>
		[Fact]
		public async Task TwoHostBuildersNoException()
		{
			var hostBuilder1 = CreateHostBuilder().Build();
			var hostBuilder2 = CreateHostBuilder().Build();
			var builder1Task = hostBuilder1.StartAsync();
			var builder2Task = hostBuilder2.StartAsync();

			await Task.WhenAll(builder1Task, builder2Task);
			await Task.WhenAll(hostBuilder1.StopAsync(), hostBuilder2.StopAsync());
		}

		[Fact]
		public async Task CaptureErrorLogsAsApmError()
		{
			var payloadSender = new MockPayloadSender();
			var hostBuilder = CreateHostBuilder(payloadSender).Build();

			var builderTask = hostBuilder.StartAsync();

			await builderTask;
			payloadSender.WaitForErrors(TimeSpan.FromSeconds(5));
			payloadSender.Errors.Should().NotBeEmpty();

			payloadSender.FirstError.Log.Message.Should().Be("This is a sample error log message, with a sample value: 42");
			payloadSender.FirstError.Log.ParamMessage.Should().Be("This is a sample error log message, with a sample value: {intParam}");

			await hostBuilder.StopAsync();
		}

		/// <summary>
		/// Makes sure that <see cref="Agent.IsConfigured" /> is <code>true</code> after the agent is enabled through
		/// <see cref="HostBuilderExtensions.UseElasticApm" />.
		/// </summary>
		[Fact]
		public void IsAgentInitializedAfterUseElasticApm()
		{
			var _ = CreateHostBuilder().Build();
			Agent.IsConfigured.Should().BeTrue();
		}

		/// <summary>
		/// Makes sure that agent enables the <see cref="IDiagnosticsSubscriber" /> passed into
		/// <see cref="HostBuilderExtensions.UseElasticApm" />.
		/// </summary>
		[Fact]
		public void DiagnosticSubscriberWithUseElasticApm()
		{
			var fakeSubscriber = new FakeSubscriber();
			fakeSubscriber.IsSubscribed.Should().BeFalse();

			Host.CreateDefaultBuilder()
				.ConfigureServices((context, services) => { services.AddHostedService<HostedService>(); })
				.UseElasticApm(fakeSubscriber)
				.Build();

			fakeSubscriber.IsSubscribed.Should().BeTrue();
		}

		/// <summary>
		/// Sets `enabled=false` and makes sure that <see cref="HostBuilderExtensions.UseElasticApm" /> does not turn on diagnostic
		/// listeners.
		/// </summary>
		[Fact]
		public void DiagnosticSubscriberWithUseElasticApmAgentDisabled()
		{
			var fakeSubscriber = new FakeSubscriber();
			fakeSubscriber.IsSubscribed.Should().BeFalse();

			Environment.SetEnvironmentVariable("ELASTIC_APM_ENABLED", "false");

			try
			{
				Host.CreateDefaultBuilder()
					.ConfigureServices((context, services) => { services.AddHostedService<HostedService>(); })
					.UseElasticApm(fakeSubscriber)
					.Build();

				fakeSubscriber.IsSubscribed.Should().BeFalse();
			}
			finally
			{
				Environment.SetEnvironmentVariable("ELASTIC_APM_ENABLED", null);
			}
		}

		private static IHostBuilder CreateHostBuilder(MockPayloadSender payloadSender = null) =>
			Host.CreateDefaultBuilder()
				.ConfigureServices(n=> n.AddSingleton<IPayloadSender, MockPayloadSender>( n=> payloadSender))
				.ConfigureServices((context, services) => { services.AddHostedService<HostedService>(); })
				.UseElasticApm();


		public class FakeSubscriber : IDiagnosticsSubscriber
		{
			public bool IsSubscribed { get; set; }

			public IDisposable Subscribe(IApmAgent components)
			{
				IsSubscribed = true;
				return null;
			}
		}
	}
}
