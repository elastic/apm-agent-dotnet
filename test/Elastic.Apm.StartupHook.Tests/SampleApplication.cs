// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.ExceptionServices;
using System.Threading;
using ProcNet;

namespace Elastic.Apm.StartupHook.Tests
{
	/// <summary>
	/// A sample ASP.NET 5/Core application that can be instrumented using startup hooks
	/// </summary>
	public class SampleApplication : IDisposable
	{
		private readonly string _startupHookZipPath;
		private ObservableProcess _process;

		public SampleApplication() : this(SolutionPaths.AgentZip)
		{
		}

		public SampleApplication(string startupHookZipPath)
		{
			if (startupHookZipPath is null)
				throw new ArgumentNullException(nameof(startupHookZipPath));

			if (!File.Exists(startupHookZipPath))
				throw new FileNotFoundException($"startup hook zip file could not be found at {startupHookZipPath}", startupHookZipPath);

			_startupHookZipPath = startupHookZipPath;
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
			var startupHookAssembly = UnzipStartupHook();
			environmentVariables ??= new Dictionary<string, string>();
			environmentVariables["DOTNET_STARTUP_HOOKS"] = startupHookAssembly;
			var arguments = new StartArguments("dotnet", "run", "-f", targetFramework)
			{
				WorkingDirectory = Path.Combine(SolutionPaths.Root, "sample", "Elastic.Apm.StartupHook.Sample"),
				SendControlCFirst = true,
				Environment = environmentVariables
			};

			var startHandle = new ManualResetEvent(false);
			ExceptionDispatchInfo e = null;
			Uri uri = null;

			var capturedLines = new List<string>();

			_process = new ObservableProcess(arguments);
			_process.SubscribeLines(
				onNext: line =>
				{
					capturedLines.Add(line.Line);
					if (line.Line.StartsWith("Now listening on: http:"))
					{
						try
						{
							var endpoint = line.Line.Substring("Now listening on:".Length).Trim();
							uri = new Uri(endpoint);
						}
						catch (Exception exception)
						{
							e = ExceptionDispatchInfo.Capture(exception);
						}

						startHandle.Set();
					}
				},
				onError: exception => e = ExceptionDispatchInfo.Capture(exception));

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

		public void Stop()
		{
			if (_process?.ProcessId != null)
			{
				_process.SendControlC();
				_process.Dispose();
			}
		}

		public void Dispose() => Stop();
	}
}
