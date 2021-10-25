// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Elastic.Apm.Tests.Utilities;
using ProcNet;
using ProcNet.Std;

namespace Elastic.Apm.Profiler.Managed.Tests
{
	/// <summary>
	/// A sample application that can be instrumented with the (Core)CLR profiler
	/// </summary>
	public class ProfiledApplication : IDisposable
	{
		private const string ProfilerClassId = "{FA65FE15-F085-4681-9B20-95E04F6C03CC}";
		private readonly string _profilerPath;
		private readonly string _projectDirectory;
		private readonly string _projectName;
		private readonly string _publishDirectory;

		public ProfiledApplication(string projectName)
		{
			_projectName = projectName;
			_projectDirectory = Path.Combine(SolutionPaths.Root, "sample", projectName);

			if (!Directory.Exists(_projectDirectory))
				throw new DirectoryNotFoundException($"project could not be found at {_projectDirectory}");

			string profilerFile;
			if (TestEnvironment.IsWindows)
				profilerFile = "elastic_apm_profiler.dll";
			else if (TestEnvironment.IsLinux)
				profilerFile = "libelastic_apm_profiler.so";
			else
				profilerFile = "libelastic_apm_profiler.dylib";

			_profilerPath = Path.Combine(SolutionPaths.Root, "target", "release", profilerFile);

			if (!File.Exists(_profilerPath))
				throw new FileNotFoundException(
					$"profiler could not be found at {_profilerPath}. Run `build.[bat|sh] in project root to build it",
					_profilerPath);

			_publishDirectory = Path.Combine("bin", "Publish");
		}

		private ObservableProcess _process;

		private void Publish(string targetFramework)
		{
			var processInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"publish -c Release -f {targetFramework} -o {Path.Combine(_publishDirectory, targetFramework)}",
				WorkingDirectory = _projectDirectory
			};

			using var process = new Process { StartInfo = processInfo };
			process.Start();
			process.WaitForExit();
		}

		/// <summary>
		/// Starts the sample application and returns the <see cref="Uri" /> on which the application
		/// can be reached.
		/// </summary>
		/// <param name="targetFramework">
		/// The target framework under which to run the profiled app. Must be a version supported in
		/// the TargetFrameworks of the profiled app
		/// </param>
		/// <param name="timeout">A timeout to wait for the process to complete.</param>
		/// <param name="environmentVariables">
		/// The environment variables to start the sample app with. The profiler
		/// environment variables will be added.
		/// </param>
		/// <param name="onNext">delegate to call when line is received</param>
		/// <param name="onException">delegate to call when exception occurs</param>
		/// <returns></returns>
		public void Start(
			string targetFramework,
			TimeSpan timeout,
			IDictionary<string, string> environmentVariables = null,
			Action<LineOut> onNext = null,
			Action<Exception> onException = null
		)
		{
			Publish(targetFramework);

			environmentVariables ??= new Dictionary<string, string>();
			environmentVariables["CORECLR_ENABLE_PROFILING"] = "1";
			environmentVariables["CORECLR_PROFILER"] = ProfilerClassId;
			environmentVariables["CORECLR_PROFILER_PATH"] = _profilerPath;

			environmentVariables["COR_ENABLE_PROFILING"] = "1";
			environmentVariables["COR_PROFILER"] = ProfilerClassId;
			environmentVariables["COR_PROFILER_PATH"] = _profilerPath;

			environmentVariables["ELASTIC_APM_PROFILER_HOME"] =
				Path.Combine(SolutionPaths.Root, "src", "Elastic.Apm.Profiler.Managed", "bin", "Release");
			environmentVariables["ELASTIC_APM_PROFILER_INTEGRATIONS"] =
				Path.Combine(SolutionPaths.Root, "src", "Elastic.Apm.Profiler.Managed", "integrations.yml");

			environmentVariables["ELASTIC_APM_PROFILER_LOG"] = "trace";
			// log to relative logs directory for managed loader
			environmentVariables["ELASTIC_APM_PROFILER_LOG_DIR"] = Path.Combine(SolutionPaths.Root, "logs");

			//environmentVariables["ELASTIC_APM_PROFILER_LOG_TARGETS"] = "file;stdout";
			//environmentVariables["ELASTIC_APM_PROFILER_LOG_IL"] = "true";

			var workingDirectory = Path.Combine(_projectDirectory, _publishDirectory, targetFramework);

			// use the .exe for net461
			var arguments = targetFramework == "net461"
				? new StartArguments(Path.Combine(workingDirectory, $"{_projectName}.exe"))
				: new StartArguments("dotnet", $"{_projectName}.dll");

			arguments.Environment = environmentVariables;
			arguments.WorkingDirectory = workingDirectory;

			_process = new ObservableProcess(arguments);
			_process.SubscribeLines(onNext ?? (_ => { }), onException ?? (_ => { }));
			_process.WaitForCompletion(timeout);
		}

		public void Dispose() => _process?.Dispose();
	}
}
