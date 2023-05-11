// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

// ReSharper disable AccessToDisposedClosure

namespace Elastic.Apm.Metrics.MetricsProvider
{
	/// <summary>
	/// A metrics provider that collects GC metrics.
	/// On .NET Core it collects metrics through EventSource,
	/// </summary>
	internal class GcMetricsProvider : IMetricsProvider, IDisposable
	{
		internal const string GcCountName = "clr.gc.count";
		internal const string GcGen0SizeName = "clr.gc.gen0size";
		internal const string GcGen1SizeName = "clr.gc.gen1size";
		internal const string GcGen2SizeName = "clr.gc.gen2size";
		internal const string GcGen3SizeName = "clr.gc.gen3size";
		internal const string GcTimeName = "clr.gc.time";

		private readonly bool _collectGcCount;
		private readonly bool _collectGcGen0Size;
		private readonly bool _collectGcGen1Size;
		private readonly bool _collectGcGen2Size;
		private readonly bool _collectGcGen3Size;
		private readonly bool _collectGcTime;

		private readonly GcEventListener _eventListener;
		private readonly object _lock = new object();
		private readonly IApmLogger _logger;

		private uint _gcCount;
		private long _gcTimeInTicks;
		private ulong _gen0Size;
		private ulong _gen1Size;
		private ulong _gen2Size;
		private ulong _gen3Size;

		private volatile bool _isMetricAlreadyCaptured;
		private readonly bool _isEnabled;

