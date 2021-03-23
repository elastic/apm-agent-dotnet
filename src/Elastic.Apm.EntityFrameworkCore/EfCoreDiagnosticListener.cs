// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Model;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Elastic.Apm.EntityFrameworkCore
{
	internal class EfCoreDiagnosticListener : DiagnosticListenerBase
	{
		private readonly ConcurrentDictionary<Guid, ISpan> _spans = new ConcurrentDictionary<Guid, ISpan>();
		private ApmAgent _agent;

		public EfCoreDiagnosticListener(IApmAgent agent) : base(agent) => _agent = agent as ApmAgent;

		public override string Name => "Microsoft.EntityFrameworkCore";

		protected override void HandleOnNext(KeyValuePair<string, object> kv)
		{
			// check for competing instrumentation
			if (_agent?.TracerInternal.CurrentSpan is Span currentSpan)
			{
				if (currentSpan.InstrumentationFlag == InstrumentationFlag.SqlClient)
					return;
			}

			switch (kv.Key)
			{
				case { } k when k == RelationalEventId.CommandExecuting.Name && ApmAgent.Tracer.CurrentTransaction != null:
					if (kv.Value is CommandEventData commandEventData)
					{
						var newSpan = _agent?.TracerInternal.DbSpanCommon.StartSpan(ApmAgent, commandEventData.Command, InstrumentationFlag.EfCore,
							captureStackTraceOnStart: true);
						_spans.TryAdd(commandEventData.CommandId, newSpan);
					}
					break;
				case { } k when k == RelationalEventId.CommandExecuted.Name:
					if (kv.Value is CommandExecutedEventData commandExecutedEventData)
					{
						if (_spans.TryRemove(commandExecutedEventData.CommandId, out var span))
						{
							_agent?.TracerInternal.DbSpanCommon.EndSpan(span, commandExecutedEventData.Command, Outcome.Success,
								commandExecutedEventData.Duration);
						}
					}
					break;
				case { } k when k == RelationalEventId.CommandError.Name:
					if (kv.Value is CommandErrorEventData commandErrorEventData)
					{
						if (_spans.TryRemove(commandErrorEventData.CommandId, out var span))
						{
							span.CaptureException(commandErrorEventData.Exception);
							_agent?.TracerInternal.DbSpanCommon.EndSpan(span, commandErrorEventData.Command, Outcome.Failure,
								commandErrorEventData.Duration);
						}
					}
					break;
			}
		}
	}
}
