// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.Cloud;
using Elastic.Apm.Config;
using Elastic.Apm.Filters;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Report
{
	/// <summary>
	/// Responsible for sending the data to APM server. Implements Intake V2.
	/// Each instance creates its own thread to do the work. Therefore, instances should be reused if possible.
	/// </summary>
	internal class PayloadSenderV2 : BackendCommComponentBase, IPayloadSender, IPayloadSenderWithFilters
	{
		private const string ThisClassName = nameof(PayloadSenderV2);
		private readonly List<Func<IError, IError>> ErrorFilters = new();
		private readonly List<Func<ISpan, ISpan>> SpanFilters = new();
		private readonly List<Func<ITransaction, ITransaction>> TransactionFilters = new();

		private readonly IApmServerInfo _apmServerInfo;

		// A callback which is triggered after the ServerInfo is fetched.
		// Components that depend on the server version can register for this callback and make decisions right after the server version is fetched.
		private readonly Action<bool, IApmServerInfo> _serverInfoCallback;
		private readonly CloudMetadataProviderCollection _cloudMetadataProviderCollection;
		internal readonly Api.System System;
		private readonly IConfiguration _configuration;

		private readonly BatchBlock<object> _eventQueue;
		private readonly TimeSpan _flushInterval;
		private readonly Uri _intakeV2EventsAbsoluteUrl;
		private readonly IApmLogger _logger;
		private readonly int _maxQueueEventCount;
		private readonly Metadata _metadata;
		private readonly PayloadItemSerializer _payloadItemSerializer;

		private string _cachedMetadataJsonLine;
		private long _eventQueueCount;

		private readonly ElasticVersion _brokenActivationMethodVersion;
		private readonly string _cachedActivationMethod;
		private readonly bool _isEnabled;

		public PayloadSenderV2(
			IApmLogger logger,
			IConfiguration configuration,
			Service service,
			Api.System system,
			IApmServerInfo apmServerInfo,
			HttpMessageHandler httpMessageHandler = null,
			string dbgName = null,
			bool isEnabled = true,
			IEnvironmentVariables environmentVariables = null,
			Action<bool, IApmServerInfo> serverInfoCallback = null
		)
			: base(isEnabled, logger, ThisClassName, service, configuration, httpMessageHandler)
		{
			_logger = logger?.Scoped(ThisClassName + (dbgName == null ? "" : $" (dbgName: `{dbgName}')"));
			_isEnabled = isEnabled;
			if (!_isEnabled)
				_logger?.Debug()?.Log($"{nameof(PayloadSenderV2)} is not enabled, work loop won't be started.");

			_payloadItemSerializer = new PayloadItemSerializer();
			_configuration = configuration;
			_brokenActivationMethodVersion = new ElasticVersion(8, 7, 0);
			_intakeV2EventsAbsoluteUrl = BackendCommUtils.ApmServerEndpoints.BuildIntakeV2EventsAbsoluteUrl(configuration.ServerUrl);

			System = system;

			_cloudMetadataProviderCollection = new CloudMetadataProviderCollection(configuration.CloudProvider, _logger, environmentVariables);
			_apmServerInfo = apmServerInfo ?? new ApmServerInfo();
			_serverInfoCallback = serverInfoCallback;

			var process = ProcessInformation.Create();
			_metadata = new Metadata { Service = service, System = System, Process = process };
			foreach (var globalLabelKeyValue in configuration.GlobalLabels)
				_metadata.Labels.Add(globalLabelKeyValue.Key, globalLabelKeyValue.Value);
			_cachedActivationMethod = _metadata.Service?.Agent.ActivationMethod;

			if (_isEnabled)
				ResetActivationMethodIfKnownBrokenApmServer();

			if (configuration.MaxQueueEventCount < configuration.MaxBatchEventCount)
			{
				_logger?.Error()
					?.Log(
						"MaxQueueEventCount is less than MaxBatchEventCount - using MaxBatchEventCount as MaxQueueEventCount."
						+ " MaxQueueEventCount: {MaxQueueEventCount}."
						+ " MaxBatchEventCount: {MaxBatchEventCount}.",
						configuration.MaxQueueEventCount, configuration.MaxBatchEventCount);

				_maxQueueEventCount = configuration.MaxBatchEventCount;
			}
			else
				_maxQueueEventCount = configuration.MaxQueueEventCount;

			_flushInterval = configuration.FlushInterval;

			_logger?.Debug()
				?.Log(
					"Using the following configuration options:"
					+ " Events intake API absolute URL: {EventsIntakeAbsoluteUrl}"
					+ ", FlushInterval: {FlushInterval}"
					+ ", MaxBatchEventCount: {MaxBatchEventCount}"
					+ ", MaxQueueEventCount: {MaxQueueEventCount}"
					, _intakeV2EventsAbsoluteUrl.Sanitize(), _flushInterval.ToHms(), configuration.MaxBatchEventCount, _maxQueueEventCount);

			_eventQueue = new BatchBlock<object>(configuration.MaxBatchEventCount);

			SetUpFilters(TransactionFilters, SpanFilters, ErrorFilters, apmServerInfo, logger);

			if (_isEnabled)
				StartWorkLoop();
		}

		internal static void SetUpFilters(
			List<Func<ITransaction, ITransaction>> transactionFilters,
			List<Func<ISpan, ISpan>> spanFilters,
			List<Func<IError, IError>> errorFilters,
			IApmServerInfo apmServerInfo,
			IApmLogger logger
		)
		{
			transactionFilters.Add(TransactionIgnoreUrlsFilter.Filter);
			transactionFilters.Add(CookieHeaderRedactionFilter.Filter);
			transactionFilters.Add(HeaderDictionarySanitizerFilter.Filter);

			// with this, stack trace demystification and conversion to the intake API model happens on a non-application thread:
			spanFilters.Add(new SpanStackTraceCapturingFilter(logger, apmServerInfo).Filter);

			errorFilters.Add(ErrorContextSanitizerFilter.Filter);
			errorFilters.Add(CookieHeaderRedactionFilter.Filter);
			errorFilters.Add(HeaderDictionarySanitizerFilter.Filter);
		}

		private bool _getApmServerVersion;
		private bool _getCloudMetadata;
		private bool _allowFilterAdd = true;
		private static readonly UTF8Encoding Utf8Encoding;
		private static readonly MediaTypeHeaderValue MediaTypeHeaderValue;

		static PayloadSenderV2()
		{
			Utf8Encoding = new UTF8Encoding(false);
			MediaTypeHeaderValue = new MediaTypeHeaderValue("application/x-ndjson") { CharSet = Utf8Encoding.WebName };
		}

		public void QueueTransaction(ITransaction transaction) => EnqueueEvent(transaction, "Transaction");

		public void QueueSpan(ISpan span) => EnqueueEvent(span, "Span");

		public void QueueMetrics(IMetricSet metricSet) => EnqueueEvent(metricSet, "MetricSet");

		public void QueueError(IError error) => EnqueueEvent(error, "Error");

		protected override void Dispose(bool disposing)
		{
			_eventQueue?.TriggerBatch();
			_eventQueue?.Complete();
			base.Dispose(disposing);
		}

		internal async Task<bool> EnqueueEventInternal(object eventObj, string dbgEventKind)
		{
			if (!_isEnabled)
				return true;

			// Enforce _maxQueueEventCount manually instead of using BatchBlock's BoundedCapacity
			// because of the issue of Post returning false when TriggerBatch is in progress. For more details see
			// https://stackoverflow.com/questions/35626955/unexpected-behaviour-tpl-dataflow-batchblock-rejects-items-while-triggerbatch
			var newEventQueueCount = Interlocked.Increment(ref _eventQueueCount);
			if (newEventQueueCount > _maxQueueEventCount)
			{
				_logger.Debug()
					?.Log("Queue reached max capacity - " + dbgEventKind + " will be discarded."
						+ " newEventQueueCount: {EventQueueCount}."
						+ " MaxQueueEventCount: {MaxQueueEventCount}."
						+ " " + dbgEventKind + ": {" + dbgEventKind + "}."
						, newEventQueueCount, _maxQueueEventCount, eventObj);
				Interlocked.Decrement(ref _eventQueueCount);
				return false;
			}

			var enqueuedSuccessfully = await _eventQueue.SendAsync(eventObj);
			if (!enqueuedSuccessfully)
			{
				_logger.Debug()
					?.Log("Failed to enqueue " + dbgEventKind + "."
						+ " newEventQueueCount: {EventQueueCount}."
						+ " MaxQueueEventCount: {MaxQueueEventCount}."
						+ " " + dbgEventKind + ": {" + dbgEventKind + "}."
						, newEventQueueCount, _maxQueueEventCount, eventObj);
				Interlocked.Decrement(ref _eventQueueCount);
				return false;
			}

			_logger.Debug()
				?.Log("Enqueued " + dbgEventKind + "."
					+ " newEventQueueCount: {EventQueueCount}."
					+ " MaxQueueEventCount: {MaxQueueEventCount}."
					+ " " + dbgEventKind + ": {" + dbgEventKind + "}."
					, newEventQueueCount, _maxQueueEventCount, eventObj);

			if (_flushInterval == TimeSpan.Zero)
				_eventQueue.TriggerBatch();

			return true;
		}

		internal void EnqueueEvent(object eventObj, string dbgEventKind)
		{
			if (!_isEnabled)
				return;
			Task.Run(async () => await EnqueueEventInternal(eventObj, dbgEventKind));
		}

		/// <summary>
		/// Runs on the background thread dedicated to sending data to APM Server. It's ok to block this thread.
		/// </summary>
		protected override void WorkLoopIteration()
		{
			if (!_getCloudMetadata)
			{
				var cloud = _cloudMetadataProviderCollection.GetMetadataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
				if (cloud != null)
					_metadata.Cloud = cloud;

				_getCloudMetadata = true;
			}

			if (!_getApmServerVersion && _apmServerInfo?.Version is null)
			{
				ApmServerInfoProvider.FillApmServerInfo(_apmServerInfo, _logger, _configuration, HttpClient, _serverInfoCallback)
					.ConfigureAwait(false)
					.GetAwaiter()
					.GetResult();
				_getApmServerVersion = true;
				ResetActivationMethodIfKnownBrokenApmServer();
			}

			var batch = ReceiveBatch();
			if (_allowFilterAdd && batch is { Length: > 0 })
				_allowFilterAdd = false;
			if (batch != null)
				ProcessQueueItems(batch);
		}

		private void ResetActivationMethodIfKnownBrokenApmServer()
		{
			// if we are on a known apm-server version that breaks metrics intake with activation method
			if (_apmServerInfo?.Version == _brokenActivationMethodVersion)
			{
				// remove activation method since we know apm-server 8.7.0 rejects it
				// and may cause metrics to be dropped
				_metadata.Service.Agent.ActivationMethod = null;
				_cachedMetadataJsonLine = null;
			}
			// else if activation method was unset but now the server has been upgraded re-apply activation method
			else if (_metadata.Service.Agent.ActivationMethod == null && _cachedActivationMethod != null)
			{
				_metadata.Service.Agent.ActivationMethod = _cachedActivationMethod;
				_cachedMetadataJsonLine = null;
			}
		}

		private object[] ReceiveBatch()
		{
			_eventQueue.TryReceive(null, out var receivedItems);

			if (_flushInterval == TimeSpan.Zero)
			{
				_logger.Trace()?.Log("Waiting for data to send... (not using FlushInterval timer because FlushInterval is 0)");
				try
				{
					if (receivedItems == null)
						receivedItems = _eventQueue.Receive(CancellationTokenSource.Token);
				}
				catch (InvalidOperationException ex) when (ex.Message.StartsWith("The source completed without"))
				{
					_logger.Trace()?.Log("EventQueue completed before receiving data.");
				}
				catch (OperationCanceledException)
				{
					_logger.Trace()?.Log("Waiting on EventQueue cancelled.");
				}
			}
			else
			{
				while (!CancellationTokenSource.IsCancellationRequested)
				{
					_logger.Trace()?.Log("Waiting for data to send... FlushInterval: {FlushInterval}", _flushInterval.ToHms());

					if (receivedItems == null)
					{
						try
						{
							receivedItems = _eventQueue.Receive(_flushInterval);
						}
						catch (InvalidOperationException ex) when (ex.Message.StartsWith("The source completed without"))
						{
							_logger.Trace()?.Log("EventQueue completed before receiving data.");
						}
						catch (TimeoutException)
						{
							_logger.Trace()?.Log("FlushInterval reached, no item in the queue");
						}

						if (receivedItems?.Length > 0)
							break;
					}
					else
						break;

					_eventQueue.TriggerBatch();
				}
			}

			if (receivedItems == null)
				_eventQueue.TryReceive(null, out receivedItems);

			var eventBatchToSend = receivedItems;
			if (eventBatchToSend != null)
			{
				var newEventQueueCount = Interlocked.Add(ref _eventQueueCount, -eventBatchToSend.Length);

				_logger.Trace()
					?.Log("There is data to be sent. Batch size: {BatchSize}. newEventQueueCount: {newEventQueueCount}. First event: {Event}."
						, eventBatchToSend.Length, newEventQueueCount, eventBatchToSend.Length > 0 ? eventBatchToSend[0].ToString() : "<N/A>");
			}
			else
			{
				_logger.Trace()
					?.Log("There is no data to send.");
			}
			return eventBatchToSend;
		}

		private void ProcessQueueItems(object[] queueItems)
		{
			// can reuse underlying buffers from a pool in future.
			using var stream = new MemoryStream(1024);

			try
			{
				_cachedMetadataJsonLine ??= _payloadItemSerializer.Serialize(_metadata);

				using (var writer = new StreamWriter(stream, Utf8Encoding, 1024, true))
				{
					writer.Write("{\"metadata\":");
					writer.Write(_cachedMetadataJsonLine);
					writer.Write("}\n");

					foreach (var item in queueItems)
					{
						switch (item)
						{
							case Transaction transaction:
								if (TryExecuteFilter(TransactionFilters, transaction) != null)
									Serialize(item, "transaction", writer);
								break;
							case Span span:
								if (TryExecuteFilter(SpanFilters, span) != null)
									Serialize(item, "span", writer);
								break;
							case Error error:
								if (TryExecuteFilter(ErrorFilters, error) != null)
									Serialize(item, "error", writer);
								break;
							case MetricSet _:
								Serialize(item, "metricset", writer);
								break;
						}
					}
				}

				stream.Position = 0;
				using (var content = new StreamContent(stream))
				{
					content.Headers.ContentType = MediaTypeHeaderValue;

#if NET8_0_OR_GREATER
					HttpResponseMessage response;
					try
					{
						var webRequest = new HttpRequestMessage(HttpMethod.Post, _intakeV2EventsAbsoluteUrl) { Content = content };
						response = HttpClient.Send(webRequest, CancellationTokenSource.Token);
					}
					catch (Exception) //HttpMessageHandler may not support synchronous sending
					{
						response = HttpClient.PostAsync(_intakeV2EventsAbsoluteUrl, content, CancellationTokenSource.Token)
							.ConfigureAwait(false)
							.GetAwaiter()
							.GetResult();
					}
#else
					var response = HttpClient.PostAsync(_intakeV2EventsAbsoluteUrl, content, CancellationTokenSource.Token)
						.ConfigureAwait(false)
						.GetAwaiter()
						.GetResult();
#endif
					// ReSharper disable ConditionIsAlwaysTrueOrFalse
					if (response is null || !response.IsSuccessStatusCode)
					{
						var message = "Unknown 400 Bad Request";
						if (response?.Content != null)
						{
#if NET8_0_OR_GREATER
							var intakeResponse = _payloadItemSerializer.Deserialize<IntakeResponse>(response.Content.ReadAsStream());
#else
							var intakeResponse = _payloadItemSerializer.Deserialize<IntakeResponse>(response.Content.ReadAsStreamAsync().GetAwaiter().GetResult());
#endif
							if (intakeResponse.Errors?.Count > 0)
								message = string.Join(", ", intakeResponse.Errors.Select(e => e.Message).Distinct());
						}
						_logger?.Error()
							?.Log("Failed sending event."
								+ " Events intake API absolute URL: {EventsIntakeAbsoluteUrl}."
								+ " APM Server response: status code: {ApmServerResponseStatusCode}"
								+ ", reasons: {ApmServerResponseContent}"
								, _intakeV2EventsAbsoluteUrl.Sanitize()
								, response?.StatusCode,
								message
								);
					}
					// ReSharper enable ConditionIsAlwaysTrueOrFalse
					else
					{
						_logger?.Debug()
							?.Log("Sent items to server:"
								+ $"{Environment.NewLine}{TextUtils.Indentation}{{SerializedItems}}",
								string.Join($",{Environment.NewLine}{TextUtils.Indentation}", queueItems));
					}
				}
			}
			catch (OperationCanceledException)
			{
				// handle cancellation specifically
				_logger?.Warning()
					?.Log(
						"Cancellation requested. Following events were not transferred successfully to the server ({ApmServerUrl}):"
						+ $"{Environment.NewLine}{TextUtils.Indentation}{{SerializedItems}}"
						, HttpClient.BaseAddress.Sanitize()
						, string.Join($",{Environment.NewLine}{TextUtils.Indentation}", queueItems));

				// throw to allow Workloop to handle
				throw;
			}
			catch (Exception e)
			{
				_logger?.Warning()
					?.LogException(
						e,
						"Failed sending events. Following events were not transferred successfully to the server ({ApmServerUrl}):"
						+ $"{Environment.NewLine}{TextUtils.Indentation}{{SerializedItems}}"
						, HttpClient.BaseAddress.Sanitize()
						, string.Join($",{Environment.NewLine}{TextUtils.Indentation}", queueItems)
					);
			}
		}

		private void Serialize(object item, string eventType, TextWriter writer)
		{
			writer.Write("{\"");
			writer.Write(eventType);
			writer.Write("\":");
			var traceLogger = _logger?.IfLevel(LogLevel.Trace);
			if (traceLogger.HasValue)
			{
				var serialized = _payloadItemSerializer.Serialize(item);
				writer.Write(serialized);
				traceLogger.Value.Log("Serialized item to send: {ItemToSend} as {SerializedItem}", item, serialized);
			}
			else
				_payloadItemSerializer.Serialize(item, writer);

			writer.Write("}\n");
		}

		// Executes filters for the given filter collection and handles return value and errors
		// ReSharper disable once SuggestBaseTypeForParameter
		// internal code, no need to use base type for parameter as ReSharper suggests
		private T TryExecuteFilter<T>(List<Func<T, T>> filters, T item) where T : class
		{
			if (filters.Count == 0)
				return item;

			foreach (var filter in filters)
			{
				try
				{
					var itemAfterFilter = filter(item);
					if (itemAfterFilter != null)
					{
						item = itemAfterFilter;
						continue;
					}

					_logger?.Debug()?.Log("Filter returns false, item won't be sent, {filteredItem}", item);
					return null;
				}
				catch (Exception e)
				{
					_logger.Warning()?.LogException(e, "Exception during execution of the filter on transaction");
				}
			}

			return item;
		}

		public bool AddFilter(Func<ITransaction, ITransaction> transactionFilter)
		{
			if (!_allowFilterAdd)
				return false;

			TransactionFilters.Add(transactionFilter);
			return true;
		}

		public bool AddFilter(Func<ISpan, ISpan> spanFilter)
		{
			if (!_allowFilterAdd)
				return false;

			SpanFilters.Add(spanFilter);
			return true;
		}

		public bool AddFilter(Func<IError, IError> errorFilter)
		{
			if (!_allowFilterAdd)
				return false;

			ErrorFilters.Add(errorFilter);
			return true;
		}
	}
}
