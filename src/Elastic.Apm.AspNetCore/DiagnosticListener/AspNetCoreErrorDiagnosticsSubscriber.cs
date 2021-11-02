﻿// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	/// <summary>
	/// Activates the <see cref="AspNetCoreErrorDiagnosticListener" /> which enables
	/// capturing errors within an ASP.NET Core application.
	/// This is used in combination with <see cref="ApmMiddleware"/> to capture errors handled by error pages
	/// </summary>
	public class AspNetCoreErrorDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Start listening for ASP.NET Core related diagnostic source events.
		/// </summary>
		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();
			if (!agent.ConfigurationReader.Enabled)
				return retVal;

			var subscriber = new DiagnosticInitializer(agent, new AspNetCoreErrorDiagnosticListener(agent));
			retVal.Add(subscriber);

			retVal.Add(System.Diagnostics.DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return retVal;
		}
	}
}
