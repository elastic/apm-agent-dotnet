// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="Startup.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Reflection;

namespace Elastic.Apm.Profiler.Managed.Loader
{
	public partial class Startup
	{
		static Startup()
		{
			Logger.Log(LogLevel.Information, "Elastic.Apm.Profiler.Managed.Loader.Startup: Invoked. AppDomain: {0}, CLR version: {1}",
				AppDomain.CurrentDomain.FriendlyName, Environment.Version);
			var loaderOptimization = ReadEnvironmentVariable("COMPlus_LoaderOptimization");
			Logger.Log(LogLevel.Debug, "COMPlus_LoaderOptimization={0}", loaderOptimization ?? "(not set)");
			Logger.Log(LogLevel.Debug, "AppDomain base directory: {0}", AppDomain.CurrentDomain.BaseDirectory);
			Logger.Log(LogLevel.Debug, "AppDomain shadow copy files: {0}", AppDomain.CurrentDomain.ShadowCopyFiles);
			Directory = ResolveDirectory();

			try
			{
				AppDomain.CurrentDomain.AssemblyResolve += ResolveDependencies;
				Logger.Log(LogLevel.Debug, "AssemblyResolve event handler registered on AppDomain '{0}'", AppDomain.CurrentDomain.FriendlyName);
			}
			catch (Exception e)
			{
				Logger.Log(LogLevel.Error, e, "Error registering AssemblyResolve event handler.");
			}

			TryLoadManagedAssembly();
		}

		private static void TryLoadManagedAssembly()
		{
			try
			{
				var version = Assembly.GetExecutingAssembly().GetName().Version;
				var assemblyName = $"Elastic.Apm.Profiler.Managed, Version={version}, Culture=neutral, PublicKeyToken=ae7400d2c189cf22";

				Assembly assembly;
				try
				{
					var alreadyLoaded = Array.FindAll(
						AppDomain.CurrentDomain.GetAssemblies(),
						a => string.Equals(a.GetName().Name, "Elastic.Apm.Profiler.Managed", StringComparison.OrdinalIgnoreCase));
					if (alreadyLoaded.Length > 0)
					{
						foreach (var loaded in alreadyLoaded)
							Logger.Log(LogLevel.Warning, "Elastic.Apm.Profiler.Managed already loaded in AppDomain before Assembly.Load: {0} from '{1}'", loaded.FullName, loaded.Location);
					}
					else
						Logger.Log(LogLevel.Debug, "Elastic.Apm.Profiler.Managed not yet present in AppDomain");

					Logger.Log(LogLevel.Debug, "Attempting Assembly.Load for {0}", assemblyName);
					assembly = Assembly.Load(assemblyName);
					Logger.Log(LogLevel.Debug, "Assembly.Load succeeded: {0}", assembly?.Location ?? "(null)");
				}
				catch (Exception loadEx)
				{
					// Assembly.Load can fail on .NET Framework when the CLR's fusion probing finds the
					// assembly in a path outside the profiler folder (e.g. the application's bin directory
					// from a previous shadow-copy or partial bind) but cannot satisfy all its dependencies
					// from that location. In that case AssemblyResolve is never fired. Fall back to
					// Assembly.LoadFrom with the explicit profiler path, which bypasses fusion probing.
					WarnIfConflictingAssemblyInAppPath("Elastic.Apm.Profiler.Managed.dll");
					Logger.Log(LogLevel.Warning, loadEx, "Assembly.Load failed for {0}, falling back to Assembly.LoadFrom from {1}", assemblyName, Directory);
					var path = System.IO.Path.Combine(Directory, "Elastic.Apm.Profiler.Managed.dll");
					Logger.Log(LogLevel.Debug, "Assembly.LoadFrom path: {0}, exists: {1}", path, System.IO.File.Exists(path));
					assembly = Assembly.LoadFrom(path);
					Logger.Log(LogLevel.Debug, "Assembly.LoadFrom succeeded: {0}", assembly?.Location ?? "(null)");
					Logger.Log(LogLevel.Warning,
						"Assembly loaded via LoadFrom fallback (Load context unavailable). "
						+ "Native profiler ReJIT instrumentation may not activate. "
						+ "If instrumentation is missing, set COMPlus_LoaderOptimization=1 on the app pool environment variables.");
				}

				if (assembly == null)
				{
					Logger.Log(LogLevel.Error, "Failed to load Elastic.Apm.Profiler.Managed: assembly is null");
					return;
				}

				if (!string.IsNullOrEmpty(assembly.Location)
					&& !assembly.Location.StartsWith(Directory, StringComparison.OrdinalIgnoreCase))
				{
					Logger.Log(LogLevel.Warning,
						"Elastic.Apm.Profiler.Managed loaded from unexpected location: {0}. Expected location under profiler directory: {1}. This may indicate a version mismatch.",
						assembly.Location, Directory);
				}

				var type = assembly.GetType("Elastic.Apm.Profiler.Managed.AutoInstrumentation", throwOnError: false);
				if (type == null)
				{
					Logger.Log(LogLevel.Error, "Could not find type Elastic.Apm.Profiler.Managed.AutoInstrumentation in {0}", assembly.Location);
					return;
				}

				var method = type.GetRuntimeMethod("Initialize", parameters: Type.EmptyTypes);
				if (method == null)
				{
					Logger.Log(LogLevel.Error, "Could not find method AutoInstrumentation.Initialize in {0}", assembly.Location);
					return;
				}

				Logger.Log(LogLevel.Debug, "Invoking AutoInstrumentation.Initialize");
				method.Invoke(obj: null, parameters: null);
				Logger.Log(LogLevel.Information, "AutoInstrumentation.Initialize completed successfully");
			}
			catch (Exception e)
			{
				Logger.Log(LogLevel.Error, e, "Error loading managed assemblies.");
			}
		}

		internal static string Directory { get; }

		private static void WarnIfConflictingAssemblyInAppPath(string fileName)
		{
			var baseDir = AppDomain.CurrentDomain.BaseDirectory;
			var candidates = new[]
			{
				System.IO.Path.Combine(baseDir, fileName),
				System.IO.Path.Combine(baseDir, "bin", fileName),
			};
			foreach (var candidate in candidates)
			{
				if (System.IO.File.Exists(candidate))
				{
					Logger.Log(LogLevel.Warning,
						"Conflicting assembly found at {0}. Under MultiDomainHost loader optimization (IIS default), "
						+ "this causes Assembly.Load to fail without firing AssemblyResolve. "
						+ "Set COMPlus_LoaderOptimization=1 on the app pool environment variables to resolve.",
						candidate);
				}
			}
		}

		internal static string ReadEnvironmentVariable(string key)
		{
			try
			{
				return Environment.GetEnvironmentVariable(key);
			}
			catch (Exception e)
			{
				Logger.Log(LogLevel.Error, e, "Error reading environment variable: {0}", key);
			}

			return null;
		}
	}
}
