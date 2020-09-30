using GrpcServiceSample;
using Microsoft.AspNetCore.Hosting;
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
			}).Build();

			return iHost;
		}
	}
}
