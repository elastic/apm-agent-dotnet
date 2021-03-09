using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using MongoDB.Driver.Core.Events;

namespace Elastic.Apm.MongoDb.DiagnosticSource
{
	internal class MongoDiagnosticListener
		: IDiagnosticListener
	{
		private readonly IApmAgent _apmAgent;
		private readonly IApmLogger _logger;

		private readonly ConcurrentDictionary<int, ISpan> _processingQueries = new ConcurrentDictionary<int, ISpan>();

		public string Name => "Elastic.Apm.MongoDb";

		public MongoDiagnosticListener(IApmAgent apmAgent)
		{
			_apmAgent = apmAgent;
			_logger = _apmAgent.Logger.Scoped(nameof(MongoDiagnosticListener));
		}

		public void OnNext(KeyValuePair<string, object> value)
		{
			switch (value.Key)
			{
				case Constants.Events.CommandStart when value.Value is EventPayload<CommandStartedEvent> payload &&
														_apmAgent.Tracer.CurrentTransaction != null:
					HandleCommandStartEvent(payload.Event);
					return;
				case Constants.Events.CommandEnd when value.Value is EventPayload<CommandSucceededEvent> payload:
					HandleCommandSucceededEvent(payload.Event);
					return;
				case Constants.Events.CommandFail when value.Value is EventPayload<CommandFailedEvent> payload:
					HandleCommandFailedEvent(payload.Event);
					return;
			}
		}

		[ExcludeFromCodeCoverage]
		public void OnError(Exception error)
		{
			// do nothing because it's not necessary to handle such event from provider
		}

		[ExcludeFromCodeCoverage]
		public void OnCompleted()
		{
			// do nothing because it's not necessary to handle such event from provider
		}

		private void HandleCommandStartEvent(CommandStartedEvent @event)
		{
			try
			{
				var transaction = _apmAgent.Tracer.CurrentTransaction;
				var currentExecutionSegment = _apmAgent.Tracer.CurrentSpan ?? (IExecutionSegment)transaction;
				var span = currentExecutionSegment.StartSpan(
					@event.CommandName,
					ApiConstants.TypeDb,
					"mongo");

				if (!_processingQueries.TryAdd(@event.RequestId, span))
					return;

				span.Action = ApiConstants.ActionQuery;

				span.Context.Db = new Database
				{
					Statement = @event.Command.ToString(),
					Instance = @event.DatabaseNamespace.DatabaseName,
					Type = "mongo"
				};

				if (@event.ConnectionId?.ServerId?.EndPoint != null)
				{
					span.Context.Destination = @event.ConnectionId.ServerId.EndPoint switch
					{
						IPEndPoint ipEndPoint => new Destination
						{
							Address = ipEndPoint.Address.ToString(),
							Port = ipEndPoint.Port
						},
						DnsEndPoint dnsEndPoint => new Destination { Address = dnsEndPoint.Host, Port = dnsEndPoint.Port },
						_ => null
					};
				}
			}
			catch (Exception ex)
			{
				//ignore
				_logger.Log(LogLevel.Error, "Exception was thrown while handling 'command started event'", ex, null);
			}
		}

		private void HandleCommandSucceededEvent(CommandSucceededEvent @event)
		{
			try
			{
				if (!_processingQueries.TryRemove(@event.RequestId, out var span))
					return;
				span.Duration = @event.Duration.TotalMilliseconds;
				span.End();
			}
			catch (Exception ex)
			{
				// ignore
				_logger.Log(LogLevel.Error, "Exception was thrown while handling 'command succeeded event'", ex, null);
			}
		}

		private void HandleCommandFailedEvent(CommandFailedEvent @event)
		{
			try
			{
				if (!_processingQueries.TryRemove(@event.RequestId, out var span))
					return;
				span.Duration = @event.Duration.TotalMilliseconds;
				span.CaptureException(@event.Failure);
				span.End();
			}
			catch (Exception ex)
			{
				// ignore
				_logger.Log(LogLevel.Error, "Exception was thrown while handling 'command failed event'", ex, null);
			}
		}
	}
}
