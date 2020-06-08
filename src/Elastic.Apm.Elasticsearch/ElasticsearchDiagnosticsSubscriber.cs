using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.Elasticsearch
{
	/// <summary>
	/// Registers for listeners from the elasticsearch client. 
	/// </summary>
	public class ElasticsearchDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Start listening for elasticsearch <see cref="DiagnosticSource"/> events
		/// </summary>
		public IDisposable Subscribe(IApmAgent agentComponents)
		{
			var composite = new CompositeDisposable();
			var subscriber = new DiagnosticInitializer(agentComponents.Logger, new IDiagnosticListener[]
			{
				new AuditDiagnosticsListener(agentComponents),
				new RequestPipelineDiagnosticsListener(agentComponents),
				new HttpConnectionDiagnosticsListener(agentComponents),
				new SerializerDiagnosticsListener(agentComponents),
			});
			composite.Add(subscriber);

			composite.Add(DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return composite;
		}
	}
}
