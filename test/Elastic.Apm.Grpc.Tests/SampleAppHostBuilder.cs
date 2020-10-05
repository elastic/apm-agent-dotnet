using Elastic.Apm.AspNetCore;
using GrpcServiceSample;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elastic.Apm.Grpc.Tests
{
	public class SampleAppHostBuilder
	{
		public static int SampleAppPort = 5866;
		public static string SampleAppUrl = $"http://localhost:{SampleAppPort}";

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
