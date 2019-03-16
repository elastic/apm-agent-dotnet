using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.DiagnosticListeners
{
	/// <inheritdoc />
	/// <summary>
	/// Captures web requests initiated by <see cref="T:System.Net.Http.HttpClient" />
	/// </summary>
	internal class HttpDiagnosticListener : IDiagnosticListener
	{
		/// <summary>
		/// Keeps track of ongoing requests
		/// </summary>
		internal readonly ConcurrentDictionary<HttpRequestMessage, Span> ProcessingRequests = new ConcurrentDictionary<HttpRequestMessage, Span>();

		public HttpDiagnosticListener(IApmAgent components) =>
			(Logger, ConfigurationReader) = (components.Logger?.Scoped(nameof(HttpDiagnosticListener)), components.ConfigurationReader);

		private ScopedLogger Logger { get; }
		private IConfigurationReader ConfigurationReader { get; }

		public string Name => "HttpHandlerDiagnosticListener";

		public void OnCompleted() { }

		public void OnError(Exception error) { }  //TODO =>   //Logger.LogErrorException(error, nameof(OnError));

		public void OnNext(KeyValuePair<string, object> kv)
		{
			if (kv.Value == null || string.IsNullOrEmpty(kv.Key)) return;

			if (!(kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request")?.GetValue(kv.Value) is HttpRequestMessage request)) return;

			if (IsRequestFiltered(request?.RequestUri)) return;

			switch (kv.Key)
			{
				case "System.Net.Http.Exception":
					var exception = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Exception").GetValue(kv.Value) as Exception;
					var transaction = Agent.TransactionContainer.Transactions?.Value;

					transaction.CaptureException(exception, "Failed outgoing HTTP request");
					//TODO: we don't know if exception is handled, currently reports handled = false
					break;
				case "System.Net.Http.HttpRequestOut.Start": //TODO: look for consts
					if (Agent.TransactionContainer.Transactions == null || Agent.TransactionContainer.Transactions.Value == null) return;

					transaction = Agent.TransactionContainer.Transactions.Value;

					var span = transaction.StartSpanInternal($"{request?.Method} {request?.RequestUri?.Host}", ApiConstants.TypeExternal,
						ApiConstants.SubtypeHttp);

					if (ProcessingRequests.TryAdd(request, span))
					{
						span.Context.Http = new Http
						{
							Url = request?.RequestUri?.ToString(),
							Method = request?.Method?.Method
						};

						var frames = new StackTrace(true).GetFrames();
						var stackFrames = StacktraceHelper.GenerateApmStackTrace(frames, Logger, span.Name);
						span.StackTrace = stackFrames;
					}
					break;

				case "System.Net.Http.HttpRequestOut.Stop":
					var response = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Response").GetValue(kv.Value) as HttpResponseMessage;

					if (ProcessingRequests.TryRemove(request, out var mspan))
					{
						//TODO: response can be null if for example the request Task is Faulted.
						//E.g. writing this from an airplane without internet, and requestTaskStatus is "Faulted" and response is null
						//How do we report this? There is no response code in that case.
						if (response != null) mspan.Context.Http.StatusCode = (int)response.StatusCode;

						mspan.End();
					}
					else
					{
						const string message = "Failed capturing request '{HttpMethod} {Url}' in System.Net.Http.HttpRequestOut.Stop. This Span will be skipped in case it wasn't captured before.";
						var url = request?.RequestUri?.AbsoluteUri;
						var method = request?.Method?.Method;
						Logger?.Warning()?.Log(message, method, url);
					}
					break;
			}
		}

		/// <summary>
		/// Tells if the given request should be filtered from being captured.
		/// </summary>
		/// <returns><c>true</c>, if request should not be captured, <c>false</c> otherwise.</returns>
		/// <param name="requestUri">Request URI. Can be null, which is not filtered</param>
		private bool IsRequestFiltered(Uri requestUri)
		{
			switch (requestUri)
			{
				case Uri uri when uri == null: return true;
				case Uri uri when ConfigurationReader.ServerUrls.Any(n => n.IsBaseOf(uri)): //TODO: measure the perf of this!
					return true;
				default:
					return false;
			}
		}
	}
}
