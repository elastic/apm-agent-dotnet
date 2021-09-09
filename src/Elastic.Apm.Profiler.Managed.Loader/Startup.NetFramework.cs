// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="Startup.NetFramework.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETFRAMEWORK
using System;
using System.IO;
using System.Reflection;

namespace Elastic.Apm.Profiler.Managed.Loader
{
	public partial class Startup
	{
		private static string ResolveDirectory()
		{
			var framework = "net461";
			var directory = ReadEnvironmentVariable("ELASTIC_APM_PROFILER_HOME") ?? string.Empty;
			return Path.Combine(directory, framework);
		}

		private static Assembly ResolveDependencies(object sender, ResolveEventArgs args)
		{
			var assemblyName = new AssemblyName(args.Name).Name;

			// On .NET Framework, having a non-US locale can cause mscorlib
			// to enter the AssemblyResolve event when searching for resources
			// in its satellite assemblies. Exit early so we don't cause
			// infinite recursion.
			if (string.Equals(assemblyName, "mscorlib.resources", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(assemblyName, "System.Net.Http", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			var path = Path.Combine(Directory, $"{assemblyName}.dll");
			if (File.Exists(path))
			{
				Logger.Log(LogLevel.Debug, "Loading {0} assembly", assemblyName);
				return Assembly.LoadFrom(path);
			}

			return null;
		}
	}
}
#endif
