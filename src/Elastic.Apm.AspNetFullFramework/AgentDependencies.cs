// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework
{
	/// <summary>
	/// Dependencies to initialize the APM agent with
	/// </summary>
	public static class AgentDependencies
	{
		/// <summary>
		/// The logger
		/// </summary>
		public static IApmLogger Logger { get; set; }
	}
}
