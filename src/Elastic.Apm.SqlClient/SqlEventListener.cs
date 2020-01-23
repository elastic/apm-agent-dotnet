using System.Diagnostics.Tracing;
using Elastic.Apm.Logging;

namespace Elastic.Apm.SqlClient
{
	internal class SqlEventListener : EventListener
	{
		private readonly IApmAgent _apmAgent;
		private readonly IApmLogger _logger;

		public SqlEventListener(IApmAgent apmAgent)
		{
			_apmAgent = apmAgent;
			_logger = _apmAgent.Logger.Scoped(nameof(SqlEventListener));
		}

		protected override void OnEventSourceCreated(EventSource eventSource)
		{
			if (eventSource != null && eventSource.Name == "Microsoft-AdoNet-SystemData")
			{
				EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)1);
			}

			base.OnEventSourceCreated(eventSource);
		}

		protected override void OnEventWritten(EventWrittenEventArgs eventData)
		{
			if (eventData?.Payload == null) return;
			
		}
	}
}
