// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.Logging;

namespace Elastic.Apm.StartupHook.Loader;

internal static class AssemblyLoadLogger
{
	// Assemblies with no independent diagnostic value — their identity is already captured by
	// the .NET runtime version string, or they are infrastructure shims.
	private static readonly HashSet<string> SkipByName = new(StringComparer.OrdinalIgnoreCase)
		{ "mscorlib", "netstandard", "System.Private.CoreLib", "dotnet" };

	private static readonly ConcurrentDictionary<string, byte> Logged = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Subscribes to <see cref="AppDomain.AssemblyLoad"/> and logs assemblies as they load.
	/// Also scans assemblies already loaded at call time. Subscription is established first
	/// so no assembly loaded during the initial scan is missed.
	/// </summary>
	internal static void Subscribe(IApmLogger logger)
	{
		AppDomain.CurrentDomain.AssemblyLoad += (_, args) => TryLog(logger, args.LoadedAssembly);

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			TryLog(logger, assembly);
	}

	private static void TryLog(IApmLogger logger, Assembly assembly)
	{
		if (!logger.IsEnabled(LogLevel.Debug))
			return;

		try
		{
			var name = assembly.GetName();
			if (string.IsNullOrEmpty(name.Name) || SkipByName.Contains(name.Name))
				return;

			// Key on name + assembly version: the same library name can appear multiple times in a
			// process when different versions are loaded into separate AssemblyLoadContexts (e.g.
			// plugin hosts, or the agent isolating its own dependencies). Each distinct binding
			// version is worth logging — it is precisely these conflicts that cause support issues.
			// Keying on assembly version (not informational version) matches the CLR binding identity.
			var assemblyVersion = name.Version?.ToString() ?? "unknown";
			if (!Logged.TryAdd($"{name.Name}|{assemblyVersion}", 0))
				return;

			// AssemblyInformationalVersion is the most precise — it carries the full semantic
			// version including pre-release labels and git commit SHA for official packages.
			// AssemblyVersion is logged separately because it is often locked to a major version
			// for binary compatibility and may differ significantly from the actual release version.
			var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
			var location = assembly.Location;

			var tokenBytes = name.GetPublicKeyToken();
			var publicKeyToken = tokenBytes is { Length: > 0 }
				? BitConverter.ToString(tokenBytes).Replace("-", "").ToLowerInvariant()
				: "null";

			if (informationalVersion != null)
			{
				logger.Debug()?.Log(
					"Assembly loaded: {AssemblyName}, AssemblyVersion={AssemblyVersion}, InformationalVersion={InformationalVersion}, PublicKeyToken={PublicKeyToken}, Location={Location}",
					name.Name, assemblyVersion, informationalVersion, publicKeyToken, location);
			}
			else
			{
				logger.Debug()?.Log(
					"Assembly loaded: {AssemblyName}, AssemblyVersion={AssemblyVersion}, PublicKeyToken={PublicKeyToken}, Location={Location}",
					name.Name, assemblyVersion, publicKeyToken, location);
			}
		}
		catch
		{
			logger.Error()?.Log("Failed to log assembly load for {Assembly}", assembly.FullName);
		}
	}
}
