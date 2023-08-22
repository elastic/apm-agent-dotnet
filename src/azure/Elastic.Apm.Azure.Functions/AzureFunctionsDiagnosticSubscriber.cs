// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Azure.Functions;

public class AzureFunctionsDiagnosticSubscriber : IDiagnosticsSubscriber
{
	public IDisposable Subscribe(IApmAgent agent)
	{
		agent.Logger.Debug()?.Log($"{nameof(AzureFunctionsDiagnosticSubscriber)} starting to subscribe");

		var retVal = new CompositeDisposable();
		if (!agent.Configuration.Enabled)
			return retVal;

		if (agent is not ApmAgent apmAgent)
		{
			agent.Logger.Warning()?.Log($"'{agent}' is not an instance of ApmAgent");
			return retVal;
		}

		var subscriber = new DiagnosticInitializer(agent, new AzureFunctionsDiagnosticListener(apmAgent));
		retVal.Add(subscriber);

		retVal.Add(System.Diagnostics.DiagnosticListener
			.AllListeners
			.Subscribe(subscriber));

		agent.Logger.Debug()?.Log($"{nameof(AzureFunctionsDiagnosticSubscriber)} subscribed");

		return retVal;
	}
}
