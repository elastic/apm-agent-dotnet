// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Apm.Tests.MockApmServer
{
	public class MockApmServer
	{
		private const string ThisClassName = nameof(MockApmServer);

		private readonly string _dbgCurrentTestName;
		private readonly bool _useHttps;
		private readonly object _lock = new object();

		public ReceivedData ReceivedData { get; } = new ReceivedData();

		/// <summary>
		/// Raised when the APM server received data
		/// </summary>
		public event Action<object> OnReceive;

		internal void AddInvalidPayload(string payload)
		{
			ReceivedData.InvalidPayloadErrors = ReceivedData.InvalidPayloadErrors.Add(payload);
			OnReceive?.Invoke(payload);
		}

		internal void AddError(ErrorDto error)
		{
			ReceivedData.Errors = ReceivedData.Errors.Add(error);
			OnReceive?.Invoke(error);
		}

		internal void AddTransaction(TransactionDto transaction)
		{
			ReceivedData.Transactions = ReceivedData.Transactions.Add(transaction);
			OnReceive?.Invoke(transaction);
		}

		internal void AddSpan(SpanDto span)
		{
			ReceivedData.Spans = ReceivedData.Spans.Add(span);
			OnReceive?.Invoke(span);
		}

		internal void AddMetricSet(MetricSetDto metricSet)
		{
			ReceivedData.Metrics = ReceivedData.Metrics.Add(metricSet);
			OnReceive?.Invoke(metricSet);
		}

		internal void AddMetadata(MetadataDto metadata)
		{
			ReceivedData.Metadata = ReceivedData.Metadata.Add(metadata);
			OnReceive?.Invoke(metadata);
		}

		public MockApmServer(IApmLogger logger, string dbgCurrentTestName, bool useHttps = false)
		{
			InternalLogger = logger.Scoped(ThisClassName);
			_dbgCurrentTestName = dbgCurrentTestName;
			_useHttps = useHttps;

			if (useHttps)
			{
				using var ms = new MemoryStream();
				using var certStream = typeof(MockApmServer).Assembly.GetManifestResourceStream("Elastic.Apm.Tests.MockApmServer.cert.pfx");
				certStream.CopyTo(ms);
				_serverCertificate = ms.ToArray();
			}
		}

		private static class PortScanRange
		{
			internal const int Begin = 23_456;
			internal const int End = 24_567; // not included
		}

		private CancellationTokenSource _cancellationTokenSource;
		private volatile Func<HttpRequest, HttpResponse, IActionResult> _getAgentsConfig;
		private int _port;
		private Task _runningTask;
		private readonly byte[] _serverCertificate;

		internal Func<HttpRequest, HttpResponse, IActionResult> GetAgentsConfig
		{
			get => _getAgentsConfig;
			set => _getAgentsConfig = value;
		}

		internal IApmLogger InternalLogger { get; }

		public int FindAvailablePortToListen()
		{
			var numberOfPortsTried = 0;
			const int numberOfPortsInScanRange = PortScanRange.End - PortScanRange.Begin;
			var currentPort = RandomGenerator.GetInstance().Next(PortScanRange.Begin, PortScanRange.End);
			while (true)
			{
				++numberOfPortsTried;
				try
				{
					InternalLogger.Debug()?.Log("Trying to listen on port {PortNumber}...", currentPort);
					var listener = new HttpListener();
					listener.Prefixes.Add($"http://localhost:{currentPort}/");
					listener.Start();
					listener.Stop();
					InternalLogger.Debug()?.Log("Port {PortNumber} is available - it will be used to accept connections from the agent", currentPort);
					return currentPort;
				}
				catch (HttpListenerException ex)
				{
					InternalLogger.Debug()
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

		public void RunInBackground(int port)
		{
			Assertion.IfEnabled?.That(_cancellationTokenSource == null, "");
			Assertion.IfEnabled?.That(_runningTask == null, "");

			_cancellationTokenSource = new CancellationTokenSource();
			_port = port;
			_runningTask = CreateWebHostBuilder().Build().RunAsync(_cancellationTokenSource.Token);
			InternalLogger.Info()?.Log("Started: {MockApmServer}", this);
		}

		public void Run(int port)
		{
			_port = port;
			var webHost = CreateWebHostBuilder().Build();
			InternalLogger.Info()?.Log("About to start: {MockApmServer}", this);
			webHost.Run();
		}

		internal TResult DoUnderLock<TResult>(Func<TResult> func)
		{
			lock (_lock) return func();
		}

		internal Task<TResult> DoUnderLock<TResult>(Func<Task<TResult>> asyncFunc)
		{
			lock (_lock) return asyncFunc();
		}

		public async Task StopAsync()
		{
			Assertion.IfEnabled?.That(_cancellationTokenSource != null, "");
			Assertion.IfEnabled?.That(_runningTask != null, "");

			// ReSharper disable once PossibleNullReferenceException
			_cancellationTokenSource.Cancel();
			// ReSharper disable once PossibleNullReferenceException
			await _runningTask;
			_cancellationTokenSource = null;
			_runningTask = null;

			InternalLogger.Info()?.Log("Stopped");
		}

		internal void ClearState() => ReceivedData.Clear();

		private IWebHostBuilder CreateWebHostBuilder() =>
			WebHost.CreateDefaultBuilder(new string[0])
				.ConfigureServices(services =>
				{
					services.AddMvc()
						.AddApplicationPart(typeof(MockApmServer).Assembly)
#if !NET5_0
						.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
#else
						;
#endif

					// Add this instance of MockApmServer as injected dependency for controllers
					services.AddSingleton(this);
				})
				.UseStartup<Startup>()
				.UseKestrel(k =>
				{
					if (_useHttps)
					{
						k.ConfigureHttpsDefaults(h =>
						{
							h.ServerCertificate = new X509Certificate2(_serverCertificate, "password");
						});
					}
				})
				.UseUrls(_useHttps ? $"https://localhost:{_port}" : $"http://localhost:{_port}");

		public override string ToString() =>
			new ToStringBuilder(ThisClassName) { { "port", _port }, { "current test", _dbgCurrentTestName } }.ToString();
	}
}
