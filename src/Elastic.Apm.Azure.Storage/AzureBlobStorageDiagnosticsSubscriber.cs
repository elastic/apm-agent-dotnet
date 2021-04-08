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

			var initializer = new DiagnosticInitializer(agent.Logger, new[] { new AzureBlobStorageDiagnosticListener(agent) });
			retVal.Add(initializer);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(initializer));

			if (agent is ApmAgent realAgent)
			{
				if (realAgent.HttpDiagnosticListener is null)
					realAgent.Subscribe(new HttpDiagnosticsSubscriber(false));

				realAgent.HttpDiagnosticListener.AddCreator(new MicrosoftAzureBlobStorageCreator());
			}

			return retVal;
		}
	}
}
