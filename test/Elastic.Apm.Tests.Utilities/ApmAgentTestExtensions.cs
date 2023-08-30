// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;

namespace Elastic.Apm.Tests.Utilities;

public static class ApmAgentTestExtensions
{
	public static HashSet<Type> SubscribedListeners(this IApmAgent agent) => agent.SubscribedListeners;
}
