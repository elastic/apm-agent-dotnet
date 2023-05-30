
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Elastic.Apm.StartupHook.Sample
{
	public class Program
	{
		public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

		public static IWebHostBuilder CreateHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();
	}
}
