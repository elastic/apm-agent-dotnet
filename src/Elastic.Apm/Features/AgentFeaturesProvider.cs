// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Features
{
	internal static class AgentFeaturesProvider
	{
		private static AgentFeatures AgentFeatures;

		/// <summary>
		/// Returns the agent feature set based on the current habitat (e.g. Azure, AWS Lambda, ...).
		/// </summary>
		/// <returns>The agent features.</returns>
		internal static AgentFeatures Get(IApmLogger logger, IEnvironmentVariables environmentVariables = null)
		{
			var current = Volatile.Read(ref AgentFeatures);
			if (current != null)
				return current;

			// Publish with a compare-exchange so a concurrent caller cannot overwrite a feature set that was already
			// established, which would otherwise happen when one thread passes the null check and another assigns
			// before it writes.
			var created = Create(logger, environmentVariables);
			var published = Interlocked.CompareExchange(ref AgentFeatures, created, null);
			if (published != null)
				return published;

			logger?.Trace()?.Log("[Agent Features] Using '{AgentFeaturesName}' feature set", created.Name);
			return created;
		}

		/// <summary>
		/// Determines the agent feature set for the given environment without reading or populating the cached feature set.
		/// </summary>
		/// <returns>The agent features.</returns>
		internal static AgentFeatures Create(IApmLogger logger, IEnvironmentVariables environmentVariables = null)
		{
			environmentVariables ??= new EnvironmentVariables(logger);
			return environmentVariables.GetEnvironmentVariables().Contains("FUNCTIONS_WORKER_RUNTIME")
				? new AzureFunctionsAgentFeatures(logger)
				: new DefaultAgentFeatures(logger);
		}

		/// <summary>
		/// Meant for testing purposes only.
		/// </summary>
		/// <param name="agentFeatures">The agent features.</param>
		internal static void Set(AgentFeatures agentFeatures) => Volatile.Write(ref AgentFeatures, agentFeatures);
	}
}
