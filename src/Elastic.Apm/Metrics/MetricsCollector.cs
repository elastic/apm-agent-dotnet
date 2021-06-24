// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

		private readonly IConfigSnapshotProvider _configSnapshotProvider;

		private readonly AgentSpinLock _isCollectionInProgress = new AgentSpinLock();

		private readonly IApmLogger _logger;

		private readonly IPayloadSender _payloadSender;

		private readonly Timer _timer;

		public MetricsCollector(IApmLogger logger, IPayloadSender payloadSender, IConfigSnapshotProvider configSnapshotProvider,
			params IMetricsProvider[] metricsProvider
		)
		{
			_logger = logger.Scoped(nameof(MetricsCollector));
			_payloadSender = payloadSender;
			_configSnapshotProvider = configSnapshotProvider;

			var currentConfigSnapshot = configSnapshotProvider.CurrentSnapshot;

			var interval = currentConfigSnapshot.MetricsIntervalInMilliseconds;

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (interval == 0)
			{
				_logger.Info()?.Log("Collecting metrics is disabled - the agent won't collect metrics");
				return;
			}

			MetricsProviders = new List<IMetricsProvider>();
			var disabledMetrics = configSnapshotProvider.CurrentSnapshot.DisableMetrics;

			if (metricsProvider != null)
			{
				foreach (var item in metricsProvider)
				{
					if (item != null)
						AddIfEnabled(item);
				}
			}

			AddIfEnabled(new ProcessTotalCpuTimeProvider(_logger));
			AddIfEnabled(new SystemTotalCpuProvider(_logger));
			AddIfEnabled(new ProcessWorkingSetAndVirtualMemoryProvider(disabledMetrics));
			AddIfEnabled(new FreeAndTotalMemoryProvider(disabledMetrics));
			try
			{
				// We saw some Exceptions in GcMetricsProvider.ctor, so we try-catch it
				AddIfEnabled(new GcMetricsProvider(_logger, disabledMetrics));
			}
			catch (Exception e)
			{
				_logger.Warning()?.LogException(e, "Failed loading {ProviderName}", nameof(GcMetricsProvider));
			}
			AddIfEnabled(new CgroupMetricsProvider(_logger, disabledMetrics));

			_logger.Info()?.Log("Collecting metrics in {interval} milliseconds interval", interval);
			_timer = new Timer(interval);
			_timer.Elapsed += (sender, args) => { CollectAllMetrics(); };

			void AddIfEnabled(IMetricsProvider provider)
			{
				if (provider.IsEnabled(disabledMetrics))
					MetricsProviders.Add(provider);
			}
		}

		public void StartCollecting() => _timer?.Start();

		internal void CollectAllMetrics()
		{
			using var acq = _isCollectionInProgress.TryAcquireWithDisposable();
			if (!acq.IsAcquired)
			{
				_logger.Trace()?.Log("Previous CollectAllMetrics call is still in progress - skipping this one");
				return;
			}

			if (!_configSnapshotProvider.CurrentSnapshot.Recording)
			{
				//We only handle the Recording=false here. If Enabled=false, then the MetricsCollector is not started at all.
				_logger.Trace()?.Log("Skip collecting metrics - Recording is set to false");
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

		private void CollectAllMetricsImpl()
		{
			_logger.Trace()?.Log("CollectAllMetrics started");

			var samplesFromAllProviders = CollectMetricsFromProviders();
			if (samplesFromAllProviders.IsEmpty())
			{
				_logger.Debug()?.Log("No metrics collected (no provider returned valid samples) - nothing to send to APM Server");
				return;
			}

			try
			{
				foreach (var item in samplesFromAllProviders) _payloadSender.QueueMetrics(item);

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

		private List<IMetricSet> CollectMetricsFromProviders()
		{
			var samples = new List<IMetricSet>();
			// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
			foreach (var metricsProvider in MetricsProviders)
			{
				if (metricsProvider.ConsecutiveNumberOfFailedReads == MaxTryWithoutSuccess)
					continue;

				if (!metricsProvider.IsMetricAlreadyCaptured)
				{
					_logger.Trace()
						?.Log("Skipping {MetricsProviderName} - {propertyName} indicated false", metricsProvider.DbgName,
							nameof(IMetricsProvider.IsMetricAlreadyCaptured));
					continue;
				}

				try
				{
					_logger.Trace()?.Log("Start collecting {MetricsProviderName}", metricsProvider.DbgName);

					var samplesFromProvider = metricsProvider.GetSamples()?.ToArray();

					if (samplesFromProvider != null && samplesFromProvider.Any())
					{
						_logger.Trace()?.Log("Collected {MetricsProviderName} - adding it to MetricSet", metricsProvider.DbgName);

						foreach (var item in samplesFromProvider)
						{
							// filter out NaN and infinity
							item.Samples = item.Samples.Where(x => !double.IsNaN(x.KeyValue.Value) && !double.IsInfinity(x.KeyValue.Value)).ToArray();

							if (item.Samples.Any())
							{
								samples.Add(item);
								metricsProvider.ConsecutiveNumberOfFailedReads = 0;
							}
							else
								ProcessFailedReading(metricsProvider);
						}
					}
					else
						ProcessFailedReading(metricsProvider);
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

			void ProcessFailedReading(IMetricsProvider metricsProvider)
			{
				metricsProvider.ConsecutiveNumberOfFailedReads++;
				_logger.Warning()
					?.Log("Failed reading {MetricsProviderName} {NumberOfFails} times: no valid samples", metricsProvider.DbgName,
						metricsProvider.ConsecutiveNumberOfFailedReads);
			}
		}

		public void Dispose()
		{
			if (_timer != null)
			{
				_timer.Stop();
				_timer.Dispose();
			}

			if (MetricsProviders != null)
			{
				foreach (var provider in MetricsProviders)
				{
					if (provider is IDisposable disposable)
						disposable.Dispose();
				}
			}
		}
	}
}
