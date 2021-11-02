// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.SqlClient
{
	/// <summary>
	/// Subscribes to diagnostics events from System.Data.SqlClient and Microsoft.Data.SqlClient
	/// </summary>
	public class SqlClientDiagnosticSubscriber : IDiagnosticsSubscriber
	{
		/// <inheritdoc />
		public IDisposable Subscribe(IApmAgent agentComponents)
		{
			var retVal = new CompositeDisposable();

			if (!agentComponents.ConfigurationReader.Enabled)
				return retVal;

			if (PlatformDetection.IsDotNetCore || PlatformDetection.IsDotNet5)
			{
				var initializer = new DiagnosticInitializer(agentComponents, new SqlClientDiagnosticListener(agentComponents));

				retVal.Add(initializer);

				retVal.Add(DiagnosticListener
					.AllListeners
					.Subscribe(initializer));
			}
			else
				retVal.Add(new SqlEventListener(agentComponents));

			return retVal;
		}
	}
}
