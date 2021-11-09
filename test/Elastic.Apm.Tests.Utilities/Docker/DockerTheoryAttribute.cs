// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using ProcNet;
using Xunit;

namespace Elastic.Apm.Tests.Utilities.Docker
{
	/// <summary>
	/// Test method that should be run only if docker exists on the host
	/// </summary>
	public class DockerTheoryAttribute : TheoryAttribute
	{
		private static readonly string _skip;

		static DockerTheoryAttribute()
		{
			try
			{
				if (TestEnvironment.IsCi && TestEnvironment.IsWindows)
				{
					_skip = "not running tests that require docker in CI on Windows";
					return;
				}

				var result = Proc.Start(new StartArguments("docker", "--version"));
				if (result.ExitCode != 0)
					_skip = "docker not installed";
			}
			catch (Exception)
			{
				_skip = "could not get version of docker";
			}
		}

		public DockerTheoryAttribute() => Skip = _skip;
	}
}
