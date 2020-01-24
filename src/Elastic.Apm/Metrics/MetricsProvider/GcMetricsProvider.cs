using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	/// <summary>
	/// A metrics provider that collects GC metrics through EventSource
	/// </summary>
	internal class GcMetricsProvider : IMetricsProvider, IDisposable
	{
		private const string GcCountName = "clr.gc.count";
		private const string GcGen0SizeName = "clr.gc.gen0size";
		private const string GcGen1SizeName = "clr.gc.gen1size";
		private const string GcGen2SizeName = "clr.gc.gen2size";
		private const string GcGen3SizeName = "clr.gc.gen3size";

		private readonly GcEventListener _eventListener = new GcEventListener();

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "GcMetricsProvider";

		public IEnumerable<MetricSample> GetSamples()
		{
			if (_eventListener.GcCount != 0 || _eventListener.Gen0Size != 0 || _eventListener.Gen2Size != 0 || _eventListener.Gen3Size != 0)
				return null;

			return new List<MetricSample>
			{
				new MetricSample(GcCountName, _eventListener.GcCount),
				new MetricSample(GcGen0SizeName, _eventListener.Gen0Size),
				new MetricSample(GcGen1SizeName, _eventListener.Gen1Size),
				new MetricSample(GcGen2SizeName, _eventListener.Gen2Size),
				new MetricSample(GcGen3SizeName, _eventListener.Gen3Size)
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

			internal uint GcCount;
			internal ulong Gen0Size;
			internal ulong Gen1Size;
			internal ulong Gen2Size;
			internal ulong Gen3Size;

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
					SetValue("GenerationSize0", ref Gen0Size);
					SetValue("GenerationSize1", ref Gen1Size);
					SetValue("GenerationSize2", ref Gen2Size);
					SetValue("GenerationSize3", ref Gen3Size);
				}

				// Collect GC count
				if (eventData.EventName.Contains("GCEnd"))
				{
					var indexOfCount = IndexOf("Count");
					if (indexOfCount < 0) return;

					var gcCount = eventData.Payload[indexOfCount];

					if (!(gcCount is uint gcCountInt)) return;

					GcCount = gcCountInt;
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
				{
					return eventData.PayloadNames.IndexOf(name);
				}
			}

			public override void Dispose()
			{
				DisableEvents(_eventSourceDotNet);
				base.Dispose();
			}
		}
	}
}
