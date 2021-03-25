// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Elasticsearch
{
	public abstract class ElasticsearchDiagnosticsListenerBase : DiagnosticListenerBase
	{
		protected const string StartSuffix = ".Start";
		protected const string StopSuffix = ".Stop";

		internal readonly ConcurrentDictionary<string, Span> Spans = new ConcurrentDictionary<string, Span>();

		protected ElasticsearchDiagnosticsListenerBase(IApmAgent agent) : base(agent) { }

		protected IObserver<KeyValuePair<string, object>> Observer { get; set; }

		protected override void HandleOnNext(KeyValuePair<string, object> kv)
		{
			Logger.Trace()?.Log("Called with key: `{DiagnosticEventKey}'", kv.Key);
			if (ApmAgent.Tracer.CurrentTransaction == null)
			{
				Logger.Debug()?.Log("No active transaction, skip creating span for outgoing HTTP request");
				return;
			}

			try
			{
				Observer.OnNext(kv);
			}
			catch (Exception e)
			{
				Logger.Error()?.LogException(e, "An exception occured calling OnNext on an ElasticsearchDiagnostic observer");
			}
		}

		internal bool TryStartElasticsearchSpan(string name, out Span span, Uri instanceUri = null)
		{
			span = null;
			var transaction = ApmAgent.Tracer.CurrentTransaction;
			if (transaction == null)
				return false;

			span = (Span)ApmAgent.GetCurrentExecutionSegment()
				.StartSpan(
					name,
					ApiConstants.TypeDb,
					ApiConstants.SubtypeElasticsearch);

			span.Action = name;
			SetDbContext(span, instanceUri);
			SetDestination(span, instanceUri);

			var id = Activity.Current.Id;
			if (Spans.TryAdd(id, span)) return true;

			Logger.Error()?.Log("Failed to register start of span in ConcurrentDictionary {SpanDetails}", span.ToString());
			span = null;
			return false;
		}

		private void SetDestination(Span span, Uri instance)
		{
			if (instance == null)
				return;

			span.Context.Destination = new Destination { Port = instance.Port, Address = instance.Host };
		}

		internal bool TryGetCurrentElasticsearchSpan(out Span span, Uri instance = null)
		{
			var id = Activity.Current.Id;
			if (Spans.TryRemove(id, out span))
			{
				SetDbContext(span, instance);
				return true;
			}

			span = null;
			Logger.Error()?.Log("Failed to find current span in ConcurrentDictionary {ActiviyId}", id);

			return false;
		}

		private static void SetDbContext(ISpan span, Uri instance)
		{
			var instanceUriString = instance?.ToString();
			if (span.Context.Db?.Instance != null || instanceUriString == null) return;

			span.Context.Db = new Database { Instance = instanceUriString, Type = Database.TypeElasticsearch };
		}
	}
}
