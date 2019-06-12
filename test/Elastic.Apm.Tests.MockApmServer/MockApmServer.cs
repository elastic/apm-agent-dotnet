using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Apm.Tests.MockApmServer
{
	public class MockApmServer
	{
		internal readonly ReceivedData ReceivedData = new ReceivedData();
		private readonly string _dbgCurrentTestName;

		internal MockApmServer(IApmLogger logger, string dbgCurrentTestName)
		{
			Logger = logger.Scoped(nameof(MockApmServer));
			_dbgCurrentTestName = dbgCurrentTestName;
		}

		private CancellationTokenSource _cancellationTokenSource;
		private Task _runningTask;

		internal IApmLogger Logger { get; }

		internal int Port { get; private set; }

		internal void RunAsync()
		{
			Contract.Requires(_cancellationTokenSource == null);
			Contract.Requires(_runningTask == null);
			Contract.Requires(Port == 0);

			_cancellationTokenSource = new CancellationTokenSource();
			_runningTask = CreateWebHostBuilder().Build().RunAsync(_cancellationTokenSource.Token);

			Logger.Info()?.Log("Started: {MockApmServer}", this);
		}

		internal void Run()
		{
			var webHost = CreateWebHostBuilder().Build();
			Logger.Info()?.Log("About to start: {MockApmServer}", this);
			webHost.Run();
		}

		internal async Task StopAsync()
		{
			Contract.Requires(_cancellationTokenSource != null);
			Contract.Requires(_runningTask != null);
			Contract.Requires(Port != 0);

			_cancellationTokenSource.Cancel();
			await _runningTask;
			_cancellationTokenSource = null;
			_runningTask = null;
			Port = 0;

			Logger.Info()?.Log("Stopped");
		}

		private IWebHostBuilder CreateWebHostBuilder() =>
			WebHost.CreateDefaultBuilder(new string[0])
				.ConfigureServices(services =>
				{
					services.AddMvc()
						.AddApplicationPart(typeof(MockApmServer).Assembly)
						.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

					// Add this instance of MockApmServer as injected dependency for controllers
					services.AddSingleton(this);
				})
				.UseStartup<Startup>()
				.UseUrls($"http://localhost:{FindPort()}");

		private int FindPort()
		{
			Port = 8200;
			return Port;
		}

		public override string ToString() =>
			new ToStringBuilder(nameof(MockApmServer)) { { "port", Port }, { "current test", _dbgCurrentTestName }, }.ToString();
	}
}
