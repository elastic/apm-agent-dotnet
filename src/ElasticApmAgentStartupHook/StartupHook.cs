// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Elastic.Apm.StartupHook.Common;

// ReSharper disable once CheckNamespace - per doc. this must be called StartupHook without a namespace with an Initialize method.
internal class StartupHook
{
	private const string ElasticApmStartuphookLoaderDll = "Elastic.Apm.StartupHook.Loader.dll";
	private const string SystemDiagnosticsDiagnosticsource = "System.Diagnostics.DiagnosticSource";

	private static readonly byte[] SystemDiagnosticsDiagnosticSourcePublicKeyToken = { 204, 123, 19, 255, 205, 45, 221, 81 };
	private static readonly Regex VersionRegex = new Regex(
		@"^(?<major>\d+)(\.(?<minor>\d+))?(\.(?<patch>\d+))?(\-(?<pre>[0-9A-Za-z]+))?$",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

	private static StartupHookLogger _logger;

	/// <summary>
	/// The Initialize method called by DOTNET_STARTUP_HOOKS
	/// </summary>
	public static void Initialize()
	{
		var startupHookEnvVar = Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS");
		var startupHookDirectory = Path.GetDirectoryName(startupHookEnvVar);

		if (string.IsNullOrEmpty(startupHookEnvVar) || !File.Exists(startupHookEnvVar))
			return;

		_logger = StartupHookLogger.CreateLogger();
		_logger.WriteLine($"Check if {SystemDiagnosticsDiagnosticsource} is loaded");

		var assemblies = AppDomain.CurrentDomain.GetAssemblies();

		_logger.WriteLine($"Assemblies loaded:{Environment.NewLine}{string.Join(",", assemblies.Select(a => a.GetName()))}");
		var diagnosticSourceAssemblies = assemblies
			.Where(a => a.GetName().Name.Equals(SystemDiagnosticsDiagnosticsource, StringComparison.Ordinal))
			.ToList();

		Assembly diagnosticSourceAssembly;
		switch (diagnosticSourceAssemblies.Count)
		{
			case 0:
				_logger.WriteLine($"No {SystemDiagnosticsDiagnosticsource} loaded. Load using AssemblyLoadContext.Default");
				diagnosticSourceAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(SystemDiagnosticsDiagnosticsource));
				break;
			case 1:
				diagnosticSourceAssembly = diagnosticSourceAssemblies[0];
				break;
			default:
				_logger.WriteLine($"Found {diagnosticSourceAssemblies.Count} {SystemDiagnosticsDiagnosticsource} assemblies loaded in the app domain");
				diagnosticSourceAssembly = diagnosticSourceAssemblies.First();
				break;
		}

		Assembly loader = null;

		if (diagnosticSourceAssembly is null)
		{
			// use agent compiled against the highest version of System.Diagnostics.DiagnosticSource
			var highestAvailableAgent = Directory.EnumerateDirectories(startupHookDirectory)
				.Where(d => VersionRegex.IsMatch(d))
				.OrderByDescending(d => VersionRegex.Match(d).Groups["major"].Value)
				.First();

			var versionDirectory = Path.Combine(startupHookDirectory, highestAvailableAgent);
			loader = AssemblyLoadContext.Default
				.LoadFromAssemblyPath(Path.Combine(versionDirectory, ElasticApmStartuphookLoaderDll));
		}
		else
		{
			var diagnosticSourceAssemblyName = diagnosticSourceAssembly.GetName();
			var diagnosticSourcePublicKeyToken = diagnosticSourceAssemblyName.GetPublicKeyToken();
			if (!diagnosticSourcePublicKeyToken.SequenceEqual(SystemDiagnosticsDiagnosticSourcePublicKeyToken))
			{
				_logger.WriteLine($"{SystemDiagnosticsDiagnosticsource} public key token "
					+ $"{PublicKeyTokenBytesToString(diagnosticSourcePublicKeyToken)} did not match expected "
					+ $"public key token {PublicKeyTokenBytesToString(SystemDiagnosticsDiagnosticSourcePublicKeyToken)}");
				return;
			}

			var diagnosticSourceVersion = diagnosticSourceAssemblyName.Version;
			_logger.WriteLine($"{SystemDiagnosticsDiagnosticsource} {diagnosticSourceVersion} loaded");

			var versionDirectory = Path.Combine(startupHookDirectory, $"{diagnosticSourceVersion.Major}.0.0");
			if (Directory.Exists(versionDirectory))
			{
				loader = AssemblyLoadContext.Default
					.LoadFromAssemblyPath(Path.Combine(versionDirectory, ElasticApmStartuphookLoaderDll));
			}
			else
				_logger.WriteLine(
					$"No compatible agent for {SystemDiagnosticsDiagnosticsource} {diagnosticSourceVersion}. Agent not loaded");
		}

		InvokerLoaderMethod(loader);
	}

	/// <summary>
	/// Converts public key token bytes into a string
	/// </summary>
	private static string PublicKeyTokenBytesToString(byte[] publicKeyToken)
	{
		var token = string.Empty;
		for (var i = 0; i < publicKeyToken.Length; i++)
			token += $"{publicKeyToken[i]:x2}";

		return token;
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
}
