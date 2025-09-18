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
using ElasticApmStartupHook;

// ReSharper disable once CheckNamespace - per doc. this must be called StartupHook without a namespace with an Initialize method.
internal class StartupHook
{
	private const string LoaderDll = "Elastic.Apm.StartupHook.Loader.dll";
	private const string LoaderTypeName = "Elastic.Apm.StartupHook.Loader.Loader";
	private const string LoaderTypeMethod = "Initialize";

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
				"hook is only supported on .NET 8 or higher.");

			return;
		}

		var startupHookEnvVar = Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS");
		var startupHookDirectory = Path.GetDirectoryName(startupHookEnvVar);

		if (string.IsNullOrEmpty(startupHookEnvVar) || !File.Exists(startupHookEnvVar))
			return;

		var assemblies = AppDomain.CurrentDomain.GetAssemblies();

		Logger.WriteLine($"Assemblies loaded:{Environment.NewLine}{string.Join(Environment.NewLine, assemblies.Select(a => a.GetName()))}");

		var loaderDirectory = startupHookDirectory;
		var loaderAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(loaderDirectory, LoaderDll));

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
