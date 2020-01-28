using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Parsers;
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
		private const string GcCountName = "clr.gc.count";
		private const string GcGen0SizeName = "clr.gc.gen0size";
		private const string GcGen1SizeName = "clr.gc.gen1size";
		private const string GcGen2SizeName = "clr.gc.gen2size";
		private const string GcGen3SizeName = "clr.gc.gen3size";

		private const string SessionNamePrefix = "EtwSessionForCLRElasticApm_";

		private readonly GcEventListener _eventListener;

		private uint _gcCount;
		private ulong _gen0Size;
		private ulong _gen1Size;
		private ulong _gen2Size;
		private ulong _gen3Size;

		private readonly IApmLogger _logger;

		private readonly TraceEventSession _traceEventSession;

		public GcMetricsProvider(IApmLogger logger)
		{
			_logger = logger.Scoped(nameof(SystemTotalCpuProvider));
			if (PlatformDetection.IsDotNetFullFramework)
			{
				var sessionName = SessionNamePrefix + Guid.NewGuid().ToString();

				using (_traceEventSession = new TraceEventSession(sessionName))
				{
					Task.Run(() =>
					{
						try
						{
							_traceEventSession.EnableProvider(
								ClrTraceEventParser.ProviderGuid,
								TraceEventLevel.Verbose,
								(ulong)
								ClrTraceEventParser.Keywords.GC // garbage collector details
							);
						}
						catch (Exception e)
						{
							_logger.Warning()?.LogException(e, "TraceEventSession initialization failed - GC metrics won't be collected");
							return;
						}

						var source = _traceEventSession.Source;
						source.NeedLoadedDotNetRuntimes();
						source.AddCallbackOnProcessStart((proc) =>
						{
							proc.AddCallbackOnDotNetRuntimeLoad((runtime) =>
							{
								runtime.GCEnd += (process, gc) =>
								{
									_gen0Size = (ulong)gc.HeapStats.GenerationSize0;
									_gen1Size = (ulong)gc.HeapStats.GenerationSize1;
									_gen2Size = (ulong)gc.HeapStats.GenerationSize2;
									_gen3Size = (ulong)gc.HeapStats.GenerationSize3;
									_gcCount = (uint)runtime.GC.GCs.Count;
								};

							});
						});

						_traceEventSession.Source.Process();
					});
				}
			}

			if (PlatformDetection.IsDotNetCore)
				_eventListener = new GcEventListener(this);
		}

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "GcMetricsProvider";

		public IEnumerable<MetricSample> GetSamples()
		{
			if (_gcCount != 0 || _gen0Size != 0 || _gen2Size != 0 || _gen3Size != 0)
			{
				return new List<MetricSample>
				{
					new MetricSample(GcCountName, _gcCount),
					new MetricSample(GcGen0SizeName, _gen0Size),
					new MetricSample(GcGen1SizeName, _gen1Size),
					new MetricSample(GcGen2SizeName, _gen2Size),
					new MetricSample(GcGen3SizeName, _gen3Size)
				};
			}
			_logger.Trace()
				?.Log(
					"Collected gc metrics values: gcCount: {gcCount}, gen0Size: {gen0Size},  gen1Size: {gen1Size}, gen2Size: {gen2Size}, gen1Size: {gen3Size}",
					_gcCount, _gen0Size, _gen2Size, _gen3Size);
			return null;
		}

		public void Dispose()
		{
			_eventListener?.Dispose();
			_traceEventSession?.Dispose();
		}


		/// <summary>
		/// An event listener that collects the GC stats
		/// </summary>
		private class GcEventListener : EventListener
		{
			private static readonly int keywordGC = 1;

			private EventSource _eventSourceDotNet;
			private readonly GcMetricsProvider _gcMetricsProvider;

			public GcEventListener(GcMetricsProvider gcMetricsProvider) => _gcMetricsProvider = gcMetricsProvider;

			protected override void OnEventSourceCreated(EventSource eventSource)
			{
				try
				{
					if (!eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime")) return;

					EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)keywordGC);
					_eventSourceDotNet = eventSource;
				}
				catch(Exception e)
				{
					_gcMetricsProvider?._logger.Warning()?.LogException(e, "EnableEvents failed - no GC metrics will be collected");
				}
			}

			protected override void OnEventWritten(EventWrittenEventArgs eventData)
			{
				// Collect heap sizes
				if (eventData.EventName.Contains("GCHeapStats_V1"))
				{
					SetValue("GenerationSize0", ref _gcMetricsProvider._gen0Size);
					SetValue("GenerationSize1", ref _gcMetricsProvider._gen1Size);
					SetValue("GenerationSize2", ref _gcMetricsProvider._gen2Size);
					SetValue("GenerationSize3", ref _gcMetricsProvider._gen3Size);
				}

				// Collect GC count
				if (eventData.EventName.Contains("GCEnd"))
				{
					var indexOfCount = IndexOf("Count");
					if (indexOfCount < 0) return;

					var gcCount = eventData.Payload[indexOfCount];

					if (!(gcCount is uint gcCountInt)) return;

					_gcMetricsProvider._gcCount = gcCountInt;
				}

				void SetValue(string name, ref ulong value)
				{
					var gen0SizeIndex = IndexOf(name);
					if (gen0SizeIndex < 0) return;

					var gen0Size = eventData.Payload[gen0SizeIndex];
					if (gen0Size is ulong gen0SizeLong)
						value = gen0SizeLong;
				}

				int IndexOf(string name)
					=> eventData.PayloadNames.IndexOf(name);
			}

			public override void Dispose()
			{
				try
				{
					if (_eventSourceDotNet != null)
						DisableEvents(_eventSourceDotNet);
					base.Dispose();
				}
				catch(Exception e)
				{
					_gcMetricsProvider?._logger.Warning()?.LogException(e, "Disposing {classname} failed", nameof(GcEventListener));
				}
			}
		}
	}
}
