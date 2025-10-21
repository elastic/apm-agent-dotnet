// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Http;
using System.Reflection;
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
		internal static HttpClient ConfigureHttpClient<T>(bool createDefaultClient, ApmAgent agent,
			WebApplicationFactory<T> factory
		) where T : class
		{
			HttpClient client;
			if (createDefaultClient)
			{
#pragma warning disable IDE0022 // Use expression body for methods
				client = GetClient(agent, factory);
#pragma warning restore IDE0022 // Use expression body for methods
				client.DefaultRequestVersion = new Version(2, 0);
			}
			else
				client = GetClientWithoutExceptionPage(agent, factory);

			return client;
		}

		private static IDiagnosticsSubscriber[] UseApmListeners =>
			new IDiagnosticsSubscriber[] { new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber() };

		internal static HttpClient GetClient<T>(ApmAgent agent, WebApplicationFactory<T> factory) where T : class
		{
			var builder = factory
				.WithWebHostBuilder(n =>
				{
					n.Configure(app =>
					{
						app.UseElasticApm(agent, agent.Logger, UseApmListeners);

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

		internal static HttpClient GetClientWithoutExceptionPage<T>(ApmAgent agent, WebApplicationFactory<T> factory)
			where T : class
		{
			var builder = factory
				.WithWebHostBuilder(n =>
				{
					n.Configure(app =>
					{
						app.UseElasticApm(agent, agent.Logger, UseApmListeners);

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
						app.UseElasticApm(agent, agent.Logger);
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
