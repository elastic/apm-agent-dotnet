// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using ElasticApmStartupHook;

// ReSharper disable once CheckNamespace - per doc. this must be called StartupHook without a namespace with an Initialize method.
internal class StartupHook
{
	private const string LoaderDll = "Elastic.Apm.StartupHook.Loader.dll";
	private const string LoaderTypeName = "Elastic.Apm.StartupHook.Loader.Loader";
	private const string LoaderTypeMethod = "Initialize";

	private const string SystemDiagnosticsDiagnosticsource = "System.Diagnostics.DiagnosticSource";

	private static readonly byte[] SystemDiagnosticsDiagnosticSourcePublicKeyToken = { 204, 123, 19, 255, 205, 45, 221, 81 };

	private static readonly Regex VersionRegex = new(
		@"^(?<major>\d+)(\.(?<minor>\d+))?(\.(?<patch>\d+))?(\-(?<pre>[0-9A-Za-z]+))?$",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

	private static StartupHookLogger Logger;

	/// <summary>
	/// The Initialize method called by DOTNET_STARTUP_HOOKS
	/// </summary>
	public static void Initialize()
	{
		Logger = StartupHookLogger.Create();

		if (!IsNet8OrHigher())
		{
			Logger.WriteLine("The .NET runtime version is lower than .NET 8. The Elastic APM .NET Agent startup " +
				"hook is only supported on .NET 8 or higher. Some functionality may not work as expected.");
		}

		var startupHookEnvVar = Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS");
		var startupHookDirectory = Path.GetDirectoryName(startupHookEnvVar);

		if (string.IsNullOrEmpty(startupHookEnvVar) || !File.Exists(startupHookEnvVar))
			return;

		var assemblies = AppDomain.CurrentDomain.GetAssemblies();

		Logger.WriteLine($"Assemblies loaded:{Environment.NewLine}{string.Join(Environment.NewLine, assemblies.Select(a => a.GetName()))}");

		Logger.WriteLine($"Check if {SystemDiagnosticsDiagnosticsource} is loaded");

		var diagnosticSourceAssemblies = assemblies
			.Where(a => a.GetName().Name.Equals(SystemDiagnosticsDiagnosticsource, StringComparison.Ordinal))
			.ToList();

		Assembly diagnosticSourceAssembly;
		switch (diagnosticSourceAssemblies.Count)
		{
			case 0:
				Logger.WriteLine($"No {SystemDiagnosticsDiagnosticsource} loaded. Loading using AssemblyLoadContext.Default");
				diagnosticSourceAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(SystemDiagnosticsDiagnosticsource));
				break;
			case 1:
				diagnosticSourceAssembly = diagnosticSourceAssemblies[0];
				Logger.WriteLine($"{SystemDiagnosticsDiagnosticsource} is loaded (Version: {diagnosticSourceAssembly.GetName().Version}). ");
				break;
			default:
				Logger.WriteLine($"Found {diagnosticSourceAssemblies.Count} {SystemDiagnosticsDiagnosticsource} assemblies loaded in the app domain");
				diagnosticSourceAssembly = diagnosticSourceAssemblies.First();
				Logger.WriteLine($"{SystemDiagnosticsDiagnosticsource} is loaded (Version: {diagnosticSourceAssembly.GetName().Version}). ");
				break;
		}

		Assembly loaderAssembly = null;
		string loaderDirectory;

		if (diagnosticSourceAssembly is null)
		{
			// use agent compiled against the highest version of System.Diagnostics.DiagnosticSource
			var highestAvailableAgent = Directory.EnumerateDirectories(startupHookDirectory)
				.Where(d => VersionRegex.IsMatch(d))
				.OrderByDescending(d => VersionRegex.Match(d).Groups["major"].Value)
				.First();

			loaderDirectory = Path.Combine(startupHookDirectory, highestAvailableAgent);

			Logger.WriteLine($"Loading {LoaderDll} using AssemblyLoadContext.Default from {loaderDirectory}");

			loaderAssembly = AssemblyLoadContext.Default
				.LoadFromAssemblyPath(Path.Combine(loaderDirectory, LoaderDll));
		}
		else
		{
			var diagnosticSourceAssemblyName = diagnosticSourceAssembly.GetName();
			var diagnosticSourcePublicKeyToken = diagnosticSourceAssemblyName.GetPublicKeyToken();
			if (!diagnosticSourcePublicKeyToken.SequenceEqual(SystemDiagnosticsDiagnosticSourcePublicKeyToken))
			{
				Logger.WriteLine($"{SystemDiagnosticsDiagnosticsource} public key token "
					+ $"{PublicKeyTokenBytesToString(diagnosticSourcePublicKeyToken)} did not match expected "
					+ $"public key token {PublicKeyTokenBytesToString(SystemDiagnosticsDiagnosticSourcePublicKeyToken)}");
				return;
			}

			var diagnosticSourceVersion = diagnosticSourceAssemblyName.Version;
			Logger.WriteLine($"{SystemDiagnosticsDiagnosticsource} {diagnosticSourceVersion} loaded");

			if (diagnosticSourceVersion.Major < 6)
			{
				Logger.WriteLine($"{SystemDiagnosticsDiagnosticsource} version {diagnosticSourceVersion} is not supported. 6.0.0 or higher is required. Agent not loaded.");
				return;
			}
			else if (diagnosticSourceVersion.Major == 6 || diagnosticSourceVersion.Major == 7)
			{
				loaderDirectory = Path.Combine(startupHookDirectory, "6.0.0");
			}
			else
			{
				loaderDirectory = Path.Combine(startupHookDirectory, "8.0.0");
			}

			if (Directory.Exists(loaderDirectory))
			{
				Logger.WriteLine($"Loading {LoaderDll} using AssemblyLoadContext.Default from {loaderDirectory}");

				loaderAssembly = AssemblyLoadContext.Default
					.LoadFromAssemblyPath(Path.Combine(loaderDirectory, LoaderDll));
			}
			else
			{
				Logger.WriteLine(
					$"No compatible agent for {SystemDiagnosticsDiagnosticsource} {diagnosticSourceVersion}. Agent not loaded");
			}
		}

		if (loaderAssembly is null)
		{
			Logger.WriteLine($"No {LoaderDll} assembly loaded. Agent not loaded");
			return;
		}

		LoadAssembliesFromLoaderDirectory(loaderDirectory);
		InvokerLoaderMethod(loaderAssembly);

		static bool IsNet8OrHigher()
		{
			var desc = RuntimeInformation.FrameworkDescription;
			if (desc.StartsWith(".NET", StringComparison.OrdinalIgnoreCase))
			{
				var parts = desc.Split(' ');
				if (parts.Length >= 2 && Version.TryParse(parts[1], out var version))
				{
					return version.Major >= 8;
				}
			}
			return false;
		}

		static string PublicKeyTokenBytesToString(byte[] publicKeyToken)
		{
			var token = string.Empty;
			for (var i = 0; i < publicKeyToken.Length; i++)
				token += $"{publicKeyToken[i]:x2}";

			return token;
		}
	}

