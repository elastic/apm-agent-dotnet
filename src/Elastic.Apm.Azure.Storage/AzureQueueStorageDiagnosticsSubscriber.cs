// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.Azure.Storage
{
	/// <summary>
	/// Subscribes to diagnostic source events from Azure.Storage.Queues
	/// </summary>
	public class AzureQueueStorageDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Subscribes diagnostic source events.
		/// </summary>
		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();
			var initializer = new DiagnosticInitializer(agent.Logger, new IDiagnosticListener[]
				{
					new AzureQueueStorageDiagnosticListener(agent),
					new AzureCoreDiagnosticListener(agent)
				});

			retVal.Add(initializer);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(initializer));

			return retVal;
		}
	}
}
