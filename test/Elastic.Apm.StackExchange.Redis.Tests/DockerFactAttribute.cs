// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using ProcNet;
using Xunit;

namespace Elastic.Apm.StackExchange.Redis.Tests
{
	/// <summary>
	/// Test method that should be run only if docker exists on the host
	/// </summary>
	public class DockerFactAttribute : FactAttribute
	{
		public DockerFactAttribute()
		{
			try
			{
				var result = Proc.Start(new StartArguments("docker", "--version"));
				if (result.ExitCode != 0)
					Skip = "docker not installed";
			}
			catch (Exception)
			{
				Skip = "could not get version of docker";
			}
		}
	}
}
