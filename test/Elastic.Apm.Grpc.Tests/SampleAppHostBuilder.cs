using GrpcServiceSample;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Elastic.Apm.Grpc.Tests
{
	public class SampleAppHostBuilder
	{
		public IHost BuildHost()
		{
			var iHost =
			Host.CreateDefaultBuilder()
			.ConfigureWebHostDefaults(webBuilder =>
			{
				webBuilder.UseStartup<Startup>();
			}).Build();

			return iHost;
		}
	}
}
