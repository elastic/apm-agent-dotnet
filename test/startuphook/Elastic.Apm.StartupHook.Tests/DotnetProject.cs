// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using ProcNet;
using ProcNet.Std;
using Xunit.Abstractions;

namespace Elastic.Apm.StartupHook.Tests
{
	/// <summary>
	/// Creates new dotnet projects from templates, providing the ability to run them
	/// </summary>
	public class DotnetProject : IDisposable
	{
		private readonly string _publishDirectory = Path.Combine("bin", "Publish");
		private readonly ITestOutputHelper _output;
		private ObservableProcess _process;

		private DotnetProject(string name, string template, string framework, string directory, ITestOutputHelper output)
		{
			Name = name;
			Template = template;
			Framework = framework;
			Directory = directory;
			_output = output;
		}

		/// <summary>
		/// The directory in which the project is created
		/// </summary>
		public string Directory { get; }

		/// <summary>
		/// The dotnet template used to create the project
		/// </summary>
		public string Template { get; }

		/// <summary>
		/// The project target framework
		/// </summary>
		public string Framework { get; }

		/// <summary>
		/// The name of the project
		/// </summary>
		public string Name { get; }

		private bool TryPublish()
		{
			var workingDirectory = Path.Combine(Directory, Name);

			try
			{
				_output.WriteLine("Publishing {0} to {1}.", Name, _publishDirectory);

				var args = new[]
				{
					"publish",
					"-c", "Release",
					"--output", _publishDirectory
				};

				_output.WriteLine("Running 'dotnet {0}' in {1}", string.Join(' ', args), workingDirectory);

				var startArgs = new StartArguments("dotnet", args)
				{
					WorkingDirectory = workingDirectory,
					Timeout = TimeSpan.FromSeconds(30)
				};

				var publishResult = Proc.Start(startArgs);

				foreach (var line in publishResult.ConsoleOut)
				{
					_output.WriteLine(line.Line);
				}

				_output.WriteLine("Publish exited with exit code: {0}", publishResult.ExitCode ?? -1);

				return publishResult.ExitCode.HasValue && publishResult.ExitCode.Value == 0;
			}
			catch (Exception e)
			{
				throw new Exception($"Problem running dotnet publish in {workingDirectory} to output to {_publishDirectory}.", e);
			}
		}

		/// <summary>
		/// Creates a process to run the dotnet project that will start when subscribed to.
		/// </summary>
		/// <param name="startupHookZipPath">The path to the startup hook zip file</param>
		/// <param name="environmentVariables">The environment variables to start the project with. The DOTNET_STARTUP_HOOKS environment variable will be added.</param>
		/// <returns></returns>
		public ObservableProcess CreateProcess(string startupHookZipPath, IDictionary<string, string> environmentVariables = null)
		{
			if (!TryPublish())
				throw new Exception("Unable to publish sample application.");

			var startupHookAssembly = UnzipStartupHook(startupHookZipPath);

			_output.WriteLine("Unzipped startup hooks to {0}.", startupHookAssembly);

			environmentVariables ??= new Dictionary<string, string>();
			environmentVariables["DOTNET_STARTUP_HOOKS"] = startupHookAssembly;

			var workingDir = Path.Combine(Directory, Name, _publishDirectory);

			_output.WriteLine("Launching process 'dotnet {0}.dll' from {1}", Name, workingDir);

			var arguments = new StartArguments("dotnet", $"{Name}.dll")
			{
				WorkingDirectory = workingDir,
				SendControlCFirst = true,
				Environment = environmentVariables
			};

			_process = new ObservableProcess(arguments);

			if (_process.ExitCode.HasValue)
				_output.WriteLine("Launching process 'dotnet {0}.dll' failed with exit code {1}", Name, _process.ExitCode.Value);

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
		private static string UnzipStartupHook(string startupHookZipPath)
		{
			var tempDirectory = Path.GetTempPath();
			var destination = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

			ZipFile.ExtractToDirectory(startupHookZipPath, destination);
			var startupHookAssembly = Path.Combine(destination, "ElasticApmAgentStartupHook.dll");

			if (!File.Exists(startupHookAssembly))
				throw new FileNotFoundException($"startup hook assembly does not exist at {startupHookAssembly}", startupHookAssembly);

			return startupHookAssembly;
		}

		public void Dispose() => _process?.Dispose();

		public static DotnetProject Create(ITestOutputHelper output, string name, string template, string framework, params string[] arguments)
		{
			var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

			System.IO.Directory.CreateDirectory(directory);

			output.WriteLine("Using temp directory '{0}'", directory);

			var globalJsonCreationResult = Proc.Start(new StartArguments("dotnet",
			[
				"new",
				"globaljson",
				"--sdk-version",
				"8.0.404", // Fixing this specific version, for now
				"--roll-forward",
				"disable"
			])
			{
				WorkingDirectory = directory
			});

			foreach (var line in globalJsonCreationResult.ConsoleOut)
			{
				output.WriteLine(line.Line);
			}

			var args = new[]
			{
				"new", template,
				"--name", name,
				"--no-update-check",
				"--framework", framework
			}.Concat(arguments);

			var argsString = string.Join(' ', args);
			output.WriteLine("Running dotnet {0}", argsString);

			var result = Proc.Start(new StartArguments("dotnet", args)
			{
				WorkingDirectory = directory,
				Timeout = TimeSpan.FromSeconds(30)
			});

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

			output.WriteLine("Created new {0} project for {1} in {2}.", template, framework, directory);

			foreach (var line in result.ConsoleOut)
			{
				output.WriteLine(line.Line);
			}

			return new DotnetProject(name, template, framework, directory, output);
		}
	}
}
