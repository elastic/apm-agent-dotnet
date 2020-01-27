using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

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

		private readonly GcEventListener _eventListener;

		private uint GcCount;
		private ulong Gen0Size;
		private ulong Gen1Size;
		private ulong Gen2Size;
		private ulong Gen3Size;

		public GcMetricsProvider()
		{
			if (PlatformDetection.IsDotNetFullFramework)
			{
				var sessionName = "EtwSessionForCLRElasticApm_" + Guid.NewGuid().ToString();
				
				using (var userSession = new TraceEventSession(sessionName, TraceEventSessionOptions.Create))
				{
					Task.Run(() =>
					{
						userSession.EnableProvider(
							ClrTraceEventParser.ProviderGuid,
							TraceEventLevel.Verbose,
							(ulong)
							ClrTraceEventParser.Keywords.GC // garbage collector details
						);

						var source = userSession.Source;
						source.NeedLoadedDotNetRuntimes();
						source.AddCallbackOnProcessStart((TraceProcess proc) =>
						{
							proc.AddCallbackOnDotNetRuntimeLoad((TraceLoadedDotNetRuntime runtime) =>
							{
								runtime.GCStart += (TraceProcess p, TraceGC gc) =>
								{

								};
								runtime.GCEnd += (TraceProcess p, TraceGC gc) =>
								{
									Gen0Size = (ulong)gc.HeapStats.GenerationSize0;
									Gen1Size = (ulong)gc.HeapStats.GenerationSize1;
									Gen2Size = (ulong)gc.HeapStats.GenerationSize2;
									Gen3Size = (ulong)gc.HeapStats.GenerationSize3;
									GcCount = (uint)gc.Number;
								};
							});
						});

						userSession.Source.Process();
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
			if (GcCount == 0 && Gen0Size == 0 && Gen2Size == 0 && Gen3Size == 0)
				return null;

			return new List<MetricSample>
			{
				new MetricSample(GcCountName, GcCount),
				new MetricSample(GcGen0SizeName, Gen0Size),
				new MetricSample(GcGen1SizeName, Gen1Size),
				new MetricSample(GcGen2SizeName, Gen2Size),
				new MetricSample(GcGen3SizeName, Gen3Size)
			};
		}

		public void Dispose() => _eventListener?.Dispose();

		/// <summary>
		/// An event listener that collects the GC stats
		/// </summary>
		private class GcEventListener : EventListener
		{
			private static readonly int keywordGC = 1; //TODO

			private EventSource _eventSourceDotNet;
			private readonly GcMetricsProvider _gcMetricsProvider;

			public GcEventListener(GcMetricsProvider gcMetricsProvider) => _gcMetricsProvider = gcMetricsProvider;

			protected override void OnEventSourceCreated(EventSource eventSource)
			{
				if (!eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime")) return;

				EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)keywordGC);
				_eventSourceDotNet = eventSource;
			}

			protected override void OnEventWritten(EventWrittenEventArgs eventData)
			{
				// Collect heap sizes
				if (eventData.EventName.Contains("GCHeapStats_V1"))
				{
					SetValue("GenerationSize0", ref _gcMetricsProvider.Gen0Size);
					SetValue("GenerationSize1", ref _gcMetricsProvider.Gen1Size);
					SetValue("GenerationSize2", ref _gcMetricsProvider.Gen2Size);
					SetValue("GenerationSize3", ref _gcMetricsProvider.Gen3Size);
				}

				// Collect GC count
				if (eventData.EventName.Contains("GCEnd"))
				{
					var indexOfCount = IndexOf("Count");
					if (indexOfCount < 0) return;

					var gcCount = eventData.Payload[indexOfCount];

					if (!(gcCount is uint gcCountInt)) return;

					_gcMetricsProvider.GcCount = gcCountInt;
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
				DisableEvents(_eventSourceDotNet);
				base.Dispose();
			}
		}
	}
}
