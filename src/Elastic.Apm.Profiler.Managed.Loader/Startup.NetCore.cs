// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="Startup.NetCore.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>


#if !NETFRAMEWORK
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Elastic.Apm.Profiler.Managed.Loader
{
	public partial class Startup
    {
        private static AssemblyLoadContext DependencyLoadContext { get; } = new ProfilerAssemblyLoadContext();
        private static string ResolveDirectory()
        {
			var version = Environment.Version;
			// use netcoreapp3.1 for netcoreapp3.1 and later
			var framework = version.Major == 3 && version.Minor >= 1 || version.Major >= 5
				? "netcoreapp3.1"
				: "netstandard2.0";

            var directory = ReadEnvironmentVariable("ELASTIC_APM_PROFILER_HOME") ?? string.Empty;
            return Path.Combine(directory, framework);
        }

        private static Assembly ResolveDependencies(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            // Having a non-US locale can cause mscorlib
            // to enter the AssemblyResolve event when searching for resources
            // in its satellite assemblies. This seems to have been fixed in
            // .NET Core in the 2.0 servicing branch, so we should not see this
            // occur, but guard against it anyways. If we do see it, exit early
            // so we don't cause infinite recursion.
            if (string.Equals(assemblyName.Name, "System.Private.CoreLib.resources", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assemblyName.Name, "System.Net.Http", StringComparison.OrdinalIgnoreCase))
				return null;

			var path = Path.Combine(Directory, $"{assemblyName.Name}.dll");

            // Only load the main Elastic.Apm.Profiler.Managed.dll into the default Assembly Load Context.
            // If Elastic.Apm or other libraries are provided by the NuGet package, their loads are handled in the following two ways.
            // 1) The AssemblyVersion is greater than or equal to the version used by Elastic.Apm.Profiler.Managed, the assembly
            //    will load successfully and will not invoke this resolve event.
            // 2) The AssemblyVersion is lower than the version used by Elastic.Apm.Profiler.Managed, the assembly will fail to load
            //    and invoke this resolve event. It must be loaded in a separate AssemblyLoadContext since the application will only
            //    load the originally referenced version
			if (File.Exists(path))
			{
				if (assemblyName.Name.StartsWith("Elastic.Apm.Profiler.Managed", StringComparison.OrdinalIgnoreCase)
					&& assemblyName.FullName.IndexOf("PublicKeyToken=ae7400d2c189cf22", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					Logger.Log(LogLevel.Debug, "Loading {0} assembly using Assembly.LoadFrom", assemblyName);
					return Assembly.LoadFrom(path);
				}

				Logger.Log(LogLevel.Debug, "Loading {0} assembly using DependencyLoadContext.LoadFromAssemblyPath", assemblyName);
				return DependencyLoadContext.LoadFromAssemblyPath(path);
			}

			return null;
        }
    }

    internal class ProfilerAssemblyLoadContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName) => null;
    }
}
#endif
