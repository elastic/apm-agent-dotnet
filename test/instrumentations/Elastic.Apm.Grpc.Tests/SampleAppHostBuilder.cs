using Elastic.Apm.AspNetCore;
using Elastic.Apm.Grpc.Tests.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elastic.Apm.Grpc.Tests
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
			=> services.AddGrpc();

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGrpcService<GreeterService>();

				endpoints.MapGet("/",
					async context =>
					{
						await context.Response.WriteAsync(
							"Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
					});
			});
		}
	}

	public class SampleAppHostBuilder
	{
		public static readonly int SampleAppPort = 5866;
		public static readonly string SampleAppUrl = $"http://localhost:{SampleAppPort}";

		public IHost BuildHost()
		{
			var iHost =
				Host.CreateDefaultBuilder()
					.ConfigureWebHostDefaults(webBuilder =>
					{
						webBuilder.UseUrls(SampleAppUrl);
						webBuilder.UseStartup<Startup>();
					})
					.Build();

			return iHost;
		}

		internal IHost BuildHostWithMiddleware(ApmAgent agent)
		{
			var iHost = Host.CreateDefaultBuilder()
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseUrls(SampleAppUrl);
					webBuilder.Configure(app =>
						{
							app.UseElasticApm(agent, agent.Logger);

							app.UseRouting();

							app.UseEndpoints(endpoints =>
							{
								endpoints.MapGrpcService<GreeterService>();

								endpoints.MapGet("/",
									async context =>
									{
										await context.Response.WriteAsync(
											"Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
									});
							});
						})
						.ConfigureServices(services => { services.AddGrpc(); });
				})
				.Build();

			return iHost;
		}
	}
}
