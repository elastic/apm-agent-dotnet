using System;
using System.Diagnostics.Contracts;
using System.Net;
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
		private const string ThisClassName = nameof(MockApmServer);

		private readonly IApmLogger _logger;
		internal readonly ReceivedData ReceivedData = new ReceivedData();
		private readonly string _dbgCurrentTestName;
		private readonly object _lock = new object();

		internal MockApmServer(IApmLogger logger, string dbgCurrentTestName)
		{
			_logger = logger.Scoped(ThisClassName);
			_dbgCurrentTestName = dbgCurrentTestName;
		}

		private static class PortScanRange
		{
			internal const int Begin = 23_456;
			internal const int End = 24_567; // not included
		}

		private CancellationTokenSource _cancellationTokenSource;
		private int _port;
		private Task _runningTask;

		internal IApmLogger InternalLogger => _logger;

		internal int FindAvailablePortToListen()
		{
			var numberOfPortsTried = 0;
			var numberOfPortsInScanRange = PortScanRange.End - PortScanRange.Begin;
			var currentPort = RandomGenerator.GetInstance().Next(PortScanRange.Begin, PortScanRange.End);
			while (true)
			{
				++numberOfPortsTried;
				try
				{
					_logger.Debug()?.Log("Trying to listen on port {PortNumber}...", currentPort);
					var listener = new HttpListener();
					listener.Prefixes.Add($"http://localhost:{currentPort}/");
					listener.Start();
					listener.Stop();
					_logger.Debug()?.Log("Port {PortNumber} is available - it will be used to accept connections from the agent", currentPort);
					return currentPort;
				}
				catch (HttpListenerException ex)
				{
					_logger.Debug()
						?.LogException(ex, "Failed to listen on port {PortNumber}. " +
							"Number of ports tried so far: {NumberOfPorts} out of {NumberOfPorts}",
							currentPort, numberOfPortsTried, numberOfPortsInScanRange);
					if (numberOfPortsTried == numberOfPortsInScanRange)
					{
						throw new InvalidOperationException("Could not find an available port for Mock APM Server to listen. " +
							$"Ports range that was tried: [{PortScanRange.Begin}, {PortScanRange.End})");
					}
				}

				if (currentPort + 1 == PortScanRange.End) currentPort = PortScanRange.Begin;
				else ++currentPort;
			}
		}

		internal void RunInBackground(int port)
		{
			Contract.Requires(_cancellationTokenSource == null);
			Contract.Requires(_runningTask == null);

			_cancellationTokenSource = new CancellationTokenSource();
			_port = port;
			_runningTask = CreateWebHostBuilder().Build().RunAsync(_cancellationTokenSource.Token);
			_logger.Info()?.Log("Started: {MockApmServer}", this);
		}

		internal void Run(int port)
		{
			_port = port;
			var webHost = CreateWebHostBuilder().Build();
			_logger.Info()?.Log("About to start: {MockApmServer}", this);
			webHost.Run();
		}

		internal Task<TResult> DoUnderLock<TResult>(Func<Task<TResult>> asyncFunc)
		{
			lock (_lock) return asyncFunc();
		}

		internal async Task StopAsync()
		{
			Contract.Requires(_cancellationTokenSource != null);
			Contract.Requires(_runningTask != null);

			_cancellationTokenSource.Cancel();
			await _runningTask;
			_cancellationTokenSource = null;
			_runningTask = null;

			_logger.Info()?.Log("Stopped");
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
				.UseUrls($"http://localhost:{_port}");

		public override string ToString() =>
			new ToStringBuilder(ThisClassName) { { "port", _port }, { "current test", _dbgCurrentTestName }, }.ToString();
	}
}
