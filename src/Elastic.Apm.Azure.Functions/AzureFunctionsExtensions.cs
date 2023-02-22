// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.DiagnosticSource;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

namespace Elastic.Apm.Azure.Functions
{
	public static class AzureFunctionsExtensions
	{
		public static IFunctionsHostBuilder AddElasticApm(this IFunctionsHostBuilder builder)
		{
			AddElasticApm(Agent.Instance, null);
			return builder;
		}

		private static void AddElasticApm(ApmAgent agent, IDiagnosticsSubscriber[]? subscribers)
		{
			var subs = subscribers?.ToList() ?? new List<IDiagnosticsSubscriber>(1);
			if (subs.Count == 0 || subs.All(s => s.GetType() != typeof(AzureFunctionsDiagnosticSubscriber)))
				subs.Add(new AzureFunctionsDiagnosticSubscriber());
			agent.Subscribe(subs.ToArray());
		}
	}
}
