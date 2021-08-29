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
	/// Subscribes to diagnostic source events from Azure.Storage.Blobs and Microsoft.Azure.Storage.Blob
	/// </summary>
	public class AzureBlobStorageDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Subscribes diagnostic source events.
		/// </summary>
		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();
			var initializer = new DiagnosticInitializer(agent.Logger, new IDiagnosticListener[]
				{
					new AzureBlobStorageDiagnosticListener(agent),
					new AzureCoreDiagnosticListener(agent)
				});

			retVal.Add(initializer);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(initializer));

			if (agent is ApmAgent realAgent)
			{
				realAgent.HttpTraceConfiguration.AddTracer(new MicrosoftAzureBlobStorageTracer());

				if (!realAgent.HttpTraceConfiguration.Subscribed)
					retVal.Add(realAgent.Subscribe(new HttpDiagnosticsSubscriber(false)));
			}

			return retVal;
		}
	}
}
