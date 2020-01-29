using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.SqlClient
{
	internal class SqlClientDiagnosticListener : IDiagnosticListener
	{
		private class PropertyFetcherSet
		{
			public PropertyFetcher StartCorrelationId { get; } = new PropertyFetcher("OperationId");
			public PropertyFetcher StopCorrelationId { get; } = new PropertyFetcher("OperationId");
			public PropertyFetcher ErrorCorrelationId { get; } = new PropertyFetcher("OperationId");

			public PropertyFetcher Statistics { get; } = new PropertyFetcher("Statistics");

			public PropertyFetcher StartCommand { get; } = new PropertyFetcher("Command");
			public PropertyFetcher StopCommand { get; } = new PropertyFetcher("Command");
			public PropertyFetcher ErrorCommand { get; } = new PropertyFetcher("Command");

			public PropertyFetcher Exception { get; } = new PropertyFetcher("Exception");
		}

		private readonly ApmAgent _apmAgent;
		private readonly IApmLogger _logger;

		private readonly ConcurrentDictionary<Guid, Span> _spans = new ConcurrentDictionary<Guid, Span>();

		private readonly PropertyFetcherSet _systemPropertyFetcherSet = new PropertyFetcherSet();
		private readonly PropertyFetcherSet _microsoftPropertyFetcherSet = new PropertyFetcherSet();

		public SqlClientDiagnosticListener(IApmAgent apmAgent)
		{
			_apmAgent = (ApmAgent)apmAgent;
			_logger = _apmAgent.Logger.Scoped(nameof(SqlClientDiagnosticListener));
		}

		public string Name => "SqlClientDiagnosticListener";

		// prefix - Microsoft.Data.SqlClient. or System.Data.SqlClient.
		public void OnNext(KeyValuePair<string, object> value)
		{
			if (value.Key.StartsWith("Microsoft.Data.SqlClient.") || value.Key.StartsWith("System.Data.SqlClient."))
			{
				switch (value.Key)
				{
					case string s when s.EndsWith("WriteCommandBefore") && _apmAgent.Tracer.CurrentTransaction != null:
						HandleStartCommand(value.Value, value.Key.StartsWith("System") ? _systemPropertyFetcherSet : _microsoftPropertyFetcherSet);
						break;
					case string s when s.EndsWith("WriteCommandAfter"):
						HandleStopCommand(value.Value, value.Key.StartsWith("System") ? _systemPropertyFetcherSet : _microsoftPropertyFetcherSet);
						break;
					case string s when s.EndsWith("WriteCommandError"):
						HandleErrorCommand(value.Value, value.Key.StartsWith("System") ? _systemPropertyFetcherSet : _microsoftPropertyFetcherSet);
						break;
				}
			}
		}

		private void HandleStartCommand(object payloadData, PropertyFetcherSet propertyFetcherSet)
		{
			try
			{
				if (propertyFetcherSet.StartCorrelationId.Fetch(payloadData) is Guid operationId
					&& propertyFetcherSet.StartCommand.Fetch(payloadData) is IDbCommand dbCommand)
				{
					var span = _apmAgent.TracerInternal.DbSpanCommon.StartSpan(_apmAgent, dbCommand);
					_spans.TryAdd(operationId, span);
				}
			}
			catch (Exception ex)
			{
				//ignore
				_logger.Error()?.LogException(ex, "Exception was thrown while handling 'command started event'");
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

					TimeSpan? duration = null;

					if (propertyFetcherSet.Statistics.Fetch(payloadData) is IDictionary<object, object> statistics &&
						statistics.ContainsKey("ExecutionTime") && statistics["ExecutionTime"] is long durationInMs)
						duration = TimeSpan.FromMilliseconds(durationInMs);

					_apmAgent.TracerInternal.DbSpanCommon.EndSpan(span, dbCommand, duration);
				}
			}
			catch (Exception ex)
			{
				// ignore
				_logger.Error()?.LogException(ex, "Exception was thrown while handling 'command succeeded event'");
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
					{
						_apmAgent.TracerInternal.DbSpanCommon.EndSpan(span, dbCommand);
					}
					else
					{
						_logger.Warning()?.Log("Cannot extract database command from {PayloadData}", payloadData);
						span.End();
					}
				}
			}
			catch (Exception ex)
			{
				// ignore
				_logger.Error()?.LogException(ex, "Exception was thrown while handling 'command failed event'");
			}
		}

		public void OnError(Exception error)
		{
			// do nothing because it's not necessary to handle such event from provider
		}

		public void OnCompleted()
		{
			// do nothing because it's not necessary to handle such event from provider
		}
	}
}
