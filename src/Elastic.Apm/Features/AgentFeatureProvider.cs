// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Features
{
	internal static class AgentFeatureProvider
	{
		/// <summary>
		/// Returns the agent feature set based on the current habitat (e.g. Azure, AWS Lambda, ...).
		/// </summary>
		/// <returns>The features.</returns>
		internal static AgentFeatures Get(IApmLogger logger)
		{
			var agentFeatures = new AgentFeatures(logger);
			if (Environment.GetEnvironmentVariable("foo") == "bar")
			{
				agentFeatures.ToString();
			}
			return agentFeatures;
		}
	}
}
