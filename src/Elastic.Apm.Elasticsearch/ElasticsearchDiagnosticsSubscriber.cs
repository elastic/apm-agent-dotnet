using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.Elasticsearch
{
	/// <summary>
	/// Manages the Entity Framework Core listener, which listens to EF Core events
	/// </summary>
	public class ElasticsearchDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Start listening for EF Core <see cref="DiagnosticSource"/> events
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
