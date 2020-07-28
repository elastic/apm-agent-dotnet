// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Model;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Elastic.Apm.EntityFrameworkCore
{
	internal class EfCoreDiagnosticListener : IDiagnosticListener
	{
		private readonly ApmAgent _agent;
		private readonly ConcurrentDictionary<Guid, Span> _spans = new ConcurrentDictionary<Guid, Span>();

		public EfCoreDiagnosticListener(IApmAgent agent) => _agent = (ApmAgent)agent;

		public string Name => "Microsoft.EntityFrameworkCore";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> kv)
		{
			// check for competing instrumentation
			if (_agent.TracerInternal.CurrentSpan is Span currentSpan)
			{
				if (currentSpan.InstrumentationFlag == InstrumentationFlag.SqlClient)
					return;
			}

			switch (kv.Key)
			{
				case { } k when k == RelationalEventId.CommandExecuting.Name && _agent.Tracer.CurrentTransaction != null:
					if (kv.Value is CommandEventData commandEventData)
					{
						var newSpan = _agent.TracerInternal.DbSpanCommon.StartSpan(_agent, commandEventData.Command, InstrumentationFlag.EfCore);
						_spans.TryAdd(commandEventData.CommandId, newSpan);
					}
					break;
				case { } k when k == RelationalEventId.CommandExecuted.Name:
					if (kv.Value is CommandExecutedEventData commandExecutedEventData)
					{
						if (_spans.TryRemove(commandExecutedEventData.CommandId, out var span))
							_agent.TracerInternal.DbSpanCommon.EndSpan(span, commandExecutedEventData.Command, commandExecutedEventData.Duration);
					}
					break;
				case { } k when k == RelationalEventId.CommandError.Name:
					if (kv.Value is CommandErrorEventData commandErrorEventData)
					{
						if (_spans.TryRemove(commandErrorEventData.CommandId, out var span))
						{
							span.CaptureException(commandErrorEventData.Exception);
							_agent.TracerInternal.DbSpanCommon.EndSpan(span, commandErrorEventData.Command, commandErrorEventData.Duration);
						}
					}
					break;
			}
		}
	}
}
