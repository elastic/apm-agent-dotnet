using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class GcMetricsProvider : IMetricsProvider //TODO: Make disposable
	{
		private const string GcCount = "clr.gc.count";
		private readonly SimpleEventListener _eventListener;

		private static readonly object Lock = new object();

		public GcMetricsProvider() => _eventListener = new SimpleEventListener();

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "GcMetricsProvider";

		public IEnumerable<MetricSample> GetSamples()
		{
			if (_eventListener.Samples == null)
				return null;

			var retVal = new MetricSample[_eventListener.Samples.Count];
			lock (Lock)
			{
				_eventListener.Samples.CopyTo(retVal, 0);
				_eventListener.Samples.Clear();
			}

			return retVal;
		}

		private class SimpleEventListener : EventListener
		{
			private static readonly int keyword = 1; //TODO

			internal List<MetricSample> Samples = new List<MetricSample>();

			public SimpleEventListener() =>
				Console.WriteLine("SimpleEventListener ctor");

			protected override void OnEventSourceCreated(EventSource eventSource)
			{
				Console.WriteLine(eventSource.Name);
				if (!eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime")) return;

				EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)keyword);
			}

			protected override void OnEventWritten(EventWrittenEventArgs eventData)
			{
				Console.WriteLine("OnEventWritten :" + eventData.EventName);

				if (eventData.EventName.Contains("GCStart")) { } //TODO: remove

				else if (eventData.EventName.Contains("GCEnd"))
				{
					var indexOfCount = eventData.PayloadNames.IndexOf("Count");
					if (indexOfCount < 0) return;

					var gcCount = eventData.Payload[indexOfCount];

					if (!(gcCount is uint gcCountInt)) return;

					lock (Lock) Samples.Add(new MetricSample(GcCount, gcCountInt));
				}
			}
		}
	}
}
