using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Elasticsearch
{
	public abstract class ElasticsearchDiagnosticsListenerBase : IDiagnosticListener
	{
		private readonly IApmAgent _agent;
		protected ElasticsearchDiagnosticsListenerBase(IApmAgent agent, string name)
		{
			_agent = agent;
			Name = name;
			Logger = agent.Logger.Scoped(GetType().Name);
		}

		protected const string StartSuffix = ".Start";
		protected const string StopSuffix = ".Stop";

		internal readonly ConcurrentDictionary<string, Span> Spans = new ConcurrentDictionary<string, Span>();

		public string Name { get; }

		protected IObserver<KeyValuePair<string, object>> Observer { get; set; }

		protected IApmLogger Logger { get; }

		void IObserver<KeyValuePair<string, object>>.OnCompleted() => Observer.OnCompleted();

		void IObserver<KeyValuePair<string, object>>.OnError(Exception error) => Observer.OnError(error);

		void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> kv)
		{
			Logger.Trace()?.Log("Called with key: `{DiagnosticEventKey}'", kv.Key);
			if (_agent.Tracer.CurrentTransaction == null)
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

		internal bool TryStartElasticsearchSpan(string name, out Span span, string instance = null)
		{
			span = null;
			var transaction = _agent.Tracer.CurrentTransaction;
			if (transaction == null)
				return false;

			span = (Span)ExecutionSegmentCommon.GetCurrentExecutionSegment(_agent).StartSpan(
				name,
				ApiConstants.TypeDb,
				ApiConstants.SubtypeElasticsearch);

			span.Action = name;
			SetDbContext(span, instance);

			var id = Activity.Current.Id;
			if (Spans.TryAdd(id, span)) return true;

			Logger.Error()?.Log("Failed to register start of span in ConcurrentDictionary {SpanDetails}", span.ToString());
			span = null;
			return false;
		}

		internal bool TryGetCurrentElasticsearchSpan(out Span span, string instance = null)
		{
			var id = Activity.Current.Id;
			if (Spans.TryRemove(id, out span))
			{
				SetDbContext(span, instance);
				return true;
			}

			span = null;
			Logger.Error()?.Log("Failed to find current span in ConcurrentDictionary {ActiviyId}",id);

			return false;
		}

		private static void SetDbContext(ISpan span, string instance)
		{
			if (span.Context.Db?.Instance != null || instance == null) return;

			span.Context.Db = new Database
			{
				Instance = instance,
				Type = Database.TypeElasticsearch,
			};
		}
	}
}
