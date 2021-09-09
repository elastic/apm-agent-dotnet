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
			Directory = ResolveDirectory();

			try
			{
				AppDomain.CurrentDomain.AssemblyResolve += ResolveDependencies;
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
				var assembly = Assembly.Load($"Elastic.Apm.Profiler.Managed, Version={version}, Culture=neutral, PublicKeyToken=ae7400d2c189cf22");
				if (assembly != null)
				{
					var type = assembly.GetType("Elastic.Apm.Profiler.Managed.AutoInstrumentation", throwOnError: false);
					var method = type?.GetRuntimeMethod("Initialize", parameters: Type.EmptyTypes);
					method?.Invoke(obj: null, parameters: null);
				}
			}
			catch (Exception e)
			{
				Logger.Log(LogLevel.Error, e, "Error loading managed assemblies.");
			}
		}

		internal static string Directory { get; }

		internal static string ReadEnvironmentVariable(string key)
		{
			try
			{
				return Environment.GetEnvironmentVariable(key);
			}
			catch (Exception e)
			{
				Logger.Log(LogLevel.Error, e, "Error reading environment variable.");
			}

			return null;
		}
	}
}
