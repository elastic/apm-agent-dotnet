// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="ModuleLookup.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Elastic.Apm.Profiler.Managed.Reflection
{
	internal static class ModuleLookup
	{
		/// <summary>
		/// Some naive upper limit to resolving assemblies that we can use to stop making expensive calls.
		/// </summary>
		private const int MaxFailures = 50;

		private static readonly ManualResetEventSlim _populationResetEvent = new(initialState: true);
		private static readonly ConcurrentDictionary<Guid, Module> _modules = new();

		private static long _failures;

		public static Module GetByPointer(long moduleVersionPointer) =>
			Get(Marshal.PtrToStructure<Guid>(new IntPtr(moduleVersionPointer)));

		private static readonly object _logLock = new();
		private static bool _shortCircuitLogicHasLogged = false;

		public static Module Get(Guid moduleVersionId)
		{
			// First attempt at cached values with no blocking
			if (_modules.TryGetValue(moduleVersionId, out var value))
				return value;

			// Block if a population event is happening
			_populationResetEvent.Wait();

			// See if the previous population event populated what we need
			if (_modules.TryGetValue(moduleVersionId, out value))
				return value;

			var failures = Interlocked.Read(ref _failures);
			if (failures >= MaxFailures)
			{
				if (_shortCircuitLogicHasLogged) return null;
				lock (_logLock)
				{
					if (_shortCircuitLogicHasLogged) return null;
					Logger.Log(LogLevel.Warn,
						"Elastic APM is unable to continue attempting module lookups for this AppDomain. Falling back to legacy method lookups.");
					_shortCircuitLogicHasLogged = true;

				}
				return null;
			}

			// Block threads on this event
			_populationResetEvent.Reset();

			try
			{
				PopulateModules();
			}
			catch (Exception ex)
			{
				Interlocked.Increment(ref _failures);
				Logger.Log(LogLevel.Error, ex, "Error when populating modules.");
			}
			finally
			{
				// Continue threads blocked on this event
				_populationResetEvent.Set();
			}

			_modules.TryGetValue(moduleVersionId, out value);

			return value;
		}

		private static void PopulateModules()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
				foreach (var module in assembly.Modules)
					_modules.TryAdd(module.ModuleVersionId, module);
		}
	}
}
