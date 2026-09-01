// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Instrumentations.SqlClient
{
	/// <summary>
	/// Subscribes to diagnostics events from System.Data.SqlClient and Microsoft.Data.SqlClient
	/// </summary>
	public class SqlClientDiagnosticSubscriber : IDiagnosticsSubscriber
	{
		private readonly PendingSpanStore _spanStore;

		/// <summary>
		/// Creates a new SQL Client diagnostic subscriber.
		/// </summary>
		public SqlClientDiagnosticSubscriber() { }

		internal SqlClientDiagnosticSubscriber(PendingSpanStore spanStore) => _spanStore = spanStore;

		/// <inheritdoc />
		public IDisposable Subscribe(IApmAgent agentComponents)
		{
			var retVal = new CompositeDisposable();

			if (!agentComponents.Configuration.Enabled)
				return retVal;

			if (PlatformDetection.IsDotNetCore || PlatformDetection.IsDotNet)
			{
				var listener = new SqlClientDiagnosticListener(agentComponents, _spanStore);
				var initializer = new DiagnosticInitializer(agentComponents, listener);

				retVal.Add(initializer);

				retVal.Add(DiagnosticListener
					.AllListeners
					.Subscribe(initializer));
				retVal.Add(listener);
			}
			else
				retVal.Add(new SqlEventListener(agentComponents));

			return retVal;
		}
	}
}
