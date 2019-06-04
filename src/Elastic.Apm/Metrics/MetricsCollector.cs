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
	/// <summary>
	/// Iterates through a list of <see cref="IMetricsProvider"/> and
	/// sends the values through an <see cref="IPayloadSender"/> instance.
	///
	/// It collects the metrics on an interval read from the <see cref="IConfigurationReader"/> which is
	/// passed to the constructor.
	///
	/// In case reading a value from an <see cref="IMetricsProvider"/> fails it retries <see cref="MaxTryWithoutSuccess"/> times,
	/// after that it prints a log and won't retry anymore. This is to avoid endlessly trying to read values without success.
	/// </summary>
	internal class MetricsCollector : IDisposable, IMetricsCollector
	{
		internal const int MaxTryWithoutSuccess = 5;
		private readonly IApmLogger _logger;

		/// <summary>
		/// List of all providers that can provide metrics values.
		/// Add new providers to this list in case you want the agent to collect more metrics
		/// </summary>
		internal readonly List<IMetricsProvider> MetricsProviders;
		private readonly IPayloadSender _payloadSender;

		private readonly Timer _timer;

		public MetricsCollector(IApmLogger logger, IPayloadSender payloadSender, IConfigurationReader configurationReader)
		{
			_logger = logger.Scoped(nameof(MetricsCollector));

			MetricsProviders = new List<IMetricsProvider>
			{
				new FreeAndTotalMemoryProvider(),
				new ProcessWorkingSetAndVirtualMemoryProvider(),
				new SystemTotalCpuProvider(_logger),
				new ProcessTotalCpuTimeProvider()
			};

			_payloadSender = payloadSender;

			logger.Debug()?.Log("starting MetricsCollector");

			var interval = configurationReader.MetricsIntervalInMillisecond;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (interval == 0) return;

			_timer = new Timer(interval);
			_timer.Elapsed += (sender, args) => { CollectAllMetrics(); };
		}

		public void StartCollecting() => _timer?.Start();

		internal void CollectAllMetrics()
		{
			var samples = new List<Sample>();

			foreach (var metricsProvider in MetricsProviders)
			{
				if (metricsProvider.ConsecutiveNumberOfFailedReads < MaxTryWithoutSuccess)
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
							?.LogException(e, "Failed reading {ProviderName} {NumberOfFail} times", metricsProvider.NameInLogs,
								metricsProvider.ConsecutiveNumberOfFailedReads);
					}
				}
				if (metricsProvider.ConsecutiveNumberOfFailedReads != MaxTryWithoutSuccess) continue;

				_logger.Info()
					?.Log("Failed reading {operationName} {numberOfTimes} consecutively - the agent won't try reading {operationName} anymore",
						metricsProvider.NameInLogs, metricsProvider.ConsecutiveNumberOfFailedReads, metricsProvider.NameInLogs);

				metricsProvider.ConsecutiveNumberOfFailedReads++;
			}

			var metricSet = new MetricSet(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000, samples);

			_payloadSender.QueueMetrics(metricSet);
			_logger.Debug()
				?.Log("Metrics collected: {data}",
					samples != null && samples.Count() > 0
						? samples.Select(n => n.ToString()).Aggregate((i, j) => i + ", " + j)
						: "no metrics collected");
		}

		public void Dispose()
		{
			_timer?.Stop();
			_timer?.Dispose();
		}
	}
}
