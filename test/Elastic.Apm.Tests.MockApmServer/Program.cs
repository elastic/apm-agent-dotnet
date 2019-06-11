using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Elastic.Apm.Tests.MockApmServer
{
	// ReSharper disable once ClassNeverInstantiated.Global
	public class Program
	{
		public static void Main(string[] args) => CreateWebHostBuilder(args).Build().Run();

		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>()
				.UseUrls("http://localhost:5050");

		public static IWebHostBuilder CreateWebHostBuilder() => CreateWebHostBuilder(new string[0]);
	}
}
