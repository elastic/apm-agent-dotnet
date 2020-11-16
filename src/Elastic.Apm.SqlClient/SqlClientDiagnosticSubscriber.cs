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
	public class SqlClientDiagnosticSubscriber : DiagnosticsSubscriberBase
	{
		/// <inheritdoc />
		protected override IDisposable Subscribe(IApmAgent agentComponents, ICompositeDisposable disposable)
		{
			if (PlatformDetection.IsDotNetCore || PlatformDetection.IsDotNet5)
			{
				var initializer = new DiagnosticInitializer(agentComponents.Logger, new[] { new SqlClientDiagnosticListener(agentComponents) });

				disposable.Add(initializer);

				disposable.Add(DiagnosticListener
					.AllListeners
					.Subscribe(initializer));
			}
			else
				disposable.Add(new SqlEventListener(agentComponents));

			return disposable;
		}
	}
}