	/// <summary>
	/// Loads assemblies from the loader directory if they exist
	/// </summary>
	/// <param name="loaderDirectory"></param>
	private static void LoadAssembliesFromLoaderDirectory(string loaderDirectory)
	{
		var context = new ElasticApmAssemblyLoadContext();
		AssemblyLoadContext.Default.Resolving += (_, name) =>
		{
			var assemblyPath = Path.Combine(loaderDirectory, name.Name + ".dll");
			if (File.Exists(assemblyPath))
			{
				try
				{
					var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
					if (name.Version == assemblyName.Version)
					{
						var keyToken = name.GetPublicKeyToken();
						var assemblyKeyToken = assemblyName.GetPublicKeyToken();
						if (keyToken.SequenceEqual(assemblyKeyToken))
						{
							// load Elastic.Apm assemblies with the default assembly load context, to allow DiagnosticListeners to subscribe.
							// For all other dependencies, load with a separate load context to not conflict with application dependencies.
							return name.Name.StartsWith("Elastic.Apm", StringComparison.Ordinal)
								? AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath)
								: context.LoadFromAssemblyPath(assemblyPath);
						}
					}
				}
				catch (Exception e)
				{
					Logger.WriteLine(e.ToString());
				}
			}

			return null;
		};
	}

	/// <summary>
	/// Invokes the Elastic.Apm.StartupHook.Loader.Loader.Initialize() method from a specific assembly
	/// </summary>
	/// <param name="loaderAssembly">The loader assembly</param>
	private static void InvokerLoaderMethod(Assembly loaderAssembly)
	{
		Logger.WriteLine($"Get {LoaderTypeName} type");
		var loaderType = loaderAssembly.GetType(LoaderTypeName);

		if (loaderType is null)
		{
			Logger.WriteLine($"{LoaderTypeName} type is null");
			return;
		}

		Logger.WriteLine($"Get {LoaderTypeName}.{LoaderTypeMethod} method");
		var initializeMethod = loaderType.GetMethod(LoaderTypeMethod, BindingFlags.Public | BindingFlags.Static);

		if (initializeMethod is null)
		{
			Logger.WriteLine($"{LoaderTypeName}.{LoaderTypeMethod} method is null");
			return;
		}

		Logger.WriteLine($"Invoke {LoaderTypeName}.{LoaderTypeMethod} method");
		initializeMethod.Invoke(null, null);
	}
}
