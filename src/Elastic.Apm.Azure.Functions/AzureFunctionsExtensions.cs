// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

<<<<<<< HEAD
using System.Collections.Generic;
=======
using System;
using System.Collections.Generic;
using System.Diagnostics;
>>>>>>> 147b47f39da7635500741760c827e90c219ed9dc
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
