// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Features;

namespace Elastic.Apm.Tests.Utilities;

internal class AgentFeaturesProviderScope : IDisposable
{
	internal AgentFeaturesProviderScope(AgentFeatures agentFeatures = null) => AgentFeaturesProvider.Set(agentFeatures);
	public void Dispose() => AgentFeaturesProvider.Set(null);
}


