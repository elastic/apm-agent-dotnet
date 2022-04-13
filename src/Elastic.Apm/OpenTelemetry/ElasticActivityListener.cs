// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if NET5_0 || NET6_0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.OpenTelemetry
{
	public class ElasticActivityListener : IDisposable
	{
		private readonly ConcurrentDictionary<string, Span> ActiveSpans = new();
		private readonly ConcurrentDictionary<string, Transaction> ActiveTransactions = new();

		internal ElasticActivityListener(IApmAgent agent) => _logger = agent.Logger?.Scoped(nameof(ElasticActivityListener));

		private readonly IApmLogger _logger;
		private Tracer _tracer;

		internal void Start(Tracer tracerInternal)
		{
			_tracer = tracerInternal;
			Listener = new ActivityListener
			{
				ActivityStarted = ActivityStarted,
				ActivityStopped = ActivityStopped,
				ShouldListenTo = _ => true,
				Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
			};
			ActivitySource.AddActivityListener(Listener);
		}

		private Action<Activity> ActivityStarted =>
			activity =>
			{
				_logger.Trace()?.Log($"ActivityStarted: name:{activity.DisplayName} id:{activity.Id} traceId:{activity.TraceId}");

				if (KnownListeners.KnownListenersList.Contains(activity.DisplayName))
					return;

				Transaction transaction = null;
				if (activity?.Context != null && activity.ParentId != null && _tracer.CurrentTransaction == null)
				{
					var dt = TraceContext.TryExtractTracingData(activity.ParentId.ToString(), activity.Context.TraceState);

					transaction = _tracer.StartTransactionInternal(activity.DisplayName, "unknown",
						TimeUtils.ToTimestamp(activity.StartTimeUtc), true, activity.SpanId.ToString(),
						distributedTracingData: dt);
				}
				else if (activity.ParentId == null)
				{
					transaction = _tracer.StartTransactionInternal(activity.DisplayName, "unknown",
						TimeUtils.ToTimestamp(activity.StartTimeUtc), true, activity.SpanId.ToString(),
						activity.TraceId.ToString());
				}
				else
				{
					Span newSpan;
					if (_tracer.CurrentSpan == null)
					{
						newSpan = (_tracer.CurrentTransaction as Transaction)?.StartSpanInternal(activity.DisplayName, "unknown",
							timestamp: TimeUtils.ToTimestamp(activity.StartTimeUtc), id: activity.SpanId.ToString());
					}
					else
					{
						newSpan = (_tracer.CurrentSpan as Span)?.StartSpanInternal(activity.DisplayName, "unknown",
							timestamp: TimeUtils.ToTimestamp(activity.StartTimeUtc), id: activity.SpanId.ToString());
					}

					if (newSpan != null)
					{
						newSpan.Otel = new OTel
						{
							SpanKind = activity.Kind.ToString()
						};

						if (activity.Kind == ActivityKind.Internal)
						{
							newSpan.Type = "app";
							newSpan.Subtype = "internal";
						}

						if (activity.Id != null)
							ActiveSpans.TryAdd(activity.Id, newSpan);
					}

				}

				if (transaction != null)
				{
					transaction.Otel = new OTel
					{
						SpanKind = activity.Kind.ToString()
					};

					//if (activity.Kind == ActivityKind.Server)
					//	transaction.Type = ApiConstants.TypeRequest;
					//else if (activity.Kind == ActivityKind.Consumer) transaction.Type = ApiConstants.TypeMessaging;

					if (activity.Id != null)
						ActiveTransactions.TryAdd(activity.Id, transaction);
				}
			};


		private Action<Activity> ActivityStopped =>
			activity =>
			{
				if (activity == null)
				{
					_logger.Trace()?.Log("ActivityStopped called with `null` activity. Ignoring `null` activity.");
					return;
				}

				_logger.Trace()?.Log($"ActivityStopped: name:{activity.DisplayName} id:{activity.Id} traceId:{activity.TraceId}");

				if (KnownListeners.KnownListenersList.Contains(activity.DisplayName))
					return;

				if (activity.Id != null)
				{
					if (ActiveTransactions.TryRemove(activity.Id, out var transaction))
					{
						transaction.Duration = activity.Duration.TotalMilliseconds;

						if (activity.Tags.Any())
							transaction.Otel.Attributes = new Dictionary<string, string>();

						foreach (var tag in activity.Tags)
							transaction.Otel.Attributes.Add(tag.Key, tag.Value);

						InferTransactionType(transaction, activity);

						// By default we set unknown outcome
						transaction.Outcome = Outcome.Unknown;
#if NET6_0_OR_GREATER
						switch (activity.Status)
						{
							case ActivityStatusCode.Unset:
								transaction.Outcome = Outcome.Unknown;
								break;
							case ActivityStatusCode.Ok:
								transaction.Outcome = Outcome.Success;
								break;
							case ActivityStatusCode.Error:
								transaction.Outcome = Outcome.Failure;
								break;
							default:
								break;
						}
#endif

						transaction.End();
					}
					else if (ActiveSpans.TryRemove(activity.Id, out var span))
					{
						span.Duration = activity.Duration.TotalMilliseconds;

						if (activity.Tags.Any())
							span.Otel.Attributes = new Dictionary<string, string>();

						foreach (var tag in activity.Tags)
							span.Otel.Attributes.Add(tag.Key, tag.Value);

						InferSpanTypeAndSubType(span, activity);

						// By default we set unknown outcome
						span.Outcome = Outcome.Unknown;
#if NET6_0_OR_GREATER
						switch (activity.Status)
						{
							case ActivityStatusCode.Unset:
								span.Outcome = Outcome.Unknown;
								break;
							case ActivityStatusCode.Ok:
								span.Outcome = Outcome.Success;
								break;
							case ActivityStatusCode.Error:
								span.Outcome = Outcome.Failure;
								break;
							default:
								break;
						}
#endif
						span.End();
					}
				}
			};

		private void InferTransactionType(Transaction transaction, Activity activity)
		{

			var isRpc = activity.Tags.Any(n => n.Key == "rpc.system");
			var isHttp = activity.Tags.Any(n => n.Key == "http.url" || n.Key == "http.scheme");
			var isMessaging = activity.Tags.Any(n => n.Key == "messaging.system");

			if (activity.Kind == ActivityKind.Server && (isRpc || isHttp))
			{
				transaction.Type = ApiConstants.TypeRequest;
			}
			else if (activity.Kind == ActivityKind.Consumer && isMessaging)
			{
				transaction.Type = ApiConstants.TypeMessaging;
			}
			else
			{
				transaction.Type = "unknown";
			}
		}

		private ActivityListener Listener { get; set; }

		private void InferSpanTypeAndSubType(Span span, Activity activity)
		{

			string httpPortFromScheme(string scheme)
			{
				if (scheme == "http")
				{
					return "80";
				}
				else if (scheme == "https")
				{
					return "443";
				}

				return string.Empty;
			}

			// extracts 'host' or 'host:port' from URL
			string parseNetName(string url)
			{
				try
				{
					var u = new Uri(url); // https://developer.mozilla.org/en-US/docs/Web/API/URL
					return u.Host + ':' + u.Port;

				}
				catch
				{
					return string.Empty;
				}
			}

			var peerPort = "";
			var netName = "";

			if (activity.Tags.Any(n => n.Key == "net.peer.port"))
			{
				peerPort = activity.Tags.Where(n => n.Key == "net.peer.port").FirstOrDefault().Value;
			}

			if (activity.Tags.Any(n => n.Key == "net.peer.ip"))
			{
				netName = activity.Tags.Where(n => n.Key == "net.peer.ip").FirstOrDefault().Value;
			}
			if (activity.Tags.Any(n => n.Key == "net.peer.name"))
			{
				netName = activity.Tags.Where(n => n.Key == "net.peer.name").FirstOrDefault().Value;
			}

			if (netName.Length > 0 && peerPort.Length > 0)
			{
				netName += ':';
				netName += peerPort;
			}


			if (activity.Kind == ActivityKind.Client)
			{
				if (activity.Tags.Any(n => n.Key == "http.url" || n.Key == "http.scheme"))
				{
					span.Type = ApiConstants.TypeExternal;
					span.Subtype = ApiConstants.SubtypeHttp;

					if (activity.Tags.Any(n => n.Key == "http.host") && activity.Tags.Any(n => n.Key == "http.scheme"))
					{
						span.Context.Destination = new Destination
						{
							Service = new Destination.DestinationService
							{
								Resource = activity.Tags.FirstOrDefault(n => n.Key == "http.host").Value + ":" + httpPortFromScheme(activity.Tags.FirstOrDefault(n => n.Key == "http.scheme").Value)
							}
						};

					}
					else if (activity.Tags.Any(n => n.Key == "http.url"))
					{
						span.Context.Destination = new Destination
						{
							Service = new Destination.DestinationService
							{
								Resource = parseNetName(activity.Tags.FirstOrDefault(n => n.Key == "http.url").Value)
							}
						};
					}
					else
					{
						span.Context.Destination = new Destination
						{
							Service = new Destination.DestinationService { Resource = !string.IsNullOrEmpty(netName) ? netName : span.Subtype }
						};
					}
				}
				else if (activity.Tags.Any(n => n.Key == "db.system"))
				{
					span.Type = ApiConstants.TypeDb;
					span.Subtype = activity.Tags.First(n => n.Key == "db.system").Value;

					span.Context.Destination = new Destination
					{
						Service = new Destination.DestinationService { Resource = !string.IsNullOrEmpty(netName) ? netName : span.Subtype }
					};
					if (activity.Tags.Any(n => n.Key == "db.name"))
					{
						span.Context.Destination.Service.Resource += '/';
						span.Context.Destination.Service.Resource += activity.Tags.FirstOrDefault(n => n.Key == "db.name").Value;
					}
				}
				else if (activity.Tags.Any(n => n.Key == "rpc.system"))
				{
					span.Type = ApiConstants.TypeExternal;
					span.Subtype = activity.Tags.First(n => n.Key == "rpc.system").Value;

					span.Context.Destination = new Destination
					{
						Service = new Destination.DestinationService { Resource = !string.IsNullOrEmpty(netName) ? netName : span.Subtype }
					};

					if (activity.Tags.Any(n => n.Key == "rpc.service"))
					{
						span.Context.Destination.Service.Resource += "/";
						span.Context.Destination.Service.Resource += activity.Tags.FirstOrDefault(n => n.Key == "rpc.service").Value;
					}
				}
				else if (activity.Kind == ActivityKind.Client)
				{
					if (activity.Tags.Any(n => n.Key == "messaging.system"))
					{
						span.Subtype = activity.Tags.First(n => n.Key == "messaging.system").Value;
						span.Type = ApiConstants.TypeMessaging;
					}
				}
			}
			else if (activity.Kind == ActivityKind.Consumer)
			{
				if (activity.Tags.Any(n => n.Key == "messaging.system"))
				{
					span.Type = ApiConstants.TypeMessaging;
					span.Subtype = activity.Tags.First(n => n.Key == "messaging.system").Value;
				}
			}
		}

		public void Dispose() => Listener?.Dispose();
	}
}

#endif
