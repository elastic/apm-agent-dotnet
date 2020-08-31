// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.NetCoreAll.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

namespace Elastic.Apm.AspNetCore.Tests
{
	public static class Helper
	{
		internal static HttpClient GetClient<T>(ApmAgent agent, WebApplicationFactory<T> factory, bool useOnlyDiagnosticSource) where T : class
		{
			var builder = factory
				.WithWebHostBuilder(n =>
				{
					n.Configure(app =>
					{
						if (useOnlyDiagnosticSource)
						{
							var subs = new List<IDiagnosticsSubscriber>()
							{
								//new AspNetCoreErrorDiagnosticsSubscriber(),
								new AspNetCorePageLoadDiagnosticSubscriber()
							};
							agent.Subscribe(subs.ToArray());
						}
						else
						{
							app.UseElasticApm(agent, agent.Logger, new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());
						}

						app.UseDeveloperExceptionPage();

						app.UseHttpsRedirection();
						app.UseStaticFiles();
						app.UseCookiePolicy();

						RegisterRoutingAndMvc(app);
					});

					n.ConfigureServices(ConfigureServices);
				});
#if !NETCOREAPP2_1 && !NETCOREAPP2_2
			builder.Server.AllowSynchronousIO = true;
#endif
			return builder.CreateClient();
		}

		internal static HttpClient GetClientWithoutExceptionPage<T>(ApmAgent agent, WebApplicationFactory<T> factory, bool useOnlyDiagnosticSource) where T : class
		{
			var builder = factory
				  .WithWebHostBuilder(n =>
				  {
					  n.Configure(app =>
					  {
						  if (useOnlyDiagnosticSource)
						  {
							  var subs = new List<IDiagnosticsSubscriber>()
							{
								new AspNetCoreErrorDiagnosticsSubscriber(),
								new AspNetCorePageLoadDiagnosticSubscriber()
							};
							  agent.Subscribe(subs.ToArray());
						  }
						  else
						  {
							  app.UseElasticApm(agent, agent.Logger, new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber());
						  }

						  RegisterRoutingAndMvc(app);
					  });

					  n.ConfigureServices(ConfigureServices);
				  });
#if !NETCOREAPP2_1 && !NETCOREAPP2_2
			builder.Server.AllowSynchronousIO = true;
#endif
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

						RegisterRoutingAndMvc(app);
					});

					n.ConfigureServices(ConfigureServices);
				})
				.CreateClient();

		private static void RegisterRoutingAndMvc(IApplicationBuilder app)
		{
#if NETCOREAPP3_0 || NETCOREAPP3_1
			app.UseRouting();

			app.UseAuthentication();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
				endpoints.MapControllers();
				endpoints.MapRazorPages();
			});
#else
			app.UseAuthentication();

			app.UseMvc(routes =>
			{
				routes.MapRoute(
					"default",
					"{controller=Home}/{action=Index}/{id?}");
			});
#endif
		}

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
