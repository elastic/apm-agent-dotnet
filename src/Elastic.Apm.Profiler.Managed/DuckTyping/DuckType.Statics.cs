// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="DuckType.Statics.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Elastic.Apm.Profiler.Managed.DuckTyping
{
    /// <summary>
    /// Duck Type
    /// </summary>
    public static partial class DuckType
    {
        /// <summary>
        /// Gets the Type.GetTypeFromHandle method info
        /// </summary>
        public static readonly MethodInfo GetTypeFromHandleMethodInfo = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));

        /// <summary>
        /// Gets the Enum.ToObject method info
        /// </summary>
        public static readonly MethodInfo EnumToObjectMethodInfo = typeof(Enum).GetMethod(nameof(Enum.ToObject), new[] { typeof(Type), typeof(object) });

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly object _locker = new object();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly ConcurrentDictionary<TypesTuple, Lazy<CreateTypeResult>> DuckTypeCache = new ConcurrentDictionary<TypesTuple, Lazy<DuckType.CreateTypeResult>>();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly PropertyInfo DuckTypeInstancePropertyInfo = typeof(IDuckType).GetProperty(nameof(IDuckType.Instance));
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly MethodInfo _methodBuilderGetToken = typeof(MethodBuilder).GetMethod("GetToken", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Dictionary<Assembly, ModuleBuilder> ActiveBuilders = new Dictionary<Assembly, ModuleBuilder>();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static long _assemblyCount = 0;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static long _typeCount = 0;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static ConstructorInfo _ignoresAccessChecksToAttributeCtor = typeof(IgnoresAccessChecksToAttribute).GetConstructor(new Type[] { typeof(string) });

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static Dictionary<ModuleBuilder, HashSet<string>> _ignoresAccessChecksToAssembliesSetDictionary = new Dictionary<ModuleBuilder, HashSet<string>>();

        internal static long AssemblyCount => _assemblyCount;

        internal static long TypeCount => _typeCount;

        /// <summary>
        /// Gets the ModuleBuilder instance from a target type.  (.NET Framework / Non AssemblyLoadContext version)
        /// </summary>
        /// <param name="targetType">Target type for ducktyping</param>
        /// <param name="isVisible">Is visible boolean</param>
        /// <returns>ModuleBuilder instance</returns>
        private static ModuleBuilder GetModuleBuilder(Type targetType, bool isVisible)
        {
            var targetAssembly = targetType.Assembly ?? typeof(DuckType).Assembly;

            if (!isVisible)
            {
                // If the target type is not visible then we create a new module builder.
                // This is the only way to IgnoresAccessChecksToAttribute to work.
                // We can't reuse the module builder if the attributes collection changes.
                return CreateModuleBuilder($"DuckTypeNotVisibleAssembly.{targetType.Name}", targetAssembly);
            }

            if (targetType.IsGenericType)
            {
                foreach (var type in targetType.GetGenericArguments())
                {
                    if (type.Assembly != targetAssembly)
                    {
                        return CreateModuleBuilder($"DuckTypeGenericTypeAssembly.{targetType.Name}", targetAssembly);
                    }
                }
            }

            if (!ActiveBuilders.TryGetValue(targetAssembly, out var moduleBuilder))
            {
                moduleBuilder = CreateModuleBuilder($"DuckTypeAssembly.{targetType.Assembly?.GetName().Name}", targetAssembly);
                ActiveBuilders.Add(targetAssembly, moduleBuilder);
            }

            return moduleBuilder;

            static ModuleBuilder CreateModuleBuilder(string name, Assembly targetAssembly)
            {
                var assemblyName = new AssemblyName(name + $"_{++_assemblyCount}");
                assemblyName.Version = targetAssembly.GetName().Version;
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                return assemblyBuilder.DefineDynamicModule("MainModule");
            }
        }

        /// <summary>
        /// DynamicMethods delegates cache
        /// </summary>
        /// <typeparam name="TProxyDelegate">Proxy delegate type</typeparam>
        public static class DelegateCache<TProxyDelegate>
            where TProxyDelegate : Delegate
        {
            private static TProxyDelegate _delegate;

            /// <summary>
            /// Get cached delegate from the DynamicMethod
            /// </summary>
            /// <returns>TProxyDelegate instance</returns>
            public static TProxyDelegate GetDelegate() => _delegate;

			/// <summary>
            /// Create delegate from a DynamicMethod index
            /// </summary>
            /// <param name="index">Dynamic method index</param>
            internal static void FillDelegate(int index) =>
				_delegate = (TProxyDelegate)ILHelpersExtensions.GetDynamicMethodForIndex(index)
					.CreateDelegate(typeof(TProxyDelegate));
		}
    }
}
