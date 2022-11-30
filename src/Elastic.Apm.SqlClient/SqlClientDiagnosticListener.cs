// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Reflection;

namespace Elastic.Apm.SqlClient
{
	internal class SqlClientDiagnosticListener : DiagnosticListenerBase
	{
		private ApmAgent _agent;
		private readonly PropertyFetcherSet _microsoftPropertyFetcherSet = new PropertyFetcherSet();

		private readonly ConcurrentDictionary<Guid, ISpan> _spans = new ConcurrentDictionary<Guid, ISpan>();

		private readonly PropertyFetcherSet _systemPropertyFetcherSet = new PropertyFetcherSet();

		public SqlClientDiagnosticListener(IApmAgent apmAgent) : base(apmAgent) => _agent = apmAgent as ApmAgent;

		public override bool AllowDuplicates => true;

		public override string Name => "SqlClientDiagnosticListener";

		// prefix - Microsoft.Data.SqlClient. or System.Data.SqlClient.
		protected override void HandleOnNext(KeyValuePair<string, object> value)
		{
			// check for competing instrumentation
			if (ApmAgent.Tracer.CurrentSpan is Span span)
			{
				if (span.InstrumentationFlag == InstrumentationFlag.EfCore || span.InstrumentationFlag == InstrumentationFlag.EfClassic)
					return;
			}

			if (!value.Key.StartsWith("Microsoft.Data.SqlClient.") && !value.Key.StartsWith("System.Data.SqlClient.")) return;

			switch (value.Key)
			{
				case { } s when s.EndsWith("WriteCommandBefore") && ApmAgent.Tracer.CurrentTransaction != null:
					HandleStartCommand(value.Value, value.Key.StartsWith("System") ? _systemPropertyFetcherSet : _microsoftPropertyFetcherSet);
					break;
				case { } s when s.EndsWith("WriteCommandAfter"):
					HandleStopCommand(value.Value, value.Key.StartsWith("System") ? _systemPropertyFetcherSet : _microsoftPropertyFetcherSet);
					break;
				case { } s when s.EndsWith("WriteCommandError"):
					HandleErrorCommand(value.Value, value.Key.StartsWith("System") ? _systemPropertyFetcherSet : _microsoftPropertyFetcherSet);
					break;
			}
		}

		private void HandleStartCommand(object payloadData, PropertyFetcherSet propertyFetcherSet)
		{
			try
			{
				if (propertyFetcherSet.StartCorrelationId.Fetch(payloadData) is Guid operationId
					&& propertyFetcherSet.StartCommand.Fetch(payloadData) is IDbCommand dbCommand)
				{
					var span = _agent?.TracerInternal.DbSpanCommon.StartSpan(ApmAgent, dbCommand, InstrumentationFlag.SqlClient,
						ApiConstants.SubtypeMssql);
					_spans.TryAdd(operationId, span);
				}
			}
			catch (Exception ex)
			{
				//ignore
				Logger.Error()?.LogException(ex, "Exception was thrown while handling 'command started event'");
			}
		}

		private void HandleStopCommand(object payloadData, PropertyFetcherSet propertyFetcherSet)
		{
			try
			{
				if (propertyFetcherSet.StopCorrelationId.Fetch(payloadData) is Guid operationId
					&& propertyFetcherSet.StopCommand.Fetch(payloadData) is IDbCommand dbCommand)
				{
					if (!_spans.TryRemove(operationId, out var span)) return;

					_agent?.TracerInternal.DbSpanCommon.EndSpan(span, dbCommand, Outcome.Success);
				}
			}
			catch (Exception ex)
			{
				// ignore
				Logger.Error()?.LogException(ex, "Exception was thrown while handling 'command succeeded event'");
			}
		}

		private void HandleErrorCommand(object payloadData, PropertyFetcherSet propertyFetcherSet)
		{
			try
			{
				if (propertyFetcherSet.ErrorCorrelationId.Fetch(payloadData) is Guid operationId)
				{
					if (!_spans.TryRemove(operationId, out var span)) return;

					if (propertyFetcherSet.Exception.Fetch(payloadData) is Exception exception) span.CaptureException(exception);

					if (propertyFetcherSet.ErrorCommand.Fetch(payloadData) is IDbCommand dbCommand)
						_agent?.TracerInternal.DbSpanCommon.EndSpan(span, dbCommand, Outcome.Failure);
					else
					{
						Logger.Warning()?.Log("Cannot extract database command from {PayloadData}", payloadData);
						span.Outcome = Outcome.Failure;
						span.End();
					}
				}
			}
			catch (Exception ex)
			{
				// ignore
				Logger.Error()?.LogException(ex, "Exception was thrown while handling 'command failed event'");
			}
		}

		private class PropertyFetcherSet
		{
			public PropertyFetcher ErrorCommand { get; } = new PropertyFetcher("Command");
			public PropertyFetcher ErrorCorrelationId { get; } = new PropertyFetcher("OperationId");

			public PropertyFetcher Exception { get; } = new PropertyFetcher("Exception");

			public PropertyFetcher StartCommand { get; } = new PropertyFetcher("Command");
			public PropertyFetcher StartCorrelationId { get; } = new PropertyFetcher("OperationId");

			public PropertyFetcher Statistics { get; } = new PropertyFetcher("Statistics");
			public PropertyFetcher StopCommand { get; } = new PropertyFetcher("Command");
			public PropertyFetcher StopCorrelationId { get; } = new PropertyFetcher("OperationId");
		}
	}
}
