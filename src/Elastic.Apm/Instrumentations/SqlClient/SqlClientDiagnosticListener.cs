// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Reflection;

namespace Elastic.Apm.Instrumentations.SqlClient
{
	internal class SqlClientDiagnosticListener : DiagnosticListenerBase, IDisposable
	{
		private readonly ApmAgent _agent;
		private readonly PropertyFetcherSet _microsoftPropertyFetcherSet = new();

		// SqlClient can emit WriteCommandBefore without a matching WriteCommandAfter/WriteCommandError on certain
		// cancellation paths, so pending spans are tracked in a bounded, self-expiring store to avoid leaking them.
		private readonly PendingSpanStore _spans;

		private readonly PropertyFetcherSet _systemPropertyFetcherSet = new();

		public SqlClientDiagnosticListener(IApmAgent apmAgent) : this(apmAgent, null) { }

		internal SqlClientDiagnosticListener(IApmAgent apmAgent, PendingSpanStore spanStore) : base(apmAgent)
		{
			_agent = apmAgent as ApmAgent;
			_spans = spanStore ?? new PendingSpanStore(Logger);
		}

		internal int PendingSpanCount => _spans.Count;

		public override bool AllowDuplicates => true;

		public override string Name => "SqlClientDiagnosticListener";

		public void Dispose() => _spans.Dispose();

		// prefix - Microsoft.Data.SqlClient. or System.Data.SqlClient.
		protected override void HandleOnNext(KeyValuePair<string, object> value)
		{
			// check for competing instrumentation
			if (ApmAgent.Tracer.CurrentSpan is Span span)
			{
				if (span.InstrumentationFlag == InstrumentationFlag.EfCore || span.InstrumentationFlag == InstrumentationFlag.EfClassic)
					return;
			}

			if (!value.Key.StartsWith("Microsoft.Data.SqlClient.") && !value.Key.StartsWith("System.Data.SqlClient."))
				return;

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
					var span = DbSpanCommon.StartSpan(ApmAgent, dbCommand, InstrumentationFlag.SqlClient,
						ApiConstants.SubtypeMssql, makeCurrent: false);
					_spans.Add(operationId, span, GetMaxSpanAge(dbCommand));
				}
			}
			catch (Exception ex)
			{
				Logger.Error()?.LogException(ex, "Exception was thrown while handling 'command started event'");
			}
		}

		private void HandleStopCommand(object payloadData, PropertyFetcherSet propertyFetcherSet)
		{
			try
			{
				if (propertyFetcherSet.StopCorrelationId.Fetch(payloadData) is Guid operationId)
				{
					// Remove based on the correlation id alone, so that a failure to extract the command
					// cannot leave the entry (and the span it roots) in the store forever.
					if (!_spans.TryRemove(operationId, out var span) || span is null)
						return;

					try
					{
						if (propertyFetcherSet.StopCommand.Fetch(payloadData) is IDbCommand dbCommand)
							_agent?.TracerInternal.DbSpanCommon.EndSpan(span, dbCommand, Outcome.Success);
						else
						{
							Logger.Warning()?.Log("Cannot extract database command from {PayloadData}", payloadData);
							span.End();
						}
					}
					catch (Exception ex)
					{
						Logger.Error()?.LogException(ex, "Exception was thrown while ending span for 'command succeeded event'");
					}
					finally
					{
						// EndSpan may throw before span.End(); Abandon is a no-op if End already won the gate.
						if (span is Span capturedSpan)
							capturedSpan.Abandon();
					}
				}
			}
			catch (Exception ex)
			{
				// ignore
				Logger.Error()?.LogException(ex, "Exception was thrown while handling 'command succeeded event'");
			}
		}

		private static TimeSpan? GetMaxSpanAge(IDbCommand dbCommand)
		{
			try
			{
				var timeoutSeconds = dbCommand.CommandTimeout;
				return timeoutSeconds > 0 ? TimeSpan.FromSeconds(timeoutSeconds * 4L) : null;
			}
			catch
			{
				return null;
			}
		}

		private void HandleErrorCommand(object payloadData, PropertyFetcherSet propertyFetcherSet)
		{
			try
			{
				if (propertyFetcherSet.ErrorCorrelationId.Fetch(payloadData) is Guid operationId)
				{
					if (!_spans.TryRemove(operationId, out var span) || span is null)
						return;

					try
					{
						if (propertyFetcherSet.Exception.Fetch(payloadData) is Exception exception)
							span.CaptureException(exception);

						if (propertyFetcherSet.ErrorCommand.Fetch(payloadData) is IDbCommand dbCommand)
							_agent?.TracerInternal.DbSpanCommon.EndSpan(span, dbCommand, Outcome.Failure);
						else
						{
							Logger.Warning()?.Log("Cannot extract database command from {PayloadData}", payloadData);
							span.Outcome = Outcome.Failure;
							span.End();
						}
					}
					catch (Exception ex)
					{
						Logger.Error()?.LogException(ex, "Exception was thrown while ending span for 'command failed event'");
					}
					finally
					{
						if (span is Span capturedSpan)
							capturedSpan.Abandon();
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
			public PropertyFetcher ErrorCommand { get; } = new("Command");
			public PropertyFetcher ErrorCorrelationId { get; } = new("OperationId");

			public PropertyFetcher Exception { get; } = new("Exception");

			public PropertyFetcher StartCommand { get; } = new("Command");
			public PropertyFetcher StartCorrelationId { get; } = new("OperationId");

			// ReSharper disable once UnusedMember.Local
			public PropertyFetcher Statistics { get; } = new("Statistics");
			public PropertyFetcher StopCommand { get; } = new("Command");
			public PropertyFetcher StopCorrelationId { get; } = new("OperationId");
		}
	}
}
