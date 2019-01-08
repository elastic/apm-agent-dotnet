using System.Net.Http;
using System.Reflection;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Tests.Mock;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SampleAspNetCoreApp.Data;

namespace Elastic.Apm.AspNetCore.Tests
{
	public static class Helper
	{
		internal static HttpClient GetClient<T>(MockPayloadSender payloadSender, WebApplicationFactory<T> factory) where T : class
			=> factory
				.WithWebHostBuilder(n =>
				{
					n.Configure(app =>
					{
						app.UseElasticApm(payloadSender: payloadSender);
						new ElasticCoreListeners().Start();
						new ElasticEntityFrameworkCoreListener().Start();

						app.UseDeveloperExceptionPage();

						app.UseHsts();

						app.UseHttpsRedirection();
						app.UseStaticFiles();
						app.UseCookiePolicy();

						app.UseMvc(routes =>
						{
							routes.MapRoute(
								"default",
								"{controller=Home}/{action=Index}/{id?}");
						});
					});

					n.ConfigureServices(ConfigureServices);
				})
				.CreateClient();

		internal static HttpClient GetClientWithoutExceptionPage<T>(MockPayloadSender payloadSender, WebApplicationFactory<T> factory) where T : class
			=> factory
				.WithWebHostBuilder(n =>
				{
					n.Configure(app =>
					{
						app.UseElasticApm(payloadSender: payloadSender);
						new ElasticCoreListeners().Start();
						new ElasticEntityFrameworkCoreListener().Start();

						app.UseMvc(routes =>
						{
							routes.MapRoute(
								"default",
								"{controller=Home}/{action=Index}/{id?}");
						});
					});

					n.ConfigureServices(ConfigureServices);
				})
				.CreateClient();

		private static void ConfigureServices(IServiceCollection services)
		{
			services.Configure<CookiePolicyOptions>(options =>
			{
				options.CheckConsentNeeded = context => true;
				options.MinimumSameSitePolicy = SameSiteMode.None;
			});

			var connection = @"Data Source=blogging.db";
			services.AddDbContext<SampleDataContext>
				(options => options.UseSqlite(connection));

			services.AddMvc()
				//this is needed because of a (probably) bug:
				//https://github.com/aspnet/Mvc/issues/5992
				.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(SampleAspNetCoreApp))))
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
		}
	}
}
