using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Elastic.Apm.AspNetCore.Tests.Factories
{
	public class CustomWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
	{
		protected override IWebHostBuilder CreateWebHostBuilder() =>
			WebHost.CreateDefaultBuilder(null)
				.UseStartup<TEntryPoint>();
	}
}
