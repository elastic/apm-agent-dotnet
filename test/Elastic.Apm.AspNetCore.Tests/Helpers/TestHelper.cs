using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using AspNetCoreSampleApp;
using Elastic.Apm.AspNetCore.Tests.Services;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Elastic.Apm.NetCoreAll.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

namespace Elastic.Apm.AspNetCore.Tests.Helpers
{
	public static class TestHelper
	{
		internal static HttpClient GetClient<T>(WebApplicationFactory<T> factory, StartupConfigService startupConfigService) where T : class =>
			factory.WithWebHostBuilder(builder =>
				{
					builder.UseSolutionRelativeContentRoot(@"sample\AspNetCoreSampleApp");

					builder.ConfigureTestServices(services =>
					{
						services.AddSingleton(startupConfigService);
						services.AddMvc().AddApplicationPart(typeof(Startup).Assembly);
					});
				})
				.CreateClient();

		internal static HttpClient GetClient<T>(WebApplicationFactory<T> factory, ApmAgent agent, bool useElasticApm, bool useDeveloperExceptionPage, params IDiagnosticsSubscriber[] subscribers) where T : class =>
			GetClient(factory, new StartupConfigService(agent, useElasticApm, useDeveloperExceptionPage, subscribers));

		internal static HttpClient GetClient<T>(WebApplicationFactory<T> factory, ApmAgent agent, bool useDeveloperExceptionPage = true) where T : class =>
			GetClient(factory, agent, true, useDeveloperExceptionPage, new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());

		internal static HttpClient GetClientWithoutSubscribers<T>(WebApplicationFactory<T> factory, ApmAgent agent) where T : class =>
			GetClient(factory, agent, false, true, Array.Empty<IDiagnosticsSubscriber>());

		internal static HttpClient GetClientWithoutDeveloperExceptionPage<T>(WebApplicationFactory<T> factory, ApmAgent agent) where T : class =>
			GetClient(factory, agent, false);
	}
}
