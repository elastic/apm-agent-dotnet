// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Elastic.Apm.Perf.Tests.Helpers
{
	/// <summary>
	/// A helper class to get git related info about the git repo where the benchmark is running.
	/// </summary>
	public class GitInfo : IDisposable
	{
		private readonly Process _gitProcess;

		public GitInfo()
		{
			var processInfo = new ProcessStartInfo
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true,
				FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "git.exe" : "git",
				WorkingDirectory = Environment.CurrentDirectory
			};

			_gitProcess = new Process();
			_gitProcess.StartInfo = processInfo;
		}

		public string BranchName => RunCommand("rev-parse --abbrev-ref HEAD");

		public string CommitHash => RunCommand("rev-parse HEAD");


		private string RunCommand(string args)
		{
			_gitProcess.StartInfo.Arguments = args;
			_gitProcess.Start();
			var output = _gitProcess.StandardOutput.ReadToEnd().Trim();
			_gitProcess.WaitForExit();
			return output;
		}

		public void Dispose() => _gitProcess?.Dispose();
	}
}
