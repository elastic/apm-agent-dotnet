using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class GcMetricsProvider : IMetricsProvider //TODO: Make disposable
	{
		private SimpleEventListener _eventListener;
		public GcMetricsProvider() => _eventListener = new SimpleEventListener();

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName => "GcMetricsProvider";

		public IEnumerable<MetricSample> GetSamples()
		{
			var a = 0;
			return new List<MetricSample>();
		}
	}

	internal class SimpleEventListener : EventListener
	{
		private ulong _countTotalEvents = 0;
		private static int keyword = 1; //TODO

		private long _timeGcStart = 0;
		private EventSource _eventSourceDotNet;

		public SimpleEventListener() =>
			Console.WriteLine("SimpleEventListener ctor");

		// Called whenever an EventSource is created.
		protected override void OnEventSourceCreated(EventSource eventSource)
		{
			Console.WriteLine(eventSource.Name);
			if (!eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime")) return;

			EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)keyword);
			_eventSourceDotNet = eventSource;
		}
		// Called whenever an event is written.
		protected override void OnEventWritten(EventWrittenEventArgs eventData)
		{
			Console.WriteLine("OnEventWritten :" + eventData.EventName);
			// Write the contents of the event to the console.
			if (eventData.EventName.Contains("GCStart"))
			{

				//_timeGcStart = eventData.TimeStamp.Ticks;
			}
			else if (eventData.EventName.Contains("GCEnd"))
			{
				//long timeGCEnd = eventData.TimeStamp.Ticks;
			//	long gcIndex = long.Parse(eventData.Payload[0].ToString());
				// Console.WriteLine("GC#{0} took {1:f3}ms",
				// 	gcIndex, (double) (timeGCEnd - _timeGcStart)/10.0/1000.0);
				//
				// if (gcIndex >= 5)
				// 	DisableEvents(_eventSourceDotNet);
			}

			_countTotalEvents++;
		}
	}
}
