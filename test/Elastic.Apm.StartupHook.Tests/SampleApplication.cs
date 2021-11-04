// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using Elastic.Apm.Tests.Utilities;
using ProcNet;

namespace Elastic.Apm.StartupHook.Tests
{
	/// <summary>
	/// A sample ASP.NET 5/Core application that can be instrumented using startup hooks
	/// </summary>
	public class SampleApplication : IDisposable
	{
		private const string ElasticApmStartuphookSampleProjectName = "Elastic.Apm.StartupHook.Sample";
		private readonly string _startupHookZipPath;
		private ObservableProcess _process;
		private readonly string _publishDirectory;

		public SampleApplication()
		{
			if (!File.Exists(SolutionPaths.AgentZip))
				throw new FileNotFoundException($"startup hook zip file could not be found at {SolutionPaths.AgentZip}", SolutionPaths.AgentZip);

			_startupHookZipPath = SolutionPaths.AgentZip;
			_publishDirectory = Path.Combine("bin", "Publish");
		}

		private void Publish(string projectDirectory, string targetFramework)
		{
			var processInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"publish -c Release -f {targetFramework} -o {Path.Combine(_publishDirectory, targetFramework)}",
				WorkingDirectory = projectDirectory
			};

			using var process = new Process { StartInfo = processInfo };
			process.Start();
			process.WaitForExit();
		}

		/// <summary>
		/// Starts the sample application and returns the <see cref="Uri"/> on which the application
		/// can be reached.
		/// </summary>
		/// <param name="targetFramework">The target framework under which to run the sample app. Must be a version supported in the TargetFrameworks of the sample app</param>
		/// <param name="environmentVariables">The environment variables to start the sample app with. The DOTNET_STARTUP_HOOKS environment variable will be added.</param>
		/// <returns></returns>
		public Uri Start(string targetFramework, IDictionary<string, string> environmentVariables = null)
		{
			var projectDirectory = Path.Combine(SolutionPaths.Root, "sample", ElasticApmStartuphookSampleProjectName);
			Publish(projectDirectory, targetFramework);

			var startupHookAssembly = UnzipStartupHook();
			environmentVariables ??= new Dictionary<string, string>();
			environmentVariables["DOTNET_STARTUP_HOOKS"] = startupHookAssembly;

			var arguments = new StartArguments("dotnet", $"{ElasticApmStartuphookSampleProjectName}.dll")
			{
				WorkingDirectory = Path.Combine(projectDirectory, _publishDirectory, targetFramework),
				SendControlCFirst = true,
				Environment = environmentVariables
			};

			var startHandle = new ManualResetEvent(false);
			ExceptionDispatchInfo e = null;
			Uri uri = null;

			var capturedLines = new List<string>();
			var endpointRegex = new Regex(@"\s*Now listening on:\s*(?<endpoint>http\:[^\s]*)");

			_process = new ObservableProcess(arguments);
			_process.SubscribeLines(
				line =>
				{
					capturedLines.Add(line.Line);
					var match = endpointRegex.Match(line.Line);
					if (match.Success)
					{
						try
						{
							var endpoint = match.Groups["endpoint"].Value.Trim();
							uri = new Uri(endpoint);
						}
						catch (Exception exception)
						{
							e = ExceptionDispatchInfo.Capture(exception);
						}

						startHandle.Set();
					}
				},
				exception => e = ExceptionDispatchInfo.Capture(exception));

			var timeout = TimeSpan.FromMinutes(2);
			var signalled = startHandle.WaitOne(timeout);
			if (!signalled)
			{
				throw new Exception($"Could not start sample application within timeout {timeout}: "
					+ string.Join(Environment.NewLine, capturedLines));
			}

			_process.CancelAsyncReads();
			e?.Throw();

			return uri;
		}

		/// <summary>
		/// Unzips the agent startup hook zip file to the temp directory, and returns
		/// the path to the startup hook assembly.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException">
		///	Startup hook assembly not found in the extracted files.
		/// </exception>
		private string UnzipStartupHook()
		{
			var tempDirectory = Path.GetTempPath();
			var destination = Path.Combine(tempDirectory, Guid.NewGuid().ToString());

			ZipFile.ExtractToDirectory(_startupHookZipPath, destination);
			var startupHookAssembly = Path.Combine(destination, "ElasticApmAgentStartupHook.dll");

			if (!File.Exists(startupHookAssembly))
				throw new FileNotFoundException($"startup hook assembly does not exist at {startupHookAssembly}", startupHookAssembly);

			return startupHookAssembly;
		}

		public void Dispose() => _process?.Dispose();
	}
}
