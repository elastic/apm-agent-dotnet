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
using Microsoft.Win32;

namespace Elastic.Apm.Profiler.Managed.Loader
{
	public partial class Startup
	{
		private static string ResolveDirectory()
		{
			var directory = ReadEnvironmentVariable("ELASTIC_APM_PROFILER_HOME") ?? string.Empty;

			// Prefer the net472 build on .NET Framework 4.7.2+ — it supports VerifyServerCert and ServerCert
			// via HttpClientHandler.ServerCertificateCustomValidationCallback (added in 4.7.1).
			// Fall back to net462 if the net472 directory is absent (e.g. older profiler zip) so loading
			// still succeeds rather than silently failing to resolve assemblies.
			if (IsNet472OrHigher())
			{
				var net472Dir = Path.Combine(directory, "net472");
				if (System.IO.Directory.Exists(net472Dir))
				{
					Logger.Log(LogLevel.Debug, "Resolving assemblies from {0}", net472Dir);
					return net472Dir;
				}

				Logger.Log(LogLevel.Warning,
					"Running on .NET Framework 4.7.2+ but net472 directory not found at {0}. "
					+ "Falling back to net462: VerifyServerCert and ServerCert will have no effect.",
					net472Dir);
			}

			var net462Dir = Path.Combine(directory, "net462");
			Logger.Log(LogLevel.Debug, "Resolving assemblies from {0}", net462Dir);
			return net462Dir;
		}

		private static bool IsNet472OrHigher()
		{
			try
			{
				using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
				// Release value 461808 corresponds to .NET Framework 4.7.2 on Windows 10 1803+;
				// 461814 is the value for all other OS versions. Checking >= 461808 covers both.
				var release = Convert.ToInt32(key?.GetValue("Release") ?? 0);
				return release >= 461808;
			}
			catch
			{
				return false;
			}
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
				return null;

			var path = Path.Combine(Directory, $"{assemblyName}.dll");
			var exists = File.Exists(path);

			Logger.Log(LogLevel.Trace, "Probing: {0}, exists on disk: {1}", path, exists);

			if (exists)
			{
				Logger.Log(LogLevel.Debug, "Loading {0} assembly", assemblyName);
				return Assembly.LoadFrom(path);
			}

			return null;
		}
	}
}
#endif
