// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Http;
using System.Reflection;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;

namespace Elastic.Apm.AspNetCore.Tests
{
	public static class Helper
	{
		internal static HttpClient ConfigureHttpClient<T>(bool createDefaultClient, bool useOnlyDiagnosticSource, ApmAgent agent,
			WebApplicationFactory<T> factory
		) where T : class
		{
			HttpClient client;
			if (createDefaultClient)
			{
#pragma warning disable IDE0022 // Use expression body for methods
				client = GetClient(agent, factory, useOnlyDiagnosticSource);
#pragma warning restore IDE0022 // Use expression body for methods
#if NETCOREAPP3_0 || NETCOREAPP3_1
				client.DefaultRequestVersion = new Version(2, 0);
#endif
			}
			else
				client = GetClientWithoutExceptionPage(agent, factory, useOnlyDiagnosticSource);

			return client;
		}

		internal static HttpClient GetClient<T>(ApmAgent agent, WebApplicationFactory<T> factory, bool useOnlyDiagnosticSource) where T : class
		{
			var builder = factory
				.WithWebHostBuilder(n =>
				{
					n.Configure(app =>
					{
						if (useOnlyDiagnosticSource)
						{
							var subs = new IDiagnosticsSubscriber[]
							{
								new AspNetCoreDiagnosticSubscriber(), new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber()
							};
							agent.Subscribe(subs);
						}
						else
							app.UseElasticApm(agent, agent.Logger, new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());

						app.UseDeveloperExceptionPage();

						app.UseHttpsRedirection();
						app.UseStaticFiles();
						app.UseCookiePolicy();

						Startup.ConfigureRoutingAndMvc(app);
					});

					n.ConfigureServices(ConfigureServices);
				});

			return builder.CreateClient();
		}

		internal static HttpClient GetClientWithoutExceptionPage<T>(ApmAgent agent, WebApplicationFactory<T> factory, bool useOnlyDiagnosticSource)
			where T : class
		{
			var builder = factory
				.WithWebHostBuilder(n =>
				{
					n.Configure(app =>
					{
						if (useOnlyDiagnosticSource)
						{
							var subs = new IDiagnosticsSubscriber[]
							{
								new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber(), new AspNetCoreDiagnosticSubscriber()
							};
							agent.Subscribe(subs);
						}
						else
							app.UseElasticApm(agent, agent.Logger, new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());

						Startup.ConfigureRoutingAndMvc(app);
					});

					n.ConfigureServices(ConfigureServices);
				});

			return builder.CreateClient();
		}

		/// <summary>
		/// Configures the sample app without any diagnostic listener
		/// </summary>
		internal static HttpClient GetClientWithoutDiagnosticListeners<T>(ApmAgent agent, WebApplicationFactory<T> factory) where T : class
			=> factory.WithWebHostBuilder(n =>
				{
					n.Configure(app =>
					{
						app.UseMiddleware<ApmMiddleware>(agent.Tracer, agent);

						app.UseDeveloperExceptionPage();
						app.UseHsts();
						app.UseHttpsRedirection();
						app.UseStaticFiles();
						app.UseCookiePolicy();

						Startup.ConfigureRoutingAndMvc(app);
					});

					n.ConfigureServices(ConfigureServices);
				})
				.CreateClient();

		internal static void ConfigureServices(IServiceCollection services)
		{
			Startup.ConfigureServicesExceptMvc(services);
			services
				.AddMvc()
				//this is needed because of a (probably) bug:
				//https://github.com/aspnet/Mvc/issues/5992
				.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))));
		}
	}
}
