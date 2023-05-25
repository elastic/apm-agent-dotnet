// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework
{
	/// <summary>
	/// Agent components for ASP.NET Full Framework
	/// </summary>
	internal class FullFrameworkAgentComponents : AgentComponents
	{
		public FullFrameworkAgentComponents(
			IApmLogger logger,
			IConfigurationReader configurationReader) : base(
				logger,
				configurationReader,
				null,
				null,
				new HttpContextCurrentExecutionSegmentsContainer(),
				null,
				null)
		{
		}
	}
}
