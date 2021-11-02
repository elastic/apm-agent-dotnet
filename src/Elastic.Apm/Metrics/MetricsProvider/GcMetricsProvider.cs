// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;

// ReSharper disable AccessToDisposedClosure

namespace Elastic.Apm.Metrics.MetricsProvider
{
	/// <summary>
	/// A metrics provider that collects GC metrics.
	/// On .NET Core it collects metrics through EventSource,
	/// on Full Framework Microsoft.Diagnostics.Tracing.TraceEvent is used.
	/// </summary>
	internal class GcMetricsProvider : IMetricsProvider, IDisposable
	{
		internal const string GcCountName = "clr.gc.count";
		internal const string GcGen0SizeName = "clr.gc.gen0size";
		internal const string GcGen1SizeName = "clr.gc.gen1size";
		internal const string GcGen2SizeName = "clr.gc.gen2size";
		internal const string GcGen3SizeName = "clr.gc.gen3size";
		internal const string GcTimeName = "clr.gc.time";
		private const string SessionNamePrefix = "EtwSessionForCLRElasticApm_";

		private readonly bool _collectGcCount;
		private readonly bool _collectGcGen0Size;
		private readonly bool _collectGcGen1Size;
		private readonly bool _collectGcGen2Size;
		private readonly bool _collectGcGen3Size;
		private readonly bool _collectGcTime;

		private readonly GcEventListener _eventListener;
		private readonly object _lock = new object();
		private readonly IApmLogger _logger;
		private readonly TraceEventSession _traceEventSession;
		private readonly Task _traceEventSessionTask;

		private uint _gcCount;
		private long _gcTimeInTicks;
		private ulong _gen0Size;
		private ulong _gen1Size;
		private ulong _gen2Size;
		private ulong _gen3Size;

		private volatile bool _isMetricAlreadyCaptured;

		public GcMetricsProvider(IApmLogger logger, IReadOnlyList<WildcardMatcher> disabledMetrics)
		{
			_collectGcCount = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcCountName);
			_collectGcTime = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcTimeName);
			_collectGcGen0Size = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcGen0SizeName);
			_collectGcGen1Size = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcGen1SizeName);
			_collectGcGen2Size = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcGen2SizeName);
			_collectGcGen3Size = !WildcardMatcher.IsAnyMatch(disabledMetrics, GcGen3SizeName);

			if (!IsEnabled(disabledMetrics))
				return;

			_logger = logger.Scoped(DbgName);

			if (PlatformDetection.IsDotNetFullFramework)
			{
				// if the OS doesn't support filtering, the processes and runtimes tracked by the trace event session source can grow
				// unbounded, leading to the application consuming ever more memory.
				if (!TraceEventProviderOptions.FilteringSupported)
				{
					_logger.Info()?.Log("TraceEventSession does not support filtering on this operating system. GC metrics won't be collected");
					return;
				}

				try
				{
					TraceEventSessionName = SessionNamePrefix + Guid.NewGuid();
					_traceEventSession = new TraceEventSession(TraceEventSessionName);
					_traceEventSession.Source.NeedLoadedDotNetRuntimes();
					_traceEventSession.EnableProvider(
						ClrTraceEventParser.ProviderGuid,
						TraceEventLevel.Informational,
						(ulong)ClrTraceEventParser.Keywords.GC, // garbage collector details
						new TraceEventProviderOptions { ProcessIDFilter = new List<int>(1) { Process.GetCurrentProcess().Id } }
					);

					_traceEventSession.Source.Clr.GCHeapStats += ClrOnGCHeapStats;
					_traceEventSession.Source.AddCallbackOnProcessStart(process =>
					{
						process.AddCallbackOnDotNetRuntimeLoad(runtime =>
						{
							runtime.GCEnd += RuntimeGCEnd;
						});
					});

					_traceEventSessionTask = Task.Run(() => _traceEventSession.Source.Process());
				}
				catch (Exception e)
				{
					_logger.Warning()?.LogException(e, "TraceEventSession initialization failed - GC metrics won't be collected");
					return;
				}
			}

			if (PlatformDetection.IsDotNetCore || PlatformDetection.IsDotNet5)
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

		/// <summary>
		/// The name of the TraceEventSession when using <see cref="TraceEventSession" />
		/// to capture metrics, otherwise null.
		/// </summary>
		internal string TraceEventSessionName { get; }

		public bool IsEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) =>
			_collectGcCount || _collectGcTime || _collectGcGen0Size || _collectGcGen1Size || _collectGcGen2Size || _collectGcGen3Size;

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

				return new List<MetricSet> { new(TimeUtils.TimestampNow(), samples) };
			}

			return null;
		}

		public void Dispose()
		{
			_eventListener?.Dispose();
			if (_traceEventSession != null)
			{
				_traceEventSession.Source.Clr.GCHeapStats -= ClrOnGCHeapStats;
				_traceEventSession.Stop(true);
				_traceEventSession.Source.Dispose();
				_traceEventSession.Dispose();

				if (_traceEventSessionTask != null
					&& (_traceEventSessionTask.IsCompleted || _traceEventSessionTask.IsFaulted || _traceEventSessionTask.IsCanceled))
					_traceEventSessionTask.Dispose();
			}
		}

		private void ClrOnGCHeapStats(GCHeapStatsTraceData a)
		{
			if (!_isMetricAlreadyCaptured)
			{
				lock (_lock)
					_isMetricAlreadyCaptured = true;
			}
			_gen0Size = (ulong)a.GenerationSize0;
			_gen1Size = (ulong)a.GenerationSize1;
			_gen2Size = (ulong)a.GenerationSize2;
			_gen3Size = (ulong)a.GenerationSize3;
		}

		private void RuntimeGCEnd(TraceProcess _, TraceGC gc)
		{
			if (!_isMetricAlreadyCaptured)
			{
				lock (_lock)
					_isMetricAlreadyCaptured = true;
			}
			_gcTimeInTicks = (long)gc.DurationMSec * 10_000;
			_gcCount = (uint)gc.Number;
		}

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
