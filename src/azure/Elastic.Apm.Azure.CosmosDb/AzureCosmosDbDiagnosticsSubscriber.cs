// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.Azure.CosmosDb
{
	/// <summary>
	/// Subscribes to HTTP requests from Microsoft.Azure.Cosmos, Microsoft.Azure.DocumentDb, and Microsoft.Azure.DocumentDb.Core.
	/// </summary>
	public class AzureCosmosDbDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Subscribes to HTTP requests
		/// </summary>
		public IDisposable Subscribe(IApmAgent agent)
		{
			if (agent is ApmAgent realAgent)
			{
				realAgent.HttpTraceConfiguration.AddTracer(new AzureCosmosDbTracer());

				if (!realAgent.HttpTraceConfiguration.Subscribed)
					realAgent.Subscribe(new HttpDiagnosticsSubscriber(false));
			}

			return EmptyDisposable.Instance;
		}
	}
}
