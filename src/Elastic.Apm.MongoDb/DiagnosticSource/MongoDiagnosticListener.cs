// Based on the elastic-apm-mongo project by Vadim Hatsura (@vhatsura)
// https://github.com/vhatsura/elastic-apm-mongo
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using MongoDB.Driver.Core.Events;

namespace Elastic.Apm.MongoDb.DiagnosticSource
{
	internal class MongoDiagnosticListener : DiagnosticListenerBase
	{

		private readonly ConcurrentDictionary<int, ISpan> _processingQueries = new ConcurrentDictionary<int, ISpan>();

		public override string Name => Constants.MongoDiagnosticName;

		public MongoDiagnosticListener(IApmAgent apmAgent) : base(apmAgent) { }

		protected override void HandleOnNext(KeyValuePair<string, object> kv)
		{
			Logger.Debug()?.Log("called with key: {eventKey}", kv.Key);

			switch (kv.Key)
			{
				case Constants.Events.CommandStart when kv.Value is EventPayload<CommandStartedEvent> payload:
					if (ApmAgent.Tracer.CurrentTransaction != null)
						HandleCommandStartEvent(payload.Event);
					else
						Logger.Debug()?.Log("No current transaction, skip creating span for MongoDB call.");
					return;
				case Constants.Events.CommandEnd when kv.Value is EventPayload<CommandSucceededEvent> payload:
					HandleCommandSucceededEvent(payload.Event);
					return;
				case Constants.Events.CommandFail when kv.Value is EventPayload<CommandFailedEvent> payload:
					HandleCommandFailedEvent(payload.Event);
					return;
			}
		}

		private void HandleCommandStartEvent(CommandStartedEvent @event)
		{
			try
			{
				Logger.Trace()?.Log(nameof(HandleCommandStartEvent));
				var currentExecutionSegment = ApmAgent.GetCurrentExecutionSegment();
				var span = currentExecutionSegment.StartSpan(
					@event.CommandName,
					ApiConstants.TypeDb,
					"mongo");

				if (!_processingQueries.TryAdd(@event.RequestId, span))
				{
					Logger.Trace()?.Log("Failed adding item to _processingQueries with RequestId: {RequestId}",
						@event.RequestId);
					return;
				}

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
				Logger.Log(LogLevel.Error, "Exception was thrown while handling 'command started event'", ex, null);
			}
		}

		private void HandleCommandSucceededEvent(CommandSucceededEvent @event)
		{
			try
			{
				Logger.Trace()?.Log(nameof(HandleCommandSucceededEvent));

				if (!_processingQueries.TryRemove(@event.RequestId, out var span))
				{
					Logger.Trace()?.Log("Failed removing item from _processingQueries for RequestId: {RequestId}", @event.RequestId);
					return;
				}

				span.Duration = @event.Duration.TotalMilliseconds;
				span.End();
			}
			catch (Exception ex)
			{
				// ignore
				Logger.Log(LogLevel.Error, "Exception was thrown while handling 'command succeeded event'", ex, null);
			}
		}

		private void HandleCommandFailedEvent(CommandFailedEvent @event)
		{
			try
			{
				Logger.Trace()?.Log(nameof(HandleCommandFailedEvent));

				if (!_processingQueries.TryRemove(@event.RequestId, out var span))
				{
					Logger.Trace()?.Log("Failed removing item from _processingQueries for RequestId: {RequestId}", @event.RequestId);
					return;
				}


				span.Duration = @event.Duration.TotalMilliseconds;
				span.CaptureException(@event.Failure);
				span.End();
			}
			catch (Exception ex)
			{
				// ignore
				Logger.Log(LogLevel.Error, "Exception was thrown while handling 'command failed event'", ex, null);
			}
		}
	}
}
