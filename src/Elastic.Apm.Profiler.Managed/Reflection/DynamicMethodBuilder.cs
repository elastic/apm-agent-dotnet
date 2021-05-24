// Based on .NET Tracer for Datadog APM by Datadog
// https://github.com/DataDog/dd-trace-dotnet
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Elastic.Apm.Profiler.Managed.Reflection
{
    /// <summary>
    /// Helper class to create instances of <see cref="DynamicMethod"/> using <see cref="System.Reflection.Emit"/>.
    /// </summary>
    /// <typeparam name="TDelegate">The type of delegate</typeparam>
    internal static class DynamicMethodBuilder<TDelegate>
        where TDelegate : Delegate
    {
        private static readonly ConcurrentDictionary<Key, TDelegate> Cache = new ConcurrentDictionary<Key, TDelegate>(new KeyComparer());

        /// <summary>
        /// Gets a previously cache delegate used to call the specified method,
        /// or creates and caches a new delegate if not found.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> that contains the method.</param>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="callOpCode">The OpCode to use in the method call.</param>
        /// <param name="returnType">The method's return type.</param>
        /// <param name="methodParameterTypes">optional types for the method parameters</param>
        /// <param name="methodGenericArguments">optional generic type arguments for a generic method</param>
        /// <returns>A <see cref="Delegate"/> that can be used to execute the dynamic method.</returns>
        public static TDelegate GetOrCreateMethodCallDelegate(
            Type type,
            string methodName,
            OpCodeValue callOpCode,
            Type returnType = null,
            Type[] methodParameterTypes = null,
            Type[] methodGenericArguments = null) =>
			Cache.GetOrAdd(
				new Key(type, methodName, callOpCode, returnType, methodParameterTypes, methodGenericArguments),
				key => CreateMethodCallDelegate(
					key.Type,
					key.MethodName,
					key.CallOpCode,
					key.MethodParameterTypes,
					key.MethodGenericArguments));

		/// <summary>
        /// Creates a simple <see cref="DynamicMethod"/> using <see cref="System.Reflection.Emit"/> that
        /// calls a method with the specified name and parameter types.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> that contains the method to call when the returned delegate is executed..</param>
        /// <param name="methodName">The name of the method to call when the returned delegate is executed.</param>
        /// <param name="callOpCode">The OpCode to use in the method call.</param>
        /// <param name="methodParameterTypes">If not null, use method overload that matches the specified parameters.</param>
        /// <param name="methodGenericArguments">If not null, use method overload that has the same number of generic arguments.</param>
        /// <returns>A <see cref="Delegate"/> that can be used to execute the dynamic method.</returns>
        public static TDelegate CreateMethodCallDelegate(
            Type type,
            string methodName,
            OpCodeValue callOpCode,
            Type[] methodParameterTypes = null,
            Type[] methodGenericArguments = null)
        {
            var delegateType = typeof(TDelegate);
            var genericTypeArguments = delegateType.GenericTypeArguments;

            Type[] parameterTypes;
            Type returnType;

            if (delegateType.Name.StartsWith("Func`"))
            {
                // last generic type argument is the return type
                var parameterCount = genericTypeArguments.Length - 1;
                parameterTypes = new Type[parameterCount];
                Array.Copy(genericTypeArguments, parameterTypes, parameterCount);

                returnType = genericTypeArguments[parameterCount];
            }
            else if (delegateType.Name.StartsWith("Action`"))
            {
                parameterTypes = genericTypeArguments;
                returnType = typeof(void);
            }
            else
            {
                throw new Exception($"Only Func<> or Action<> are supported in {nameof(CreateMethodCallDelegate)}.");
            }

            // find any method that matches by name and parameter types
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                                  .Where(m => m.Name == methodName);

            // if methodParameterTypes was specified, check for a method that matches
            if (methodParameterTypes != null)
            {
                methods = methods.Where(
                    m =>
                    {
                        var ps = m.GetParameters();
                        if (ps.Length != methodParameterTypes.Length)
                        {
                            return false;
                        }

                        for (var i = 0; i < ps.Length; i++)
                        {
                            var t1 = ps[i].ParameterType;
                            var t2 = methodParameterTypes[i];

                            // generics can be tricky to compare for type equality
                            // so we will just check the namespace and name
                            if (t1.Namespace != t2.Namespace || t1.Name != t2.Name)
                            {
                                return false;
                            }
                        }

                        return true;
                    });
            }

            if (methodGenericArguments != null)
            {
                methods = methods.Where(
                    m => m.IsGenericMethodDefinition &&
                         m.GetGenericArguments().Length == methodGenericArguments.Length);
            }

            var methodInfo = methods.FirstOrDefault();
            if (methodInfo == null)
            {
                // method not found
                // TODO: logging
                return null;
            }

            if (methodGenericArguments != null)
            {
                methodInfo = methodInfo.MakeGenericMethod(methodGenericArguments);
            }

            Type[] effectiveParameterTypes;

            var reflectedParameterTypes = methodInfo.GetParameters()
                                                                  .Select(p => p.ParameterType);
            if (methodInfo.IsStatic)
            {
                effectiveParameterTypes = reflectedParameterTypes.ToArray();
            }
            else
            {
                // for instance methods, insert object's type as first element in array
                effectiveParameterTypes = new[] { type }
                                         .Concat(reflectedParameterTypes)
                                         .ToArray();
            }

            var dynamicMethod = new DynamicMethod(methodInfo.Name, returnType, parameterTypes, ObjectExtensions.Module, skipVisibility: true);
            var il = dynamicMethod.GetILGenerator();

            // load each argument and cast or unbox as necessary
            for (ushort argumentIndex = 0; argumentIndex < parameterTypes.Length; argumentIndex++)
            {
                var delegateParameterType = parameterTypes[argumentIndex];
                var underlyingParameterType = effectiveParameterTypes[argumentIndex];

                switch (argumentIndex)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        il.Emit(OpCodes.Ldarg_S, argumentIndex);
                        break;
                }

                if (underlyingParameterType.IsValueType && delegateParameterType == typeof(object))
                {
                    il.Emit(OpCodes.Unbox_Any, underlyingParameterType);
                }
                else if (underlyingParameterType != delegateParameterType)
                {
                    il.Emit(OpCodes.Castclass, underlyingParameterType);
                }
            }

            if (callOpCode == OpCodeValue.Call || methodInfo.IsStatic)
            {
                // non-virtual call (e.g. static method, or method override calling overriden implementation)
                il.Emit(OpCodes.Call, methodInfo);
            }
            else if (callOpCode == OpCodeValue.Callvirt)
            {
                // Note: C# compiler uses CALLVIRT for non-virtual
                // instance methods to get the cheap null check
                il.Emit(OpCodes.Callvirt, methodInfo);
            }
            else
            {
                throw new NotSupportedException($"OpCode {callOpCode} not supported when calling a method.");
            }

            if (methodInfo.ReturnType.IsValueType && !returnType.IsValueType)
            {
                il.Emit(OpCodes.Box, methodInfo.ReturnType);
            }
            else if (methodInfo.ReturnType.IsValueType && returnType.IsValueType && methodInfo.ReturnType != returnType)
            {
                throw new ArgumentException($"Cannot convert the target method's return type {methodInfo.ReturnType.FullName} (valuetype) to the delegate method's return type {returnType.FullName} (valuetype)");
            }
            else if (!methodInfo.ReturnType.IsValueType && returnType.IsValueType)
            {
                throw new ArgumentException($"Cannot reliably convert the target method's return type {methodInfo.ReturnType.FullName} (reference type) to the delegate method's return type {returnType.FullName} (value type)");
            }
            else if (!methodInfo.ReturnType.IsValueType && !returnType.IsValueType && methodInfo.ReturnType != returnType)
            {
                il.Emit(OpCodes.Castclass, returnType);
            }

            il.Emit(OpCodes.Ret);
            return (TDelegate)dynamicMethod.CreateDelegate(delegateType);
        }

        private struct Key
        {
            public readonly Type Type;
            public readonly string MethodName;
            public readonly OpCodeValue CallOpCode;
            public readonly Type ReturnType;
            public readonly Type[] MethodParameterTypes;
            public readonly Type[] MethodGenericArguments;

            public Key(Type type, string methodName, OpCodeValue callOpCode, Type returnType, Type[] methodParameterTypes, Type[] methodGenericArguments)
            {
                Type = type;
                MethodName = methodName;
                CallOpCode = callOpCode;
                ReturnType = returnType;
                MethodParameterTypes = methodParameterTypes;
                MethodGenericArguments = methodGenericArguments;
            }
        }

        private class KeyComparer : IEqualityComparer<Key>
        {
            public bool Equals(Key x, Key y)
            {
                if (!object.Equals(x.Type, y.Type))
                {
                    return false;
                }

                if (!object.Equals(x.MethodName, y.MethodName))
                {
                    return false;
                }

                if (!object.Equals(x.CallOpCode, y.CallOpCode))
                {
                    return false;
                }

                if (!object.Equals(x.ReturnType, y.ReturnType))
                {
                    return false;
                }

                if (!ArrayEquals(x.MethodParameterTypes, y.MethodParameterTypes))
                {
                    return false;
                }

                if (!ArrayEquals(x.MethodGenericArguments, y.MethodGenericArguments))
                {
                    return false;
                }

                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    var hash = 17;

                    if (obj.Type != null)
                    {
                        hash = (hash * 23) + obj.Type.GetHashCode();
                    }

                    if (obj.MethodName != null)
                    {
                        hash = (hash * 23) + obj.MethodName.GetHashCode();
                    }

                    hash = (hash * 23) + obj.CallOpCode.GetHashCode();

                    if (obj.MethodParameterTypes != null)
                    {
                        foreach (var t in obj.MethodParameterTypes)
                        {
                            if (t != null)
                            {
                                hash = (hash * 23) + t.GetHashCode();
                            }
                        }
                    }

                    if (obj.MethodGenericArguments != null)
                    {
                        foreach (var t in obj.MethodGenericArguments)
                        {
                            if (t != null)
                            {
                                hash = (hash * 23) + t.GetHashCode();
                            }
                        }
                    }

                    return hash;
                }
            }

            private static bool ArrayEquals<T>(T[] array1, T[] array2)
            {
                if (array1 == null && array2 == null)
                {
                    return true;
                }

                if (array1 == null || array2 == null)
                {
                    return false;
                }

                return ((IStructuralEquatable)array1).Equals(array2, EqualityComparer<T>.Default);
            }
        }
    }
}
