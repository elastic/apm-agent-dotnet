using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

// ReSharper disable once CheckNamespace - per doc. this must be called StartupHook without a namespace with an Initialize method.
internal class StartupHook
{
	private static readonly byte[] SystemDiagnosticsDiagnosticSourcePublicKeyToken = { 204, 123, 19, 255, 205, 45, 221, 81 };

	private static StartupHookLogger _logger;

	/// <summary>
	/// The Initialize method called by DOTNET_STARTUP_HOOKS
	/// </summary>
	public static void Initialize()
	{
		var startupHookEnvVar = Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS");

		if (string.IsNullOrEmpty(startupHookEnvVar) || !File.Exists(startupHookEnvVar))
			return;

		var startupHookDirectory = Path.GetDirectoryName(startupHookEnvVar);
		var startupHookLoggingEnvVar = Environment.GetEnvironmentVariable("ELASTIC_APM_STARTUP_HOOKS_LOGGING");
		_logger = new StartupHookLogger(Path.Combine(startupHookDirectory, "StartupHook.log"), !string.IsNullOrEmpty(startupHookLoggingEnvVar));

		_logger.WriteLine("Check if System.Diagnostics.DiagnosticSource is loaded");
		var diagnosticSourceAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("System.Diagnostics.DiagnosticSource"));
		Assembly loader = null;

		if (diagnosticSourceAssembly is null)
		{
			_logger.WriteLine("No System.Diagnostics.DiagnosticSource loaded");

			// use current agent
			loader = AssemblyLoadContext.Default
				.LoadFromAssemblyPath(Path.Combine(startupHookDirectory, "Elastic.Apm.StartupHook.Loader.dll"));

		}
		else
		{
			var diagnosticSourceAssemblyName = diagnosticSourceAssembly.GetName();
			var diagnosticSourcePublicKeyToken = diagnosticSourceAssemblyName.GetPublicKeyToken();
			if (!diagnosticSourcePublicKeyToken.SequenceEqual(SystemDiagnosticsDiagnosticSourcePublicKeyToken))
				return;

			var diagnosticSourceVersion = diagnosticSourceAssemblyName.Version;
			_logger.WriteLine($"System.Diagnostics.DiagnosticSource {diagnosticSourceVersion} loaded");

			if (diagnosticSourceVersion.Major == 4)
			{
				var versionDirectory = Path.Combine(startupHookDirectory, "4.0.0");
				loader = AssemblyLoadContext.Default
					.LoadFromAssemblyPath(Path.Combine(versionDirectory, "Elastic.Apm.StartupHook.Loader.dll"));
			}
			else if (diagnosticSourceVersion.Major == 5)
			{
				loader = AssemblyLoadContext.Default
					.LoadFromAssemblyPath(Path.Combine(startupHookDirectory, "Elastic.Apm.StartupHook.Loader.dll"));
			}
		}

		InvokerLoaderMethod(loader);
	}

	/// <summary>
	/// Invokes the Elastic.Apm.StartupHook.Loader.Loader.Initialize() method from a specific assembly
	/// </summary>
	/// <param name="loaderAssembly">The loader assembly</param>
	private static void InvokerLoaderMethod(Assembly loaderAssembly)
	{
		if (loaderAssembly is null)
			return;

		const string loaderTypeName = "Elastic.Apm.StartupHook.Loader.Loader";
		const string loaderTypeMethod = "Initialize";

		_logger.WriteLine($"Get {loaderTypeName} type");
		var loaderType = loaderAssembly.GetType(loaderTypeName);

		if (loaderType is null)
		{
			_logger.WriteLine($"{loaderTypeName} type is null");
			return;
		}

		_logger.WriteLine($"Get {loaderTypeName}.{loaderTypeMethod} method");
		var initializeMethod = loaderType.GetMethod(loaderTypeMethod, BindingFlags.Public | BindingFlags.Static);

		if (initializeMethod is null)
		{
			_logger.WriteLine($"{loaderTypeName}.{loaderTypeMethod} method is null");
			return;
		}

		_logger.WriteLine($"Invoke {loaderTypeName}.{loaderTypeMethod} method");
		initializeMethod.Invoke(null, null);
	}

	private class StartupHookLogger
	{
		private readonly string _logPath;
		private readonly bool _enabled;

		public StartupHookLogger(string logPath, bool enabled)
		{
			_logPath = logPath;
			_enabled = enabled;
		}

		public void WriteLine(string message)
		{
			if (_enabled)
			{
				try
				{
					File.AppendAllLines(_logPath, new[] { $"[{DateTime.UtcNow:u}] {message}" });
				}
				catch
				{
					// if we can't log a log message, there's not much that can be done, so ignore
				}
			}
		}
	}
}
