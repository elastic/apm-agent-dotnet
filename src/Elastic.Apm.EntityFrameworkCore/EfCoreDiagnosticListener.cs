using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Model;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Elastic.Apm.EntityFrameworkCore
{
	internal class EfCoreDiagnosticListener : IDiagnosticListener
	{
		private readonly IApmAgent _agent;
		private readonly ConcurrentDictionary<Guid, Span> _spans = new ConcurrentDictionary<Guid, Span>();

		public EfCoreDiagnosticListener(IApmAgent agent) => _agent = agent;

		public string Name => "Microsoft.EntityFrameworkCore";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> kv)
		{
			switch (kv.Key)
			{
				case string k when k == RelationalEventId.CommandExecuting.Name && _agent.Tracer.CurrentTransaction != null:
					if (kv.Value is CommandEventData commandEventData)
					{
						var newSpan = DbSpanCommon.StartSpan(_agent, commandEventData.Command);
						_spans.TryAdd(commandEventData.CommandId, newSpan);
					}
					break;
				case string k when k == RelationalEventId.CommandExecuted.Name:
					if (kv.Value is CommandExecutedEventData commandExecutedEventData)
					{
						if (_spans.TryRemove(commandExecutedEventData.CommandId, out var span))
							DbSpanCommon.EndSpan(span, commandExecutedEventData.Command, commandExecutedEventData.Duration);
					}
					break;
				case string k when k == RelationalEventId.CommandError.Name:
					if (kv.Value is CommandErrorEventData commandErrorEventData)
					{
						if (_spans.TryRemove(commandErrorEventData.CommandId, out var span))
						{
							span.CaptureException(commandErrorEventData.Exception);
							DbSpanCommon.EndSpan(span, commandErrorEventData.Command, commandErrorEventData.Duration);
						}
					}
					break;
			}
		}
	}
}
