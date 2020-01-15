using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Report;

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

		/// <summary>
		/// List of all providers that can provide metrics values.
		/// Add new providers to this list in case you want the agent to collect more metrics
		/// </summary>
		internal readonly List<IMetricsProvider> MetricsProviders;

		private readonly AgentSpinLock _isCollectionInProgress = new AgentSpinLock();

		private readonly IApmLogger _logger;

		private readonly IPayloadSender _payloadSender;

		private readonly Timer _timer;

		public MetricsCollector(IApmLogger logger, IPayloadSender payloadSender, IConfigurationReader configurationReader)
		{
			_logger = logger.Scoped(nameof(MetricsCollector));
			_payloadSender = payloadSender;

			var interval = configurationReader.MetricsIntervalInMilliseconds;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (interval == 0)
			{
				_logger.Info()?.Log("Collecting metrics is disabled - the agent won't collect metrics");
				return;
			}

			MetricsProviders = new List<IMetricsProvider>
			{
				new FreeAndTotalMemoryProvider(),
				new ProcessWorkingSetAndVirtualMemoryProvider(),
				new SystemTotalCpuProvider(_logger),
				new ProcessTotalCpuTimeProvider(_logger),
				new GcMetricsProvider()
			};

			_logger.Info()?.Log("Collecting metrics in {interval} milliseconds interval", interval);
			_timer = new Timer(interval);
			_timer.Elapsed += (sender, args) => { CollectAllMetrics(); };
		}

		public void StartCollecting() => _timer?.Start();

		internal void CollectAllMetrics()
		{
			using (var acq = _isCollectionInProgress.TryAcquireWithDisposable())
			{
				if (!acq.IsAcquired)
				{
					_logger.Trace()?.Log("Previous CollectAllMetrics call is still in progress - skipping this one");
					return;
				}

				try
				{
					CollectAllMetricsImpl();
				}
				catch (Exception e)
				{
					_logger.Error()
						?.LogExceptionWithCaller(e);
				}
			}
		}

		internal void CollectAllMetricsImpl()
		{
			_logger.Trace()?.Log("CollectAllMetrics started");

			var samplesFromAllProviders = CollectMetricsFromProviders();
			if (samplesFromAllProviders.IsEmpty())
			{
				_logger.Debug()?.Log("No metrics collected (no provider returned valid samples) - nothing to send to APM Server");
				return;
			}

			var metricSet = new MetricSet(TimeUtils.TimestampNow(), samplesFromAllProviders);

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

		private List<MetricSample> CollectMetricsFromProviders()
		{
			var samples = new List<MetricSample>();
			// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
			foreach (var metricsProvider in MetricsProviders)
			{
				if (metricsProvider.ConsecutiveNumberOfFailedReads == MaxTryWithoutSuccess)
					continue;

				try
				{
					_logger.Trace()?.Log("Start collecting {MetricsProviderName}", metricsProvider.DbgName);

					var samplesFromProvider = metricsProvider.GetSamples()
						?.Where(x => !double.IsNaN(x.KeyValue.Value) && !double.IsInfinity(x.KeyValue.Value))
						.ToArray();

					if (samplesFromProvider != null && samplesFromProvider.Length > 0)
					{
						_logger.Trace()?.Log("Collected {MetricsProviderName} - adding it to MetricSet", metricsProvider.DbgName);
						samples.AddRange(samplesFromProvider);
						metricsProvider.ConsecutiveNumberOfFailedReads = 0;
					}
					else
					{
						metricsProvider.ConsecutiveNumberOfFailedReads++;
						_logger.Warning()
							?.Log("Failed reading {MetricsProviderName} {NumberOfFails} times: no valid samples", metricsProvider.DbgName,
								metricsProvider.ConsecutiveNumberOfFailedReads);
					}
				}
				catch (Exception e)
				{
					metricsProvider.ConsecutiveNumberOfFailedReads++;
					_logger.Error()
						?.LogException(e, "Failed reading {MetricsProviderName} {NumberOfFails} times", metricsProvider.DbgName,
							metricsProvider.ConsecutiveNumberOfFailedReads);
				}

				if (metricsProvider.ConsecutiveNumberOfFailedReads != MaxTryWithoutSuccess) continue;

				_logger.Info()
					?.Log("Failed reading {operationName} {numberOfTimes} consecutively - the agent won't try reading {operationName} anymore",
						metricsProvider.DbgName, metricsProvider.ConsecutiveNumberOfFailedReads, metricsProvider.DbgName);
			}

			return samples;
		}

		public void Dispose()
		{
			if (MetricsProviders == null) return;

			_timer?.Stop();
			_timer?.Dispose();

			foreach (var provider in MetricsProviders)
			{
				if (provider is IDisposable disposable)
					disposable.Dispose();
			}
		}
	}
}
