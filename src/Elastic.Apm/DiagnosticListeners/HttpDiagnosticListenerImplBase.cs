using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DiagnosticListeners
{
	/// <inheritdoc />
	/// <summary>
	/// Captures web requests initiated by <see cref="T:System.Net.Http.HttpClient" />
	/// </summary>
	internal abstract class HttpDiagnosticListenerImplBase : IDiagnosticListener
	{
		protected const string EventExceptionPropertyName = "Exception";
		protected const string EventRequestPropertyName = "Request";
		protected const string EventResponsePropertyName = "Response";

		protected readonly IApmAgent Agent;

		/// <summary>
		/// Keeps track of ongoing requests
		/// </summary>
		internal readonly ConcurrentDictionary<object, ISpan> ProcessingRequests = new ConcurrentDictionary<object, ISpan>();

		private readonly ScopedLogger _logger;

		protected HttpDiagnosticListenerImplBase(IApmAgent agent)
		{
			Agent = agent;
			_logger = Agent.Logger?.Scoped("HttpDiagnosticListenerImplBase");
		}

		internal interface IEventData
		{
			string Method { get; }
			object Request { get; }
			int? StatusCode { get; }
			Uri Url { get; }

			void AddRequestHeader(string headerName, string headerValue);

			bool ContainsRequestHeader(string headerName);
		}

		protected abstract bool DispatchEventProcessing(KeyValuePair<string, object> kv);

		public abstract string Name { get; }

		public void OnCompleted() { }

		public void OnError(Exception error) => _logger.Error()?.LogExceptionWithCaller(error, nameof(OnError));

		public void OnNext(KeyValuePair<string, object> kv)
		{
			try
			{
				OnNextImpl(kv);
			}
			catch (Exception ex)
			{
				_logger.Error()?.LogException(ex, "Processing of event passed to OnNext failed. Event: {DiagnosticEvent}", kv.Key);
			}
		}

		private void OnNextImpl(KeyValuePair<string, object> kv)
		{
			_logger.Trace()?.Log("Called with key: `{DiagnosticEventKey}', value: {DiagnosticEventValue}", kv.Key, kv.Value);

			if (string.IsNullOrEmpty(kv.Key))
			{
				_logger.Trace()?.Log($"Key is {(kv.Key == null ? "null" : "an empty string")} - exiting");
				return;
			}

			if (kv.Value == null)
			{
				_logger.Trace()?.Log("Value is null - exiting");
				return;
			}

			if (!DispatchEventProcessing(kv))
				_logger.Trace()?.Log("Unrecognized key `{DiagnosticEventKey}'. Event value: {DiagnosticEventValue}", kv.Key, kv.Value);
		}

		protected static TProperty ExtractProperty<TProperty>(string propertyName, KeyValuePair<string, object> kv)
		{
			try
			{
				var propertyInfo = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty(propertyName);
				if (propertyInfo == null)
				{
					throw new FailedToExtractPropertyException("Event's value type doesn't have expected property." +
						$" Expected property name: `{propertyName}' Event: {kv}");
				}

				var propertyObject = propertyInfo.GetValue(kv.Value);

				if (!typeof(TProperty).IsAssignableFrom(propertyInfo.PropertyType))
				{
					throw new FailedToExtractPropertyException("Actual type of property value in doesn't match the expected type." +
						$" Expected type: {typeof(TProperty).FullName}." +
						$" Actual type: {propertyObject?.GetType().FullName}." +
						$" Property name: {propertyName}." +
						$" Property value: {propertyObject}." +
						$" Event: {kv}.");
				}

				return (TProperty)propertyObject;
			}
			catch (FailedToExtractPropertyException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new FailedToExtractPropertyException("Failed to extract property from event." +
					$" Property name: {propertyName}." +
					$" Expected property type: {typeof(TProperty).FullName}." +
					$" Event: {kv}.",
					ex);
			}
		}

		protected void ProcessStartStopEvent(IEventData eventData, bool isStart)
		{
			if (IsRequestFilteredOut(eventData.Url))
			{
				_logger.Trace()?.Log("Request URL ({RequestUrl}) is filtered out - exiting", eventData.Url);
				return;
			}

			if (isStart)
				ProcessStartEvent(eventData);
			else
				ProcessStopEvent(eventData);
		}

		protected void ProcessStartEvent(IEventData eventData)
		{
			_logger.Trace()?.Log("Processing start event... Request URL: {RequestUrl}", eventData.Url);

			var transaction = Agent.Tracer.CurrentTransaction;
			if (transaction == null)
			{
				_logger.Debug()?.Log("No current transaction, skip creating span for outgoing HTTP request");
				return;
			}

			var currentExecutionSegment = Agent.Tracer.CurrentSpan ?? (IExecutionSegment)transaction;
			var span = currentExecutionSegment.StartSpan(
				$"{eventData.Method} {eventData.Url.Host}",
				ApiConstants.TypeExternal,
				ApiConstants.SubtypeHttp);

			if (!ProcessingRequests.TryAdd(eventData.Request, span))
			{
				// Consider improving error reporting - see https://github.com/elastic/apm-agent-dotnet/issues/280
				_logger.Error()?.Log("Failed to add to ProcessingRequests - ???");
				return;
			}

			if (!eventData.ContainsRequestHeader(TraceParent.TraceParentHeaderName))
				// We call TraceParent.BuildTraceparent explicitly instead of DistributedTracingData.SerializeToString because
				// in the future we might change DistributedTracingData.SerializeToString to use some other internal format
				// but here we want the string to be in W3C 'traceparent' header format.
				eventData.AddRequestHeader(TraceParent.TraceParentHeaderName, TraceParent.BuildTraceparent(span.OutgoingDistributedTracingData));

			if (transaction.IsSampled) span.Context.Http = new Http { Url = eventData.Url.ToString(), Method = eventData.Method };
		}

		protected void ProcessStopEvent(IEventData eventData)
		{
			_logger.Trace()?.Log("Processing stop event... Request URL: {RequestUrl}", eventData.Url);

			if (!ProcessingRequests.TryRemove(eventData.Request, out var span))
			{
				_logger.Warning()
					?.Log("Failed to remove from ProcessingRequests - " +
						"it might be because Start event was not captured successfully. " +
						"Request: method: {HttpMethod}, URL: {RequestUrl}", eventData.Method, eventData.Url);
				return;
			}

			// if span.Context.Http == null that means the transaction is not sampled (see ProcessStartEvent)
			// and thus we don't need to capture spans' context
			if (span.Context.Http != null) span.Context.Http.StatusCode = eventData.StatusCode;

			span.End();
		}

		/// <summary>
		/// Tells if the given request should be filtered from being captured.
		/// </summary>
		/// <returns><c>true</c>, if request should not be captured, <c>false</c> otherwise.</returns>
		/// <param name="requestUri">Request URI. It cannot be null</param>
		protected bool IsRequestFilteredOut(Uri requestUri) => Agent.ConfigurationReader.ServerUrls.Any(n => n.IsBaseOf(requestUri));

		protected class FailedToExtractPropertyException : Exception
		{
			internal FailedToExtractPropertyException(string message) : base(message) { }

			internal FailedToExtractPropertyException(string message, Exception cause) : base(message, cause) { }
		}
	}
}
