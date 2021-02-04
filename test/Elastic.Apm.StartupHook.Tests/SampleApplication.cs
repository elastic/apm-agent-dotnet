// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
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
		private static readonly string SolutionRoot;

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

		static SampleApplication() => SolutionRoot = FindSolutionRoot();

		private readonly string _startupHookPath;
		private ObservableProcess _process;

		public SampleApplication() : this(Path.Combine(SolutionRoot, "build/output/ElasticApmAgent", "ElasticApmAgentStartupHook.dll"))
		{
		}

		public SampleApplication(string startupHookPath)
		{
			if (startupHookPath is null)
				throw new ArgumentNullException(nameof(startupHookPath));

			if (!File.Exists(startupHookPath))
				throw new FileNotFoundException($"startup hook could not be found at {startupHookPath}", startupHookPath);

			_startupHookPath = startupHookPath;
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
			environmentVariables ??= new Dictionary<string, string>();
			environmentVariables["DOTNET_STARTUP_HOOKS"] = _startupHookPath;
			var arguments = new StartArguments("dotnet", "run", "-f", targetFramework)
			{
				WorkingDirectory = Path.Combine(SolutionRoot, "sample", "Elastic.Apm.StartupHook.Sample"),
				SendControlCFirst = true,
				Environment = environmentVariables
			};

			var startHandle = new ManualResetEvent(false);
			ExceptionDispatchInfo e = null;

			Uri uri = null;

			_process = new ObservableProcess(arguments);
			_process.SubscribeLines(
				onNext: line =>
				{
					if (line.Line.StartsWith("Now listening on:"))
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

			startHandle.WaitOne();
			_process.CancelAsyncReads();
			e?.Throw();

			return uri;
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
