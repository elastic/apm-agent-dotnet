// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Elastic.Apm.Tests.Utilities;
using ProcNet;
using ProcNet.Std;

namespace Elastic.Apm.Profiler.Managed.Tests
{

	public class ProfiledIntegrationApplication : ProfiledApplication
	{
		public ProfiledIntegrationApplication(string projectName)
			: base(projectName, "test", "integrations", "applications")
		{}
	}

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

		public ProfiledApplication(string projectName) : this(projectName, null) {}

		protected ProfiledApplication(string projectName, params string[] folders)
		{
			_projectName = projectName;
			var root = folders == null || folders.Length == 0
				? Path.Combine(SolutionPaths.Root, "test", "profiler", "applications")
				: Path.Combine(new [] { SolutionPaths.Root }.Concat(folders).ToArray());
			_projectDirectory = Path.Combine(root, projectName);

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
			{
				throw new FileNotFoundException(
					$"profiler could not be found at {_profilerPath}. Run './build.[bat|sh] build-profiler' in project root to build it",
					_profilerPath);
			}

			_publishDirectory = Path.Combine("bin", "Publish");
		}

		private ObservableProcess _process;

		private void Publish(string targetFramework, string outputDirectory, string msBuildProperties)
		{
			// if we're running on CI and the publish directory already exists for the
			// target framework, skip publishing again.
			if (TestEnvironment.IsCi && Directory.Exists(outputDirectory))
				return;

			var processInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"publish -c Release -f {targetFramework} --property:PublishDir={outputDirectory} {msBuildProperties}",
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
		/// <param name="msBuildProperties">
		///	MsBuild properties passed to dotnet publish when compiling the sample app.
		/// Appended to dotnet publish with the form <code>-p:{key}={value}</code>
		/// </param>
		/// <param name="onNext">delegate to call when line is received</param>
		/// <param name="onException">delegate to call when exception occurs</param>
		/// <returns></returns>
		public void Start(
			string targetFramework,
			TimeSpan timeout,
			IDictionary<string, string> environmentVariables = null,
			IDictionary<string, string> msBuildProperties = null,
			Action<LineOut> onNext = null,
			Action<Exception> onException = null,
			bool doNotWaitForCompletion = false,
			bool useLocalhostHttp5000 = false
		)
		{
			var properties = CreateMsBuildProperties(msBuildProperties);
			var outputDirectory = GetPublishOutputDirectory(targetFramework, properties);
			var workingDirectory = Path.Combine(_projectDirectory, _publishDirectory, outputDirectory);

			Publish(targetFramework, workingDirectory, properties);

			environmentVariables ??= new Dictionary<string, string>();
			environmentVariables["CORECLR_ENABLE_PROFILING"] = "1";
			environmentVariables["CORECLR_PROFILER"] = ProfilerClassId;
			environmentVariables["CORECLR_PROFILER_PATH"] = _profilerPath;

			environmentVariables["COR_ENABLE_PROFILING"] = "1";
			environmentVariables["COR_PROFILER"] = ProfilerClassId;
			environmentVariables["COR_PROFILER_PATH"] = _profilerPath;

			//Temporarily disable since it does IO that fails on CI which is noisy
			environmentVariables["ELASTIC_APM_CENTRAL_CONFIG"] = "false";
			environmentVariables["ELASTIC_APM_CLOUD_PROVIDER"] = "none";

			environmentVariables["ELASTIC_APM_PROFILER_HOME"] =
				Path.Combine(SolutionPaths.Root, "src", "profiler", "Elastic.Apm.Profiler.Managed", "bin", "Release");
			environmentVariables["ELASTIC_APM_PROFILER_INTEGRATIONS"] =
				Path.Combine(SolutionPaths.Root, "src", "profiler", "Elastic.Apm.Profiler.Managed", "integrations.yml");

			environmentVariables["ELASTIC_APM_PROFILER_LOG"] = "trace";
			// log to relative logs directory for managed loader
			environmentVariables["ELASTIC_APM_PROFILER_LOG_DIR"] = Path.Combine(SolutionPaths.Root, "logs");

			environmentVariables["ELASTIC_APM_PROFILER_LOG_TARGETS"] = "file;stdout";
			//environmentVariables["ELASTIC_APM_PROFILER_LOG_IL"] = "true";

			// use the .exe for net462
			var arguments = targetFramework == "net462"
				? new StartArguments(Path.Combine(workingDirectory, $"{_projectName}.exe"))
				: useLocalhostHttp5000
					? new StartArguments("dotnet", $"{_projectName}.dll", "--urls", "http://localhost:5000")
					: new StartArguments("dotnet", $"{_projectName}.dll");

			arguments.Environment = environmentVariables;
			arguments.WorkingDirectory = workingDirectory;

			_process = new ObservableProcess(arguments);
			_process.SubscribeLines(onNext ?? (_ => { }), onException ?? (_ => { }));

			if (!doNotWaitForCompletion)
				_process.WaitForCompletion(timeout);
		}

		private static string GetPublishOutputDirectory(string targetFramework, string properties)
		{
			if (properties is null)
				return targetFramework;

			var hash = MD5.HashData(Encoding.UTF8.GetBytes(properties));
			var builder = new StringBuilder(targetFramework).Append('-');

			foreach (var b in hash)
				builder.Append(b.ToString("x2"));

			return builder.ToString();
		}

		private static string CreateMsBuildProperties(IDictionary<string, string> msBuildProperties) =>
			msBuildProperties is null
				? null
				: string.Join(" ", msBuildProperties.Select(kv => $"-p:{kv.Key}={kv.Value}"));

		public void Dispose() => _process?.Dispose();
	}
}
