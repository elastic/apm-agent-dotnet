// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Linq;

namespace Elastic.Apm.Tests.Utilities
{
	public static class SolutionPaths
	{
		private static readonly Lazy<string> _root = new Lazy<string>(FindSolutionRoot);

		private static readonly Lazy<string> _agentZip = new Lazy<string>(FindVersionedAgentZip);
		private static string FindSolutionRoot()
		{
			var solutionFileName = "ElasticApmAgent.sln";
			var currentDirectory = Directory.GetCurrentDirectory();
			var candidateDirectory = new DirectoryInfo(currentDirectory);
			do
			{
				if (File.Exists(Path.Combine(candidateDirectory.FullName, solutionFileName)))
					return candidateDirectory.FullName;

				candidateDirectory = candidateDirectory.Parent;
			} while (candidateDirectory != null);

			throw new InvalidOperationException($"Could not find solution root directory from the current directory `{currentDirectory}'");
		}

		private static string FindVersionedAgentZip()
		{
			var buildOutputDir = Path.Combine(Root, "build/output");
			if (!Directory.Exists(buildOutputDir))
			{
				throw new DirectoryNotFoundException(
					$"build output directory does not exist at {buildOutputDir}. "
					+ $"Run the build script in the solution root with agent-zip target to build");
			}

			var agentZip = Directory.EnumerateFiles(buildOutputDir, "ElasticApmAgent_*.zip", SearchOption.TopDirectoryOnly)
				.FirstOrDefault();

			if (agentZip is null)
			{
				throw new FileNotFoundException($"ElasticApmAgent_*.zip file not found in {buildOutputDir}. "
					+ $"Run the build script in the solution root with agent-zip target to build");
			}

			return agentZip;
		}

		public static string Root => _root.Value;
		public static string AgentZip => _agentZip.Value;
	}
}
