using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Report;

namespace Elastic.Apm.Metrics
{
	internal class MetricsCollector : IDisposable, IMetricsCollector
	{
		private const int MaxTryWithoutSuccess = 5;
		private readonly IApmLogger _logger;

		private readonly List<IMetricsProvider> _metricsProviders;
		private readonly IPayloadSender _payloadSender;

		private readonly Timer _timer;

		public MetricsCollector(IApmLogger logger, IPayloadSender payloadSender, IConfigurationReader configurationReader)
		{
			_metricsProviders = new List<IMetricsProvider>
			{
				new FreeAndTotalMemoryProvider(), new FreeAndTotalMemoryProvider(), new SystemTotalCpuProvider(_logger)
			};

			_logger = logger.Scoped(nameof(MetricsCollector));
			_payloadSender = payloadSender;

			logger.Debug()?.Log("starting MetricsCollector");

			var interval = configurationReader.MetricsIntervalInMillisecond;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (interval == 0) return;

			_timer = new Timer();
			_timer.Elapsed += (sender, args) => { CollectAllMetrics(); };
		}

		public void StartCollecting() => _timer?.Start();

		internal void CollectAllMetrics()
		{
			var samples = new List<Sample>();


			foreach (var metricsProvider in _metricsProviders)
			{
				if (metricsProvider.ConsecutiveNumberOfFailedReads == MaxTryWithoutSuccess)
				{
					try
					{
						var sample = metricsProvider.GetValue();
						if (sample != null)
						{
							var sampleArray = sample as Sample[] ?? sample.ToArray();
							if (sampleArray.Any())
								samples.AddRange(sampleArray);

							metricsProvider.ConsecutiveNumberOfFailedReads = 0;
						}
						else
							metricsProvider.ConsecutiveNumberOfFailedReads++;
					}
					catch (Exception e)
					{
						metricsProvider.ConsecutiveNumberOfFailedReads++;
						_logger.Error()
							?.LogException(e, "Failed reading {metricsProvider.NameInLogs} {numberOfFail} times", metricsProvider.NameInLogs,
								metricsProvider.ConsecutiveNumberOfFailedReads);
					}
				}
				if (metricsProvider.ConsecutiveNumberOfFailedReads == MaxTryWithoutSuccess)
				{
					_logger.Info()
						?.Log("Reached maximum number of fails - the agent won't try reading {operationName} anymore", metricsProvider.NameInLogs);
				}
			}

			var metricSet = new MetricSet(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000, samples);

			_payloadSender.QueueMetrics(metricSet);
			_logger.Debug()?.Log("Metrics collected: {data}", samples.Select(n => n.ToString()).Aggregate((i, j) => i + ", " + j));
		}

		public void Dispose()
		{
			_timer?.Stop();
			_timer?.Dispose();
		}
	}
}
