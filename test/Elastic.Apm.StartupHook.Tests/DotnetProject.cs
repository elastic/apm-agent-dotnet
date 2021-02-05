// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using ProcNet;
using ProcNet.Std;

namespace Elastic.Apm.StartupHook.Tests
{
	public class DotnetProject : IDisposable
	{
		private ObservableProcess _process;

		private DotnetProject(string name, string template, string directory)
		{
			Name = name;
			Template = template;
			Directory = directory;
		}

		public string Directory { get; }

		public string Template { get; }

		public string Name { get; }

		/// <summary>
		/// Creates a process to run the dotnet project that will start when subscribed to.
		/// </summary>
		/// <param name="startupHookZipPath">The path to the startup hook zip file</param>
		/// <param name="environmentVariables">The environment variables to start the project with. The DOTNET_STARTUP_HOOKS environment variable will be added.</param>
		/// <returns></returns>
		public ObservableProcess CreateProcess(string startupHookZipPath, IDictionary<string, string> environmentVariables = null)
		{
			var startupHookAssembly = UnzipStartupHook(startupHookZipPath);
			environmentVariables ??= new Dictionary<string, string>();
			environmentVariables["DOTNET_STARTUP_HOOKS"] = startupHookAssembly;
			var arguments = new StartArguments("dotnet", "run")
			{
				WorkingDirectory = Directory,
				SendControlCFirst = true,
				Environment = environmentVariables
			};

			_process = new ObservableProcess(arguments);
			return _process;
		}

		/// <summary>
		/// Unzips the agent startup hook zip file to the temp directory, and returns
		/// the path to the startup hook assembly.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException">
		///	Startup hook assembly not found in the extracted files.
		/// </exception>
		private string UnzipStartupHook(string startupHookZipPath)
		{
			var tempDirectory = Path.GetTempPath();
			var destination = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

			ZipFile.ExtractToDirectory(startupHookZipPath, destination);
			var startupHookAssembly = Path.Combine(destination, "ElasticApmAgentStartupHook.dll");

			if (!File.Exists(startupHookAssembly))
				throw new FileNotFoundException($"startup hook assembly does not exist at {startupHookAssembly}", startupHookAssembly);

			return startupHookAssembly;
		}

		public void Stop()
		{
			if (_process?.ProcessId != null)
			{
				_process.SendControlC();
				_process.Dispose();
			}
		}

		public void Dispose() => Stop();


		public static DotnetProject Create(string name, string template, params string[] arguments)
		{
			var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

			var args = new[]
			{
				"new", template,
				"--name", name,
				"--output", directory
			}.Concat(arguments);

			var result = Proc.Start(new StartArguments("dotnet", args));

			if (result.Completed)
			{
				if (!result.ExitCode.HasValue || result.ExitCode != 0)
				{
					throw new Exception($"Creating new dotnet project did not exit successfully. "
						+ $"exit code: {result.ExitCode}, "
						+ $"output: {string.Join(Environment.NewLine, result.ConsoleOut.Select(c => c.Line))}");
				}
			}
			else
			{
				throw new Exception($"Creating new dotnet project did not complete. "
					+ $"exit code: {result.ExitCode}, "
					+ $"output: {string.Join(Environment.NewLine, result.ConsoleOut.Select(c => c.Line))}");
			}

			return new DotnetProject(name, template, directory);
		}
	}
}
