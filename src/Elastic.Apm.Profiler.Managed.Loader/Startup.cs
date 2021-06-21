// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif
namespace Elastic.Apm.Profiler.Managed.Loader
{
    public partial class Startup
    {
        static Startup()
        {
            Console.WriteLine("Startup called");

            Directory = ResolveDirectory();
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveDependencies;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        internal static string Directory { get; }
        
        private static string ReadEnvironmentVariable(string key)
        {
            try
            {
                return Environment.GetEnvironmentVariable(key);
            }
            catch (Exception ex)
            {
                // TODO! logging
            }

            return null;
        }
    }
    
#if NETFRAMEWORK
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
                // TODO! logging
                return Assembly.LoadFrom(path);
            }

            return null;
        }
    }
#endif
    
#if !NETFRAMEWORK
    public partial class Startup
    {
        private static AssemblyLoadContext DependencyLoadContext { get; } = new ProfilerAssemblyLoadContext();
        private static string ResolveDirectory()
        {
            var framework = "netstandard2.0";
            var directory = ReadEnvironmentVariable("PROFILER_HOME") ?? string.Empty;
            return Path.Combine(directory, framework);
        }
        
        private static Assembly ResolveDependencies(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            // On .NET Framework, having a non-US locale can cause mscorlib
            // to enter the AssemblyResolve event when searching for resources
            // in its satellite assemblies. This seems to have been fixed in
            // .NET Core in the 2.0 servicing branch, so we should not see this
            // occur, but guard against it anyways. If we do see it, exit early
            // so we don't cause infinite recursion.
            if (string.Equals(assemblyName.Name, "System.Private.CoreLib.resources", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assemblyName.Name, "System.Net.Http", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var path = Path.Combine(Directory, $"{assemblyName.Name}.dll");

            // Only load the main dll into the default Assembly Load Context.
            // If other libraries are provided by the NuGet package their loads are handled in the following two ways.
            // 1) The AssemblyVersion is greater than or equal to the version used by Profiler.Managed, the assembly
            //    will load successfully and will not invoke this resolve event.
            // 2) The AssemblyVersion is lower than the version used by Profiler.Managed, the assembly will fail to load
            //    and invoke this resolve event. It must be loaded in a separate AssemblyLoadContext since the application will only
            //    load the originally referenced version
            if (assemblyName.Name.StartsWith("Profiler.Managed", StringComparison.OrdinalIgnoreCase)
                && assemblyName.FullName.IndexOf("PublicKeyToken=ae7400d2c189cf22", StringComparison.OrdinalIgnoreCase) >= 0
                && File.Exists(path))
            {
                // TODO! logging
                return Assembly.LoadFrom(path);
            }
            else if (File.Exists(path))
            {
                // TODO! logging
                return DependencyLoadContext.LoadFromAssemblyPath(path);
            }

            return null;
        }
    }
    
    internal class ProfilerAssemblyLoadContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName) => null;
    }
#endif
    
}