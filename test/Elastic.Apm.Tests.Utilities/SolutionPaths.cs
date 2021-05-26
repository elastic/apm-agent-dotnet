// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;

namespace Elastic.Apm.Tests.Utilities
{
	public static class SolutionPaths
	{
		private static readonly Lazy<string> _root = new Lazy<string>(FindSolutionRoot);
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

		/// <summary>
		/// The full path to the solution root
		/// </summary>
		public static string Root => _root.Value;
	}
}
