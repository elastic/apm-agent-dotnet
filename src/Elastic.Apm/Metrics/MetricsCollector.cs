using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Report;
using Timer = System.Timers.Timer;

namespace Elastic.Apm.Metrics
{
	/// <summary>
	/// Iterates through a list of <see cref="IMetricsProvider" /> and
	/// sends the values through an <see cref="IPayloadSender" /> instance.
	/// It collects the metrics on an interval read from the <see cref="IConfigurationReader" /> which is
	/// passed to the constructor.
	/// In case reading a value from an <see cref="IMetricsProvider" /> fails it retries <see cref="MaxTryWithoutSuccess" />
	/// times,
	/// after that it prints a log and won't retry anymore. This is to avoid endlessly trying to read values without success.
	/// </summary>
	internal class MetricsCollector : IDisposable, IMetricsCollector
	{
		internal const int MaxTryWithoutSuccess = 5;

		private static int _syncPoint;

		/// <summary>
		/// List of all providers that can provide metrics values.
		/// Add new providers to this list in case you want the agent to collect more metrics
		/// </summary>
		internal readonly List<IMetricsProvider> MetricsProviders;

		private readonly IApmLogger _logger;

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

			var interval = configurationReader.MetricsIntervalInMillisecond;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (interval == 0)
			{
				_logger.Info()?.Log("Collecting metrics is disabled - the agent won't collect metrics");
				return;
			}

			_logger.Info()?.Log("Collecting metrics in {interval} milliseconds interval", interval);
			_timer = new Timer(interval);
			_timer.Elapsed += (sender, args) => { CollectAllMetrics(); };
		}

		public void StartCollecting() => _timer?.Start();

		internal void CollectAllMetrics()
		{
			var sync = Interlocked.CompareExchange(ref _syncPoint, 1, 0);
			if (sync != 0) return;

			try
			{
				var samplesFromAllProviders = new List<MetricSample>();

				foreach (var metricsProvider in MetricsProviders)
				{
					if (metricsProvider.ConsecutiveNumberOfFailedReads == MaxTryWithoutSuccess)
						continue;

					try
					{
						var samplesFromCurrentProvider = metricsProvider.GetSamples();
						if (samplesFromCurrentProvider != null)
						{
							var sampleArray = samplesFromCurrentProvider as MetricSample[] ?? samplesFromCurrentProvider.ToArray();
							if (sampleArray.Any())
								samplesFromAllProviders.AddRange(sampleArray);

							metricsProvider.ConsecutiveNumberOfFailedReads = 0;
						}
						else
							metricsProvider.ConsecutiveNumberOfFailedReads++;
					}
					catch (Exception e)
					{
						metricsProvider.ConsecutiveNumberOfFailedReads++;
						_logger.Error()
							?.LogException(e, "Failed reading {ProviderName} {NumberOfFail} times", metricsProvider.DbgName,
								metricsProvider.ConsecutiveNumberOfFailedReads);
					}

					if (metricsProvider.ConsecutiveNumberOfFailedReads != MaxTryWithoutSuccess) continue;

					_logger.Info()
						?.Log("Failed reading {operationName} {numberOfTimes} consecutively - the agent won't try reading {operationName} anymore",
							metricsProvider.DbgName, metricsProvider.ConsecutiveNumberOfFailedReads, metricsProvider.DbgName);
				}

				var metricSet = new MetricSet(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000, samplesFromAllProviders);

				try
				{
					_payloadSender.QueueMetrics(metricSet);
					_logger.Debug()
						?.Log("Metrics collected: {data}",
							samplesFromAllProviders.Any()
								? samplesFromAllProviders.Select(n => n.ToString()).Aggregate((i, j) => i + ", " + j)
								: "no metrics collected");
				}
				catch (Exception e)
				{
					_logger.Error()
						?.LogException(e, "Failed sending metrics through PayloadSender - metrics collection stops");
					_timer.Stop();
					_timer.Dispose();
				}
			}
			catch (Exception e)
			{
				_logger.Error()
					?.LogExceptionWithCaller(e);
			}
			finally
			{
				_syncPoint = 0;
			}
		}

		public void Dispose()
		{
			_timer?.Stop();
			_timer?.Dispose();
		}
	}
}
