// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.EntityFrameworkCore
{
	/// <summary>
	/// Manages the Entity Framework Core listener, which listens to EF Core events
	/// </summary>
	public class EfCoreDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Start listening for EF Core <see cref="DiagnosticSource"/> events
		/// </summary>
		public IDisposable Subscribe(IApmAgent agentComponents)
		{
			var retVal = new CompositeDisposable();
			if (!agentComponents.ConfigurationReader.Enabled)
				return retVal;

			var subscriber = new DiagnosticInitializer(agentComponents.Logger, new EfCoreDiagnosticListener(agentComponents));
			retVal.Add(subscriber);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return retVal;
		}
	}
}
