using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class EventSourceMetricsProvider : IMetricsProvider
	{
		internal static readonly IReadOnlyDictionary<string, string[]> AvailableMetrics = new Dictionary<string, string[]>
		{
			{
				"System.Runtime",
				new[]
				{
					"system.runtime.cpu-usage.pct",
					"system.runtime.working-set.megabytes",
					"system.runtime.gc-heap-size.megabytes",
					"system.runtime.gen-0-gc-count",
					"system.runtime.gen-1-gc-count",
					"system.runtime.gen-2-gc-count",
					"system.runtime.threadpool-thread-count",
					"system.runtime.monitor-lock-contention-count",
					"system.runtime.threadpool-queue-length",
					"system.runtime.threadpool-completed-items-count",
					"system.runtime.alloc-rate.bytes",
					"system.runtime.active-timer-count",
					"system.runtime.gc-fragmentation.pct",
					"system.runtime.exception-count",
					"system.runtime.time-in-gc.pct",
					"system.runtime.gen-0-size.bytes",
					"system.runtime.gen-1-size.bytes",
					"system.runtime.gen-2-size.bytes",
					"system.runtime.loh-size.bytes",
					"system.runtime.poh-size.bytes",
					"system.runtime.assembly-count",
					"system.runtime.il-bytes-jitted.bytes",
					"system.runtime.methods-jitted-count"
				}
			},
			{
				"Microsoft-AspNetCore-Server-Kestrel",
				new[]
				{
					"kestrel.connection-queue-length",
					"kestrel.connections-per-second",
					"kestrel.current-connections",
					"kestrel.current-tls-handshakes",
					"kestrel.current-upgraded-requests",
					"kestrel.failed-tls-handshakes",
					"kestrel.request-queue-length",
					"kestrel.tls-handshakes-per-second",
					"kestrel.total-connections",
					"kestrel.total-tls-handshakes"
				}
			}
		};

		private readonly EventSourceEventListener _eventListener;
		private readonly ConcurrentDictionary<string, double> _currentSamples;
		private readonly object _lock = new object();
		private readonly string[] _metricsToCollect;
		private readonly IApmLogger _logger;

		private volatile bool _isMetricAlreadyCaptured;

		public EventSourceMetricsProvider(
			IApmLogger logger,
			IEnumerable<string> metricsToCollect,
			int metricsIntervalInSeconds)
		{
			_metricsToCollect = (metricsToCollect ?? throw new ArgumentNullException(nameof(metricsToCollect)))
				.Where(x => AvailableMetrics.SelectMany(y => y.Value).Contains(x))
				.Distinct()
				.ToArray();
			var enabledSources = AvailableMetrics
				.Where(x => x.Value.Any(y => _metricsToCollect.Contains(y)))
				.Select(x => x.Key)
				.ToArray();
			_currentSamples = new ConcurrentDictionary<string, double>();
			_eventListener = new EventSourceEventListener(logger, this, metricsIntervalInSeconds, enabledSources);
			_logger = logger.Scoped(nameof(EventSourceMetricsProvider));
		}

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "EventSourceMetricsProvider";

		public bool IsMetricAlreadyCaptured
		{
			get
			{
				lock (_lock) return _isMetricAlreadyCaptured;
			}
		}

		public IEnumerable<MetricSample> GetSamples() => _currentSamples.Select(x => new MetricSample(x.Key, x.Value));

		public void Dispose() => _eventListener?.Dispose();

		private class EventSourceEventListener : EventListener
		{
			private readonly ConcurrentQueue<EventSource> _eventSourcesToEnable = new ConcurrentQueue<EventSource>();
			private ConcurrentBag<EventSource> _enabledEventSources = new ConcurrentBag<EventSource>();

			private readonly IApmLogger _logger;
			private readonly EventSourceMetricsProvider _eventSourceMetricsProvider;
			private readonly string[] _nameOfEnabledSources;
			private readonly double _metricsIntervalInSeconds;

			public EventSourceEventListener(
				IApmLogger logger,
				EventSourceMetricsProvider eventSourceMetricsProvider,
				int metricsIntervalInSeconds,
				string[] nameOfEnabledSources)
			{
				_logger = logger.Scoped(nameof(EventSourceEventListener));
				_eventSourceMetricsProvider = eventSourceMetricsProvider
					?? throw new ArgumentNullException(nameof(eventSourceMetricsProvider));
				_nameOfEnabledSources = nameOfEnabledSources ?? throw new ArgumentNullException(nameof(nameOfEnabledSources));
				_metricsIntervalInSeconds = Math.Max(metricsIntervalInSeconds, 1);
				TryEnablingEventSourceEventSource();
			}

			protected override void OnEventSourceCreated(EventSource eventSource)
			{
				if (AvailableMetrics.Keys.Contains(eventSource.Name))
				{
					_eventSourcesToEnable.Enqueue(eventSource);
					TryEnablingEventSourceEventSource();
				}
			}

			protected override void OnEventWritten(EventWrittenEventArgs eventData)
			{
				if (!_nameOfEnabledSources.Contains(eventData.EventSource.Name))
				{
					return;
				}

				var payload = eventData.Payload?.OfType<IDictionary<string, object>>().FirstOrDefault();
				if (payload == null)
				{
					_logger.Trace()?.Log(
						"Payload for {eventSource} for event {eventName} was not a dictionary.",
						eventData.EventSource.Name,
						eventData.EventName);
					return;
				}

				var counterType = payload.ContainsKey("CounterType") ? payload["CounterType"]?.ToString() : null;
				var valueField = counterType == "Mean" ? "Mean" : counterType == "Sum" ? "Increment" : null;
				var payloadHasNameKey = payload.ContainsKey("Name");
				var payloadHasDisplayUnitsKey = payload.ContainsKey("DisplayUnits");
				var payloadHasValueFieldKey = payload.ContainsKey(valueField);
				if (payloadHasNameKey && payloadHasDisplayUnitsKey && valueField != null && payloadHasValueFieldKey)
				{
					var prefix = eventData.EventSource.Name.Split('-').Last().ToLower();
					var name = $"{prefix}.{payload["Name"].ToString().ToLower()}";

					var displayUnits = payload["DisplayUnits"].ToString();
					if (displayUnits.ToString() == "%")
					{
						name += ".pct";
					}
					else if (displayUnits == "B")
					{
						name += ".bytes";
					}
					else if (displayUnits == "MB")
					{
						name += ".megabytes";
					}

					if (!_eventSourceMetricsProvider._metricsToCollect.Contains(name))
					{
						_logger.Trace()?.Log("Ignoring metric {metric}", name);
						return;
					}

					if (double.TryParse(payload[valueField]?.ToString(), out var value) && !double.IsNaN(value))
					{
						if (!_eventSourceMetricsProvider._currentSamples.ContainsKey(name))
						{
							_eventSourceMetricsProvider._currentSamples.TryAdd(name, value);
						}
						else
						{
							_eventSourceMetricsProvider._currentSamples[name] = value;
						}

						lock (_eventSourceMetricsProvider._lock)
							_eventSourceMetricsProvider._isMetricAlreadyCaptured = true;
					}
					else
					{
						_logger.Trace()?.Log(
							"Could not parse payload value {value} for metric {metric}",
							payload[valueField],
							name);
					}
				}
				else
				{
					_logger.Trace()?.Log("Failed retrieving metric because the following was not satisfied: "
						+ "payloadHasNameKey:{payloadHasNameKey} && "
						+ "payloadHasDisplayUnitsKey:{payloadHasDisplayUnitsKey} && "
						+ "valueField:{valueField} != null && "
						+ "payloadHasValueFieldKey:{payloadHasValueFieldKey}",
						payloadHasNameKey,
						payloadHasDisplayUnitsKey,
						valueField,
						payloadHasValueFieldKey);
				}
			}

			private void TryEnablingEventSourceEventSource()
			{
				if (_nameOfEnabledSources == null)
				{
					// Constructor has not finished initializing properties.
					// This means that base constructor has triggered OnEventSourceCreated.
					return;
				}

				while (_eventSourcesToEnable.TryDequeue(out var eventSource))
				{
					if (_nameOfEnabledSources.Contains(eventSource.Name))
					{
						EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All, new Dictionary<string, string>
						{
							{ "EventCounterIntervalSec", _metricsIntervalInSeconds.ToString() }
						});
						_logger.Trace()?.Log("{eventSource} counters enabled", eventSource.Name);
						_enabledEventSources.Add(eventSource);
					}
				}
			}

			public override void Dispose()
			{
				base.Dispose();

				_logger.Trace()?.Log("disposing {classname}", nameof(EventSourceEventListener));

				try
				{
					var eventSourcesToDisable = _enabledEventSources.ToArray();
					foreach (var eventSource in eventSourcesToDisable)
					{
						DisableEvents(eventSource);
					}

					_enabledEventSources = null;
				}
				catch (Exception e)
				{
					_logger.Warning()?.LogException(e, "Disposing {classname} failed", nameof(EventSourceEventListener));
				}
			}
		}
	}
}