		public GcMetricsProvider(IApmLogger logger, IReadOnlyList<WildcardMatcher> disabledMetrics)
		{
			_collectGcCount = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcCountName);
			_collectGcTime = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcTimeName);
			_collectGcGen0Size = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcGen0SizeName);
			_collectGcGen1Size = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcGen1SizeName);
			_collectGcGen2Size = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcGen2SizeName);
			_collectGcGen3Size = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcGen3SizeName);
			_isEnabled = _collectGcCount || _collectGcTime || _collectGcGen0Size || _collectGcGen1Size || _collectGcGen2Size || _collectGcGen3Size;
			if (!IsEnabled(disabledMetrics)) return;

			_logger = logger.Scoped(DbgName);
			if (!PlatformDetection.IsDotNetCore && !PlatformDetection.IsDotNet)
			{
				_logger.Info()?.Log("GC metrics are only available on .NET Core, disabling metric collection");
				_isEnabled = false;
				return;
			}

			_eventListener = new GcEventListener(this, logger);
		}

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => nameof(GcMetricsProvider);

		public bool IsMetricAlreadyCaptured
		{
			get
			{
				lock (_lock)
					return _isMetricAlreadyCaptured;
			}
		}

		public bool IsEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) => _isEnabled;

		public IEnumerable<MetricSet> GetSamples()
		{
			var gcTimeInMs = Interlocked.Exchange(ref _gcTimeInTicks, 0) / 10_000.0;
			if (_gcCount != 0 || _gen0Size != 0 || _gen2Size != 0 || _gen3Size != 0 || gcTimeInMs > 0)
			{
				var samples = new List<MetricSample>(6);
				if (_collectGcCount)
					samples.Add(new MetricSample(GcCountName, _gcCount));
				if (_collectGcTime)
					samples.Add(new MetricSample(GcTimeName, Math.Round(gcTimeInMs, 6)));
				if (_collectGcGen0Size)
					samples.Add(new MetricSample(GcGen0SizeName, _gen0Size));
				if (_collectGcGen1Size)
					samples.Add(new MetricSample(GcGen1SizeName, _gen1Size));
				if (_collectGcGen2Size)
					samples.Add(new MetricSample(GcGen2SizeName, _gen2Size));
				if (_collectGcGen3Size)
					samples.Add(new MetricSample(GcGen3SizeName, _gen3Size));

				_logger.Trace()
					?.Log(
						"Collected gc metrics values: gcCount: {gcCount}, gen0Size: {gen0Size},  gen1Size: {gen1Size}, gen2Size: {gen2Size}, gen1Size: {gen3Size}, gcTime: {gcTime}",
						_gcCount, _gen0Size, _gen1Size, _gen2Size, _gen3Size, gcTimeInMs);

				return new List<MetricSet> { new(TimestampUtils.TimestampNow(), samples) };
			}

			return null;
		}

		public void Dispose() => _eventListener?.Dispose();

		/// <summary>
		/// An event listener that collects the GC stats
		/// </summary>
		private class GcEventListener : EventListener
		{
			private static readonly int keywordGC = 1;
			private readonly GcMetricsProvider _gcMetricsProvider;
			private readonly IApmLogger _logger;

			public GcEventListener(GcMetricsProvider gcMetricsProvider, IApmLogger logger)
			{
				_gcMetricsProvider = gcMetricsProvider ?? throw new Exception("gcMetricsProvider is null");

				_logger = logger.Scoped(nameof(GcEventListener));
				_logger.Trace()?.Log("Initialize GcEventListener to collect GC metrics");
			}

			private EventSource _eventSourceDotNet;
			private long _gcStartTime;

			protected override void OnEventSourceCreated(EventSource eventSource)
			{
				try
				{
					if (!eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime"))
						return;

					EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)keywordGC);
					_eventSourceDotNet = eventSource;
					_logger?.Trace()?.Log("Microsoft-Windows-DotNETRuntime enabled");
				}
				catch (Exception e)
				{
					_logger?.Warning()?.LogException(e, "EnableEvents failed - no GC metrics will be collected");
				}
			}

			protected override void OnEventWritten(EventWrittenEventArgs eventData)
			{
				// Collect heap sizes
				if (eventData.EventName.Contains("GCHeapStats_V1"))
				{
					_logger?.Trace()?.Log("OnEventWritten with GCHeapStats_V1");

					SetValue("GenerationSize0", ref _gcMetricsProvider._gen0Size);
					SetValue("GenerationSize1", ref _gcMetricsProvider._gen1Size);
					SetValue("GenerationSize2", ref _gcMetricsProvider._gen2Size);
					SetValue("GenerationSize3", ref _gcMetricsProvider._gen3Size);

					if (!_gcMetricsProvider._isMetricAlreadyCaptured)
					{
						lock (_gcMetricsProvider._lock)
							_gcMetricsProvider._isMetricAlreadyCaptured = true;
					}
				}

				if (eventData.EventName.Contains("GCHeapStats_V2"))
				{
					_logger?.Trace()?.Log("OnEventWritten with GCHeapStats_V2");

					SetValue("GenerationSize0", ref _gcMetricsProvider._gen0Size);
					SetValue("GenerationSize1", ref _gcMetricsProvider._gen1Size);
					SetValue("GenerationSize2", ref _gcMetricsProvider._gen2Size);
					SetValue("GenerationSize3", ref _gcMetricsProvider._gen3Size);

					if (!_gcMetricsProvider._isMetricAlreadyCaptured)
					{
						lock (_gcMetricsProvider._lock)
							_gcMetricsProvider._isMetricAlreadyCaptured = true;
					}
				}

				if (eventData.EventName.Contains("GCStart"))
					Interlocked.Exchange(ref _gcStartTime, DateTime.UtcNow.Ticks);

				// Collect GC count and time
				if (eventData.EventName.Contains("GCEnd"))
				{
					if (!_gcMetricsProvider._isMetricAlreadyCaptured)
					{
						lock (_gcMetricsProvider._lock)
							_gcMetricsProvider._isMetricAlreadyCaptured = true;
					}

					_logger?.Trace()?.Log("OnEventWritten with GCEnd");

					var durationInTicks = DateTime.UtcNow.Ticks - Interlocked.Read(ref _gcStartTime);
					Interlocked.Exchange(ref _gcMetricsProvider._gcTimeInTicks,
						Interlocked.Read(ref _gcMetricsProvider._gcTimeInTicks) + durationInTicks);

					var indexOfCount = IndexOf("Count");
					if (indexOfCount < 0)
						return;

					var gcCount = eventData.Payload[indexOfCount];

					if (!(gcCount is uint gcCountInt))
						return;

					_gcMetricsProvider._gcCount = gcCountInt;
				}

				void SetValue(string name, ref ulong value)
				{
					var gen0SizeIndex = IndexOf(name);
					if (gen0SizeIndex < 0)
						return;

					var gen0Size = eventData.Payload[gen0SizeIndex];
					if (gen0Size is ulong gen0SizeLong)
						value = gen0SizeLong;
				}

				int IndexOf(string name)
				{
					return eventData.PayloadNames.IndexOf(name);
				}
			}

			public override void Dispose()
			{
				try
				{
					if (_eventSourceDotNet != null)
					{
						_logger.Trace()?.Log("disposing {classname}", nameof(GcEventListener));
						DisableEvents(_eventSourceDotNet);
						_eventSourceDotNet = null;
						// calling _eventSourceDotNet.Dispose makes it impossible to re-enable the eventsource, so if we call _eventSourceDotNet.Dispose()
						// all tests will fail after Dispose()
					}
				}
				catch (Exception e)
				{
					_logger.Warning()?.LogException(e, "Disposing {classname} failed", nameof(GcEventListener));
				}
			}
		}
	}
}
