using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Elasticsearch
{
	public abstract class ElasticsearchDiagnosticsListenerBase : IDiagnosticListener
	{
		protected ElasticsearchDiagnosticsListenerBase(IApmAgent agent, string name)
		{
			Name = name;
			Logger = agent.Logger.Scoped(GetType().Name);
		}

		public string Name { get; }

		protected IObserver<KeyValuePair<string, object>> Observer { get; set; }

		protected IApmLogger Logger { get; }

		void IObserver<KeyValuePair<string, object>>.OnCompleted() => Observer.OnCompleted();

		void IObserver<KeyValuePair<string, object>>.OnError(Exception error) => Observer.OnError(error);

		void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> value) => Observer.OnNext(value);

		internal readonly ConcurrentDictionary<string, Span> Spans = new ConcurrentDictionary<string, Span>();
	}
}
