using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Tests.Mocks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class MockApmServer : MockPayloadSink
	{
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private Task _taskCompletedWhenStopped;

		internal void Start() =>
			_taskCompletedWhenStopped = Program.CreateWebHostBuilder()
				.ConfigureServices(services =>
				{
					services.AddMvc()
						.AddApplicationPart(Assembly.Load(new AssemblyName(nameof(Tests.MockApmServer))))
						.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
				})
				.Configure(app => { Startup.Configure(app); })
				.UseUrls("http://localhost:5050")
				.Build()
				.RunAsync(_cancellationTokenSource.Token);

		internal async Task StopAsync()
		{
			_cancellationTokenSource.Cancel();
			await _taskCompletedWhenStopped;
		}
	}
}
