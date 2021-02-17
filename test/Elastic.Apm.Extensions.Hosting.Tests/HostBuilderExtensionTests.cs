using System;
using System.Threading.Tasks;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleConsoleNetCoreApp;
using Xunit;
using Microsoft.Extensions.Logging;

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
			using var hostBuilder1 = CreateHostBuilder().Build();
			using var hostBuilder2 = CreateHostBuilder().Build();
			var builder1Task = hostBuilder1.StartAsync();
			var builder2Task = hostBuilder2.StartAsync();

			await Task.WhenAll(builder1Task, builder2Task);
			await Task.WhenAll(hostBuilder1.StopAsync(), hostBuilder2.StopAsync());
		}

		/// <summary>
		/// Makes sure that <see cref="Agent.IsConfigured" /> is <code>true</code> after the agent is enabled through
		/// <see cref="HostBuilderExtensions.UseElasticApm" />.
		/// </summary>
		[Fact]
		public void IsAgentInitializedAfterUseElasticApm()
		{
			using var _ = CreateHostBuilder().Build();
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

			using var _ = Host.CreateDefaultBuilder()
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
				using var _ = Host.CreateDefaultBuilder()
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

		private static IHostBuilder CreateHostBuilder() =>
			Host.CreateDefaultBuilder()
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
