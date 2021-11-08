// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ILHelpersExtensions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Elastic.Apm.Profiler.Managed.DuckTyping
{
    /// <summary>
    /// Internal IL Helpers
    /// </summary>
    internal static class ILHelpersExtensions
    {
        private static List<DynamicMethod> _dynamicMethods = new List<DynamicMethod>();

        internal static DynamicMethod GetDynamicMethodForIndex(int index)
        {
            lock (_dynamicMethods)
				return _dynamicMethods[index];
		}

        internal static void CreateDelegateTypeFor(TypeBuilder proxyType, DynamicMethod dynamicMethod, out Type delType, out MethodInfo invokeMethod)
        {
			var modBuilder = (ModuleBuilder)proxyType.Module;
			var delegateType = modBuilder.DefineType($"{dynamicMethod.Name}Delegate_" + Guid.NewGuid().ToString("N"), TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, typeof(MulticastDelegate));

            // Delegate .ctor
			var constructorBuilder = delegateType.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(object), typeof(IntPtr) });
            constructorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            // Define the Invoke method for the delegate
			var parameters = dynamicMethod.GetParameters();
			var paramTypes = new Type[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                paramTypes[i] = parameters[i].ParameterType;
            }

			var methodBuilder = delegateType.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, dynamicMethod.ReturnType, paramTypes);
            for (var i = 0; i < parameters.Length; i++)
            {
                methodBuilder.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);
            }

            methodBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            delType = delegateType.CreateTypeInfo().AsType();
            invokeMethod = delType.GetMethod("Invoke");
        }

        /// <summary>
        /// Load instance argument
        /// </summary>
        /// <param name="il">LazyILGenerator instance</param>
        /// <param name="actualType">Actual type</param>
        /// <param name="expectedType">Expected type</param>
        internal static void LoadInstanceArgument(this LazyILGenerator il, Type actualType, Type expectedType)
        {
            il.Emit(OpCodes.Ldarg_0);
            if (actualType == expectedType)
            {
                return;
            }

            if (expectedType.IsValueType)
            {
                il.DeclareLocal(expectedType);
                il.Emit(OpCodes.Unbox_Any, expectedType);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, 0);
            }
            else
            {
                il.Emit(OpCodes.Castclass, expectedType);
            }
        }

        /// <summary>
        /// Write load arguments
        /// </summary>
        /// <param name="il">LazyILGenerator instance</param>
        /// <param name="index">Argument index</param>
        /// <param name="isStatic">Define if we need to take into account the instance argument</param>
        internal static void WriteLoadArgument(this LazyILGenerator il, int index, bool isStatic)
        {
            if (!isStatic)
            {
                index += 1;
            }

            switch (index)
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
                    il.Emit(OpCodes.Ldarg_S, index);
                    break;
            }
        }

        /// <summary>
        /// Write load local
        /// </summary>
        /// <param name="il">LazyILGenerator instance</param>
        /// <param name="index">Local index</param>
        internal static void WriteLoadLocal(this LazyILGenerator il, int index)
        {
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldloc_3);
                    break;
                default:
                    il.Emit(OpCodes.Ldloc_S, index);
                    break;
            }
        }

        /// <summary>
        /// Write load local
        /// </summary>
        /// <param name="il">ILGenerator instance</param>
        /// <param name="index">Local index</param>
        internal static void WriteLoadLocal(this ILGenerator il, int index)
        {
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldloc_3);
                    break;
                default:
                    il.Emit(OpCodes.Ldloc_S, index);
                    break;
            }
        }

        /// <summary>
        /// Write store local
        /// </summary>
        /// <param name="il">LazyILGenerator instance</param>
        /// <param name="index">Local index</param>
        internal static void WriteStoreLocal(this LazyILGenerator il, int index)
        {
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Stloc_3);
                    break;
                default:
                    il.Emit(OpCodes.Stloc_S, index);
                    break;
            }
        }

        /// <summary>
        /// Write constant int value
        /// </summary>
        /// <param name="il">LazyILGenerator instance</param>
        /// <param name="value">int value</param>
        internal static void WriteInt(this LazyILGenerator il, int value)
        {
            if (value >= -1 && value <= 8)
            {
                switch (value)
                {
                    case -1:
                        il.Emit(OpCodes.Ldc_I4_M1);
                        break;
                    case 0:
                        il.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldc_I4_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldc_I4_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldc_I4_3);
                        break;
                    case 4:
                        il.Emit(OpCodes.Ldc_I4_4);
                        break;
                    case 5:
                        il.Emit(OpCodes.Ldc_I4_5);
                        break;
                    case 6:
                        il.Emit(OpCodes.Ldc_I4_6);
                        break;
                    case 7:
                        il.Emit(OpCodes.Ldc_I4_7);
                        break;
                    default:
                        il.Emit(OpCodes.Ldc_I4_8);
                        break;
                }
            }
            else if (value >= -128 && value <= 127)
            {
                il.Emit(OpCodes.Ldc_I4_S, value);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, value);
            }
        }

        /// <summary>
        /// Convert a current type to an expected type
        /// </summary>
        /// <param name="il">LazyILGenerator instance</param>
        /// <param name="actualType">Actual type</param>
        /// <param name="expectedType">Expected type</param>
        internal static void WriteTypeConversion(this LazyILGenerator il, Type actualType, Type expectedType)
        {
            var actualUnderlyingType = actualType.IsEnum ? Enum.GetUnderlyingType(actualType) : actualType;
            var expectedUnderlyingType = expectedType.IsEnum ? Enum.GetUnderlyingType(expectedType) : expectedType;

            if (actualUnderlyingType == expectedUnderlyingType)
            {
                return;
            }

            if (actualUnderlyingType.IsValueType)
            {
                if (expectedUnderlyingType.IsValueType)
                {
                    // If both underlying types are value types then both must be of the same type.
                    DuckTypeInvalidTypeConversionException.Throw(actualType, expectedType);
                }
                else
                {
                    // An underlying type can be boxed and converted to an object or interface type if the actual type support this
                    // if not we should throw.
                    if (expectedUnderlyingType == typeof(object))
                    {
                        // If the expected type is object we just need to box the value
                        il.Emit(OpCodes.Box, actualType);
                    }
                    else if (expectedUnderlyingType.IsAssignableFrom(actualUnderlyingType))
                    {
                        // If the expected type can be assigned from the value type (ex: struct implementing an interface)
                        il.Emit(OpCodes.Box, actualType);
                        il.Emit(OpCodes.Castclass, expectedType);
                    }
                    else
                    {
                        // If the expected type can't be assigned from the actual value type.
                        // Means if the expected type is an interface the actual type doesn't implement it.
                        // So no possible conversion or casting can be made here.
                        DuckTypeInvalidTypeConversionException.Throw(actualType, expectedType);
                    }
                }
            }
            else
            {
                if (expectedUnderlyingType.IsValueType)
                {
                    // We only allow conversions from objects or interface type if the actual type support this
                    // if not we should throw.
                    if (actualUnderlyingType == typeof(object) || actualUnderlyingType.IsAssignableFrom(expectedUnderlyingType))
                    {
                        // WARNING: The actual type instance can't be detected at this point, we have to check it at runtime.
                        /*
                         * In this case we emit something like:
                         * {
                         *      if (!(value is [expectedType])) {
                         *          throw new InvalidCastException();
                         *      }
                         *
                         *      return ([expectedType])value;
                         * }
                         */
						var lblIsExpected = il.DefineLabel();

                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Isinst, expectedType);
                        il.Emit(OpCodes.Brtrue_S, lblIsExpected);

                        il.Emit(OpCodes.Pop);
                        il.ThrowException(typeof(InvalidCastException));

                        il.MarkLabel(lblIsExpected);
                        il.Emit(OpCodes.Unbox_Any, expectedType);
                    }
                    else
                    {
                        DuckTypeInvalidTypeConversionException.Throw(actualType, expectedType);
                    }
                }
                else if (expectedUnderlyingType != typeof(object))
                {
                    il.Emit(OpCodes.Castclass, expectedUnderlyingType);
                }
            }
        }

        /// <summary>
        /// Write a Call to a method using Calli
        /// </summary>
        /// <param name="il">LazyILGenerator instance</param>
        /// <param name="method">Method to get called</param>
        internal static void WriteMethodCalli(this LazyILGenerator il, MethodInfo method)
        {
            il.Emit(OpCodes.Ldc_I8, (long)method.MethodHandle.GetFunctionPointer());
            il.Emit(OpCodes.Conv_I);
            il.EmitCalli(
                OpCodes.Calli,
                method.CallingConvention,
                method.ReturnType,
                method.GetParameters().Select(p => p.ParameterType).ToArray(),
                null);
        }

        /// <summary>
        /// Write a DynamicMethod call by creating and injecting a custom delegate in the proxyType
        /// </summary>
        /// <param name="il">LazyILGenerator instance</param>
        /// <param name="dynamicMethod">Dynamic method to get called</param>
        /// <param name="proxyType">ProxyType builder</param>
        internal static void WriteDynamicMethodCall(this LazyILGenerator il, DynamicMethod dynamicMethod, TypeBuilder proxyType)
        {
            // We create a custom delegate inside the module builder
            CreateDelegateTypeFor(proxyType, dynamicMethod, out var delegateType, out var invokeMethod);
            int index;
            lock (_dynamicMethods)
            {
                _dynamicMethods.Add(dynamicMethod);
                index = _dynamicMethods.Count - 1;
            }

            // We fill the DelegateCache<> for that custom type with the delegate instance
			var fillDelegateMethodInfo = typeof(Elastic.Apm.Profiler.Managed.DuckTyping.DuckType.DelegateCache<>).MakeGenericType(delegateType).GetMethod("FillDelegate", BindingFlags.NonPublic | BindingFlags.Static);
            fillDelegateMethodInfo.Invoke(null, new object[] { index });

            // We get the delegate instance and load it in to the stack before the parameters (at the begining of the IL body)
            il.SetOffset(0);
            il.EmitCall(OpCodes.Call, typeof(DuckType.DelegateCache<>).MakeGenericType(delegateType).GetMethod("GetDelegate"), null);
            il.ResetOffset();

            // We emit the call to the delegate invoke method.
            il.EmitCall(OpCodes.Callvirt, invokeMethod, null);
        }
    }
}
