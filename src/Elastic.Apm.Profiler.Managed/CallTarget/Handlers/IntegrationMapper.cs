// <copyright file="IntegrationMapper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Elastic.Apm.Profiler.Managed.DuckTyping;

namespace Elastic.Apm.Profiler.Managed.CallTarget.Handlers
{
    internal class IntegrationMapper
    {
        private const string BeginMethodName = "OnMethodBegin";
        private const string EndMethodName = "OnMethodEnd";
        private const string EndAsyncMethodName = "OnAsyncMethodEnd";

        private static readonly NullLogger Log = NullLogger.Instance;
        private static readonly MethodInfo UnwrapReturnValueMethodInfo = typeof(IntegrationMapper).GetMethod(nameof(IntegrationMapper.UnwrapReturnValue), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ConvertTypeMethodInfo = typeof(IntegrationMapper).GetMethod(nameof(IntegrationMapper.ConvertType), BindingFlags.NonPublic | BindingFlags.Static);

        internal static DynamicMethod CreateBeginMethodDelegate(Type integrationType, Type targetType, Type[] argumentsTypes)
        {
            /*
             * OnMethodBegin signatures with 1 or more parameters with 1 or more generics:
             *      - CallTargetState OnMethodBegin<TTarget>(TTarget instance);
             *      - CallTargetState OnMethodBegin<TTarget, TArg1>(TTarget instance, TArg1 arg1);
             *      - CallTargetState OnMethodBegin<TTarget, TArg1, TArg2>(TTarget instance, TArg1 arg1, TArg2);
             *      - CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, ...>(TTarget instance, TArg1 arg1, TArg2, ...);
             *      - CallTargetState OnMethodBegin<TTarget>();
             *      - CallTargetState OnMethodBegin<TTarget, TArg1>(TArg1 arg1);
             *      - CallTargetState OnMethodBegin<TTarget, TArg1, TArg2>(TArg1 arg1, TArg2);
             *      - CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, ...>(TArg1 arg1, TArg2, ...);
             *
             */

            Log.Debug($"Creating BeginMethod Dynamic Method for '{integrationType.FullName}' integration. [Target={targetType.FullName}]");
            var onMethodBeginMethodInfo = integrationType.GetMethod(BeginMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (onMethodBeginMethodInfo is null)
            {
                return null;
            }

            if (onMethodBeginMethodInfo.ReturnType != typeof(CallTargetState))
            {
                throw new ArgumentException($"The return type of the method: {BeginMethodName} in type: {integrationType.FullName} is not {nameof(CallTargetState)}");
            }

            var genericArgumentsTypes = onMethodBeginMethodInfo.GetGenericArguments();
            if (genericArgumentsTypes.Length < 1)
            {
                throw new ArgumentException($"The method: {BeginMethodName} in type: {integrationType.FullName} doesn't have the generic type for the instance type.");
            }

            var onMethodBeginParameters = onMethodBeginMethodInfo.GetParameters();
            if (onMethodBeginParameters.Length < argumentsTypes.Length)
            {
                throw new ArgumentException($"The method: {BeginMethodName} with {onMethodBeginParameters.Length} parameters in type: {integrationType.FullName} has less parameters than required.");
            }
            else if (onMethodBeginParameters.Length > argumentsTypes.Length + 1)
            {
                throw new ArgumentException($"The method: {BeginMethodName} with {onMethodBeginParameters.Length} parameters in type: {integrationType.FullName} has more parameters than required.");
            }
            else if (onMethodBeginParameters.Length != argumentsTypes.Length && onMethodBeginParameters[0].ParameterType != genericArgumentsTypes[0])
            {
                throw new ArgumentException($"The first generic argument for method: {BeginMethodName} in type: {integrationType.FullName} must be the same as the first parameter for the instance value.");
            }

            var callGenericTypes = new List<Type>();

            var mustLoadInstance = onMethodBeginParameters.Length != argumentsTypes.Length;
            var instanceGenericType = genericArgumentsTypes[0];
            var instanceGenericConstraint = instanceGenericType.GetGenericParameterConstraints().FirstOrDefault();
            Type instanceProxyType = null;
            if (instanceGenericConstraint != null)
            {
                var result = DuckType.GetOrCreateProxyType(instanceGenericConstraint, targetType);
                instanceProxyType = result.ProxyType;
                callGenericTypes.Add(instanceProxyType);
            }
            else
            {
                callGenericTypes.Add(targetType);
            }

            var callMethod = new DynamicMethod(
                     $"{onMethodBeginMethodInfo.DeclaringType.Name}.{onMethodBeginMethodInfo.Name}",
                     typeof(CallTargetState),
                     new Type[] { targetType }.Concat(argumentsTypes).ToArray(),
                     onMethodBeginMethodInfo.Module,
                     true);

            var ilWriter = callMethod.GetILGenerator();

            // Load the instance if is needed
            if (mustLoadInstance)
            {
                ilWriter.Emit(OpCodes.Ldarg_0);

                if (instanceGenericConstraint != null)
                {
                    WriteCreateNewProxyInstance(ilWriter, instanceProxyType, targetType);
                }
            }

            // Load arguments
            for (var i = mustLoadInstance ? 1 : 0; i < onMethodBeginParameters.Length; i++)
            {
                var sourceParameterType = argumentsTypes[mustLoadInstance ? i - 1 : i];
                var targetParameterType = onMethodBeginParameters[i].ParameterType;
                Type targetParameterTypeConstraint = null;
                Type parameterProxyType = null;

                if (targetParameterType.IsGenericParameter)
                {
                    targetParameterType = genericArgumentsTypes[targetParameterType.GenericParameterPosition];
                    targetParameterTypeConstraint = targetParameterType.GetGenericParameterConstraints().FirstOrDefault(pType => pType != typeof(IDuckType));
                    if (targetParameterTypeConstraint is null)
                    {
                        callGenericTypes.Add(sourceParameterType);
                    }
                    else
                    {
                        var result = DuckType.GetOrCreateProxyType(targetParameterTypeConstraint, sourceParameterType);
                        parameterProxyType = result.ProxyType;
                        callGenericTypes.Add(parameterProxyType);
                    }
                }
                else if (!targetParameterType.IsAssignableFrom(sourceParameterType) && (!(sourceParameterType.IsEnum && targetParameterType.IsEnum)))
                {
                    throw new InvalidCastException($"The target parameter {targetParameterType} can't be assigned from {sourceParameterType}");
                }

                WriteLoadArgument(ilWriter, i, mustLoadInstance);
                if (parameterProxyType != null)
                {
                    WriteCreateNewProxyInstance(ilWriter, parameterProxyType, sourceParameterType);
                }
            }

            // Call method
            onMethodBeginMethodInfo = onMethodBeginMethodInfo.MakeGenericMethod(callGenericTypes.ToArray());
            ilWriter.EmitCall(OpCodes.Call, onMethodBeginMethodInfo, null);
            ilWriter.Emit(OpCodes.Ret);

            Log.Debug($"Created BeginMethod Dynamic Method for '{integrationType.FullName}' integration. [Target={targetType.FullName}]");
            return callMethod;
        }

        internal static DynamicMethod CreateSlowBeginMethodDelegate(Type integrationType, Type targetType)
        {
            /*
             * OnMethodBegin signatures with 1 or more parameters with 1 or more generics:
             *      - CallTargetState OnMethodBegin<TTarget>(TTarget instance);
             *      - CallTargetState OnMethodBegin<TTarget, TArg1>(TTarget instance, TArg1 arg1);
             *      - CallTargetState OnMethodBegin<TTarget, TArg1, TArg2>(TTarget instance, TArg1 arg1, TArg2);
             *      - CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, ...>(TTarget instance, TArg1 arg1, TArg2, ...);
             *      - CallTargetState OnMethodBegin<TTarget>();
             *      - CallTargetState OnMethodBegin<TTarget, TArg1>(TArg1 arg1);
             *      - CallTargetState OnMethodBegin<TTarget, TArg1, TArg2>(TArg1 arg1, TArg2);
             *      - CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, ...>(TArg1 arg1, TArg2, ...);
             *
             */

            Log.Debug($"Creating SlowBeginMethod Dynamic Method for '{integrationType.FullName}' integration. [Target={targetType.FullName}]");
            var onMethodBeginMethodInfo = integrationType.GetMethod(BeginMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (onMethodBeginMethodInfo is null)
            {
                throw new NullReferenceException($"Couldn't find the method: {BeginMethodName} in type: {integrationType.FullName}");
            }

            if (onMethodBeginMethodInfo.ReturnType != typeof(CallTargetState))
            {
                throw new ArgumentException($"The return type of the method: {BeginMethodName} in type: {integrationType.FullName} is not {nameof(CallTargetState)}");
            }

            var genericArgumentsTypes = onMethodBeginMethodInfo.GetGenericArguments();
            if (genericArgumentsTypes.Length < 1)
            {
                throw new ArgumentException($"The method: {BeginMethodName} in type: {integrationType.FullName} doesn't have the generic type for the instance type.");
            }

            var onMethodBeginParameters = onMethodBeginMethodInfo.GetParameters();

            var callGenericTypes = new List<Type>();

            var mustLoadInstance = onMethodBeginParameters[0].ParameterType.IsGenericParameter && onMethodBeginParameters[0].ParameterType.GenericParameterPosition == 0;
            var instanceGenericType = genericArgumentsTypes[0];
            var instanceGenericConstraint = instanceGenericType.GetGenericParameterConstraints().FirstOrDefault();
            Type instanceProxyType = null;
            if (instanceGenericConstraint != null)
            {
                var result = DuckType.GetOrCreateProxyType(instanceGenericConstraint, targetType);
                instanceProxyType = result.ProxyType;
                callGenericTypes.Add(instanceProxyType);
            }
            else
            {
                callGenericTypes.Add(targetType);
            }

            var callMethod = new DynamicMethod(
                     $"{onMethodBeginMethodInfo.DeclaringType.Name}.{onMethodBeginMethodInfo.Name}",
                     typeof(CallTargetState),
                     new Type[] { targetType, typeof(object[]) },
                     onMethodBeginMethodInfo.Module,
                     true);

            var ilWriter = callMethod.GetILGenerator();

            // Load the instance if is needed
            if (mustLoadInstance)
            {
                ilWriter.Emit(OpCodes.Ldarg_0);

                if (instanceGenericConstraint != null)
                {
                    WriteCreateNewProxyInstance(ilWriter, instanceProxyType, targetType);
                }
            }

            // Load arguments
            for (var i = mustLoadInstance ? 1 : 0; i < onMethodBeginParameters.Length; i++)
            {
                var targetParameterType = onMethodBeginParameters[i].ParameterType;
                Type targetParameterTypeConstraint = null;

                if (targetParameterType.IsGenericParameter)
                {
                    targetParameterType = genericArgumentsTypes[targetParameterType.GenericParameterPosition];

                    targetParameterTypeConstraint = targetParameterType.GetGenericParameterConstraints().FirstOrDefault(pType => pType != typeof(IDuckType));
                    if (targetParameterTypeConstraint is null)
                    {
                        callGenericTypes.Add(typeof(object));
                    }
                    else
                    {
                        targetParameterType = targetParameterTypeConstraint;
                        callGenericTypes.Add(targetParameterTypeConstraint);
                    }
                }

                ilWriter.Emit(OpCodes.Ldarg_1);
                WriteIntValue(ilWriter, i - (mustLoadInstance ? 1 : 0));
                ilWriter.Emit(OpCodes.Ldelem_Ref);

                if (targetParameterTypeConstraint != null)
                {
                    ilWriter.EmitCall(OpCodes.Call, ConvertTypeMethodInfo.MakeGenericMethod(targetParameterTypeConstraint), null);
                }
                else if (targetParameterType.IsValueType)
                {
                    ilWriter.Emit(OpCodes.Unbox_Any, targetParameterType);
                }
            }

            // Call method
            onMethodBeginMethodInfo = onMethodBeginMethodInfo.MakeGenericMethod(callGenericTypes.ToArray());
            ilWriter.EmitCall(OpCodes.Call, onMethodBeginMethodInfo, null);
            ilWriter.Emit(OpCodes.Ret);

            Log.Debug($"Created SlowBeginMethod Dynamic Method for '{integrationType.FullName}' integration. [Target={targetType.FullName}]");
            return callMethod;
        }

        internal static DynamicMethod CreateEndMethodDelegate(Type integrationType, Type targetType)
        {
            /*
             * OnMethodEnd signatures with 2 or 3 parameters with 1 generics:
             *      - CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state);
             *      - CallTargetReturn OnMethodEnd<TTarget>(Exception exception, CallTargetState state);
             *
             */

            Log.Debug($"Creating EndMethod Dynamic Method for '{integrationType.FullName}' integration. [Target={targetType.FullName}]");
            var onMethodEndMethodInfo = integrationType.GetMethod(EndMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (onMethodEndMethodInfo is null)
            {
                return null;
            }

            if (onMethodEndMethodInfo.ReturnType != typeof(CallTargetReturn))
            {
                throw new ArgumentException($"The return type of the method: {EndMethodName} in type: {integrationType.FullName} is not {nameof(CallTargetReturn)}");
            }

            var genericArgumentsTypes = onMethodEndMethodInfo.GetGenericArguments();
            if (genericArgumentsTypes.Length != 1)
            {
                throw new ArgumentException($"The method: {EndMethodName} in type: {integrationType.FullName} must have a single generic type for the instance type.");
            }

            var onMethodEndParameters = onMethodEndMethodInfo.GetParameters();
            if (onMethodEndParameters.Length < 2)
            {
                throw new ArgumentException($"The method: {EndMethodName} with {onMethodEndParameters.Length} parameters in type: {integrationType.FullName} has less parameters than required.");
            }
            else if (onMethodEndParameters.Length > 3)
            {
                throw new ArgumentException($"The method: {EndMethodName} with {onMethodEndParameters.Length} parameters in type: {integrationType.FullName} has more parameters than required.");
            }

            if (onMethodEndParameters[onMethodEndParameters.Length - 2].ParameterType != typeof(Exception))
            {
                throw new ArgumentException($"The Exception type parameter of the method: {EndMethodName} in type: {integrationType.FullName} is missing.");
            }

            if (onMethodEndParameters[onMethodEndParameters.Length - 1].ParameterType != typeof(CallTargetState))
            {
                throw new ArgumentException($"The CallTargetState type parameter of the method: {EndMethodName} in type: {integrationType.FullName} is missing.");
            }

            var callGenericTypes = new List<Type>();

            var mustLoadInstance = onMethodEndParameters.Length == 3;
            var instanceGenericType = genericArgumentsTypes[0];
            var instanceGenericConstraint = instanceGenericType.GetGenericParameterConstraints().FirstOrDefault();
            Type instanceProxyType = null;
            if (instanceGenericConstraint != null)
            {
                var result = DuckType.GetOrCreateProxyType(instanceGenericConstraint, targetType);
                instanceProxyType = result.ProxyType;
                callGenericTypes.Add(instanceProxyType);
            }
            else
            {
                callGenericTypes.Add(targetType);
            }

            var callMethod = new DynamicMethod(
                     $"{onMethodEndMethodInfo.DeclaringType.Name}.{onMethodEndMethodInfo.Name}",
                     typeof(CallTargetReturn),
                     new Type[] { targetType, typeof(Exception), typeof(CallTargetState) },
                     onMethodEndMethodInfo.Module,
                     true);

            var ilWriter = callMethod.GetILGenerator();

            // Load the instance if is needed
            if (mustLoadInstance)
            {
                ilWriter.Emit(OpCodes.Ldarg_0);

                if (instanceGenericConstraint != null)
                {
                    WriteCreateNewProxyInstance(ilWriter, instanceProxyType, targetType);
                }
            }

            // Load the exception
            ilWriter.Emit(OpCodes.Ldarg_1);

            // Load the state
            ilWriter.Emit(OpCodes.Ldarg_2);

            // Call Method
            onMethodEndMethodInfo = onMethodEndMethodInfo.MakeGenericMethod(callGenericTypes.ToArray());
            ilWriter.EmitCall(OpCodes.Call, onMethodEndMethodInfo, null);

            ilWriter.Emit(OpCodes.Ret);

            Log.Debug($"Created EndMethod Dynamic Method for '{integrationType.FullName}' integration. [Target={targetType.FullName}]");
            return callMethod;
        }

        internal static DynamicMethod CreateEndMethodDelegate(Type integrationType, Type targetType, Type returnType)
        {
            /*
             * OnMethodEnd signatures with 3 or 4 parameters with 1 or 2 generics:
             *      - CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state);
             *      - CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TReturn returnValue, Exception exception, CallTargetState state);
             *      - CallTargetReturn<[Type]> OnMethodEnd<TTarget>([Type] returnValue, Exception exception, CallTargetState state);
             *
             */

            Log.Debug($"Creating EndMethod Dynamic Method for '{integrationType.FullName}' integration. [Target={targetType.FullName}, ReturnType={returnType.FullName}]");
            var onMethodEndMethodInfo = integrationType.GetMethod(EndMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (onMethodEndMethodInfo is null)
            {
                return null;
            }

            if (onMethodEndMethodInfo.ReturnType.GetGenericTypeDefinition() != typeof(CallTargetReturn<>))
            {
                throw new ArgumentException($"The return type of the method: {EndMethodName} in type: {integrationType.FullName} is not {nameof(CallTargetReturn)}");
            }

            var genericArgumentsTypes = onMethodEndMethodInfo.GetGenericArguments();
            if (genericArgumentsTypes.Length < 1 || genericArgumentsTypes.Length > 2)
            {
                throw new ArgumentException($"The method: {EndMethodName} in type: {integrationType.FullName} must have the generic type for the instance type.");
            }

            var onMethodEndParameters = onMethodEndMethodInfo.GetParameters();
            if (onMethodEndParameters.Length < 3)
            {
                throw new ArgumentException($"The method: {EndMethodName} with {onMethodEndParameters.Length} parameters in type: {integrationType.FullName} has less parameters than required.");
            }
            else if (onMethodEndParameters.Length > 4)
            {
                throw new ArgumentException($"The method: {EndMethodName} with {onMethodEndParameters.Length} parameters in type: {integrationType.FullName} has more parameters than required.");
            }

            if (onMethodEndParameters[onMethodEndParameters.Length - 2].ParameterType != typeof(Exception))
            {
                throw new ArgumentException($"The Exception type parameter of the method: {EndMethodName} in type: {integrationType.FullName} is missing.");
            }

            if (onMethodEndParameters[onMethodEndParameters.Length - 1].ParameterType != typeof(CallTargetState))
            {
                throw new ArgumentException($"The CallTargetState type parameter of the method: {EndMethodName} in type: {integrationType.FullName} is missing.");
            }

            var callGenericTypes = new List<Type>();

            var mustLoadInstance = onMethodEndParameters.Length == 4;
            var instanceGenericType = genericArgumentsTypes[0];
            var instanceGenericConstraint = instanceGenericType.GetGenericParameterConstraints().FirstOrDefault();
            Type instanceProxyType = null;
            if (instanceGenericConstraint != null)
            {
                var result = DuckType.GetOrCreateProxyType(instanceGenericConstraint, targetType);
                instanceProxyType = result.ProxyType;
                callGenericTypes.Add(instanceProxyType);
            }
            else
            {
                callGenericTypes.Add(targetType);
            }

            var returnParameterIndex = onMethodEndParameters.Length == 4 ? 1 : 0;
            var isAGenericReturnValue = onMethodEndParameters[returnParameterIndex].ParameterType.IsGenericParameter;
            Type returnValueGenericType = null;
            Type returnValueGenericConstraint = null;
            Type returnValueProxyType = null;
            if (isAGenericReturnValue)
            {
                returnValueGenericType = genericArgumentsTypes[1];
                returnValueGenericConstraint = returnValueGenericType.GetGenericParameterConstraints().FirstOrDefault();
                if (returnValueGenericConstraint != null)
                {
                    var result = DuckType.GetOrCreateProxyType(returnValueGenericConstraint, returnType);
                    returnValueProxyType = result.ProxyType;
                    callGenericTypes.Add(returnValueProxyType);
                }
                else
                {
                    callGenericTypes.Add(returnType);
                }
            }
            else if (onMethodEndParameters[returnParameterIndex].ParameterType != returnType)
            {
                throw new ArgumentException($"The ReturnValue type parameter of the method: {EndMethodName} in type: {integrationType.FullName} is invalid. [{onMethodEndParameters[returnParameterIndex].ParameterType} != {returnType}]");
            }

            var callMethod = new DynamicMethod(
                     $"{onMethodEndMethodInfo.DeclaringType.Name}.{onMethodEndMethodInfo.Name}.{targetType.Name}.{returnType.Name}",
                     typeof(CallTargetReturn<>).MakeGenericType(returnType),
                     new Type[] { targetType, returnType, typeof(Exception), typeof(CallTargetState) },
                     onMethodEndMethodInfo.Module,
                     true);

            var ilWriter = callMethod.GetILGenerator();

            // Load the instance if is needed
            if (mustLoadInstance)
            {
                ilWriter.Emit(OpCodes.Ldarg_0);

                if (instanceGenericConstraint != null)
                {
                    WriteCreateNewProxyInstance(ilWriter, instanceProxyType, targetType);
                }
            }

            // Load the return value
            ilWriter.Emit(OpCodes.Ldarg_1);
            if (returnValueProxyType != null)
            {
                WriteCreateNewProxyInstance(ilWriter, returnValueProxyType, returnType);
            }

            // Load the exception
            ilWriter.Emit(OpCodes.Ldarg_2);

            // Load the state
            ilWriter.Emit(OpCodes.Ldarg_3);

            // Call Method
            onMethodEndMethodInfo = onMethodEndMethodInfo.MakeGenericMethod(callGenericTypes.ToArray());
            ilWriter.EmitCall(OpCodes.Call, onMethodEndMethodInfo, null);

            // Unwrap return value proxy
            if (returnValueProxyType != null)
            {
                var unwrapReturnValue = UnwrapReturnValueMethodInfo.MakeGenericMethod(returnValueProxyType, returnType);
                ilWriter.EmitCall(OpCodes.Call, unwrapReturnValue, null);
            }

            ilWriter.Emit(OpCodes.Ret);

            Log.Debug($"Created EndMethod Dynamic Method for '{integrationType.FullName}' integration. [Target={targetType.FullName}, ReturnType={returnType.FullName}]");
            return callMethod;
        }

        internal static DynamicMethod CreateAsyncEndMethodDelegate(Type integrationType, Type targetType, Type returnType)
        {
            /*
             * OnAsyncMethodEnd signatures with 3 or 4 parameters with 1 or 2 generics:
             *      - TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state);
             *      - TReturn OnAsyncMethodEnd<TTarget, TReturn>(TReturn returnValue, Exception exception, CallTargetState state);
             *      - [Type] OnAsyncMethodEnd<TTarget>([Type] returnValue, Exception exception, CallTargetState state);
             *
             *      In case the continuation is for a Task/ValueTask, the returnValue type will be an object and the value null.
             *      In case the continuation is for a Task<T>/ValueTask<T>, the returnValue type will be T with the instance value after the task completes.
             *
             */

            Log.Debug($"Creating AsyncEndMethod Dynamic Method for '{integrationType.FullName}' integration. [Target={targetType.FullName}, ReturnType={returnType.FullName}]");
            var onAsyncMethodEndMethodInfo = integrationType.GetMethod(EndAsyncMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (onAsyncMethodEndMethodInfo is null)
            {
                Log.Warning($"Couldn't find the method: {EndAsyncMethodName} in type: {integrationType.FullName}");
                return null;
            }

            if (!onAsyncMethodEndMethodInfo.ReturnType.IsGenericParameter && onAsyncMethodEndMethodInfo.ReturnType != returnType)
            {
                throw new ArgumentException($"The return type of the method: {EndAsyncMethodName} in type: {integrationType.FullName} is not {returnType}");
            }

            var genericArgumentsTypes = onAsyncMethodEndMethodInfo.GetGenericArguments();
            if (genericArgumentsTypes.Length < 1 || genericArgumentsTypes.Length > 2)
            {
                throw new ArgumentException($"The method: {EndAsyncMethodName} in type: {integrationType.FullName} must have the generic type for the instance type.");
            }

            var onAsyncMethodEndParameters = onAsyncMethodEndMethodInfo.GetParameters();
            if (onAsyncMethodEndParameters.Length < 3)
            {
                throw new ArgumentException($"The method: {EndAsyncMethodName} with {onAsyncMethodEndParameters.Length} parameters in type: {integrationType.FullName} has less parameters than required.");
            }
            else if (onAsyncMethodEndParameters.Length > 4)
            {
                throw new ArgumentException($"The method: {EndAsyncMethodName} with {onAsyncMethodEndParameters.Length} parameters in type: {integrationType.FullName} has more parameters than required.");
            }

            if (onAsyncMethodEndParameters[onAsyncMethodEndParameters.Length - 2].ParameterType != typeof(Exception))
            {
                throw new ArgumentException($"The Exception type parameter of the method: {EndAsyncMethodName} in type: {integrationType.FullName} is missing.");
            }

            if (onAsyncMethodEndParameters[onAsyncMethodEndParameters.Length - 1].ParameterType != typeof(CallTargetState))
            {
                throw new ArgumentException($"The CallTargetState type parameter of the method: {EndAsyncMethodName} in type: {integrationType.FullName} is missing.");
            }

            var callGenericTypes = new List<Type>();

            var mustLoadInstance = onAsyncMethodEndParameters.Length == 4;
            var instanceGenericType = genericArgumentsTypes[0];
            var instanceGenericConstraint = instanceGenericType.GetGenericParameterConstraints().FirstOrDefault();
            Type instanceProxyType = null;
            if (instanceGenericConstraint != null)
            {
                var result = DuckType.GetOrCreateProxyType(instanceGenericConstraint, targetType);
                instanceProxyType = result.ProxyType;
                callGenericTypes.Add(instanceProxyType);
            }
            else
            {
                callGenericTypes.Add(targetType);
            }

            var returnParameterIndex = onAsyncMethodEndParameters.Length == 4 ? 1 : 0;
            var isAGenericReturnValue = onAsyncMethodEndParameters[returnParameterIndex].ParameterType.IsGenericParameter;
            Type returnValueGenericType = null;
            Type returnValueGenericConstraint = null;
            Type returnValueProxyType = null;
            if (isAGenericReturnValue)
            {
                returnValueGenericType = genericArgumentsTypes[1];
                returnValueGenericConstraint = returnValueGenericType.GetGenericParameterConstraints().FirstOrDefault();
                if (returnValueGenericConstraint != null)
                {
                    var result = DuckType.GetOrCreateProxyType(returnValueGenericConstraint, returnType);
                    returnValueProxyType = result.ProxyType;
                    callGenericTypes.Add(returnValueProxyType);
                }
                else
                {
                    callGenericTypes.Add(returnType);
                }
            }
            else if (onAsyncMethodEndParameters[returnParameterIndex].ParameterType != returnType)
            {
                throw new ArgumentException($"The ReturnValue type parameter of the method: {EndAsyncMethodName} in type: {integrationType.FullName} is invalid. [{onAsyncMethodEndParameters[returnParameterIndex].ParameterType} != {returnType}]");
            }

            var callMethod = new DynamicMethod(
                     $"{onAsyncMethodEndMethodInfo.DeclaringType.Name}.{onAsyncMethodEndMethodInfo.Name}.{targetType.Name}.{returnType.Name}",
                     returnType,
                     new Type[] { targetType, returnType, typeof(Exception), typeof(CallTargetState) },
                     onAsyncMethodEndMethodInfo.Module,
                     true);

            var ilWriter = callMethod.GetILGenerator();

            // Load the instance if is needed
            if (mustLoadInstance)
            {
                ilWriter.Emit(OpCodes.Ldarg_0);

                if (instanceGenericConstraint != null)
                {
                    WriteCreateNewProxyInstance(ilWriter, instanceProxyType, targetType);
                }
            }

            // Load the return value
            ilWriter.Emit(OpCodes.Ldarg_1);
            if (returnValueProxyType != null)
            {
                WriteCreateNewProxyInstance(ilWriter, returnValueProxyType, returnType);
            }

            // Load the exception
            ilWriter.Emit(OpCodes.Ldarg_2);

            // Load the state
            ilWriter.Emit(OpCodes.Ldarg_3);

            // Call Method
            onAsyncMethodEndMethodInfo = onAsyncMethodEndMethodInfo.MakeGenericMethod(callGenericTypes.ToArray());
            ilWriter.EmitCall(OpCodes.Call, onAsyncMethodEndMethodInfo, null);

            // Unwrap return value proxy
            if (returnValueProxyType != null)
            {
                var unwrapReturnValue = UnwrapReturnValueMethodInfo.MakeGenericMethod(returnValueProxyType, returnType);
                ilWriter.EmitCall(OpCodes.Call, unwrapReturnValue, null);
            }

            ilWriter.Emit(OpCodes.Ret);

            Log.Debug($"Created AsyncEndMethod Dynamic Method for '{integrationType.FullName}' integration. [Target={targetType.FullName}, ReturnType={returnType.FullName}]");
            return callMethod;
        }

        private static void WriteCreateNewProxyInstance(ILGenerator ilWriter, Type proxyType, Type targetType)
        {
            var proxyTypeCtor = proxyType.GetConstructors()[0];

            if (targetType.IsValueType && !proxyTypeCtor.GetParameters()[0].ParameterType.IsValueType)
            {
                ilWriter.Emit(OpCodes.Box, targetType);
            }

            ilWriter.Emit(OpCodes.Newobj, proxyTypeCtor);
        }

        private static TTo UnwrapReturnValue<TFrom, TTo>(TFrom returnValue)
            where TFrom : IDuckType =>
			(TTo)returnValue.Instance;

		private static void WriteIntValue(ILGenerator il, int value)
        {
            switch (value)
            {
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
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    il.Emit(OpCodes.Ldc_I4, value);
                    break;
            }
        }

        private static void WriteLoadArgument(ILGenerator il, int index, bool isStatic)
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

        private static T ConvertType<T>(object value)
        {
            var conversionType = typeof(T);
            if (value is null || conversionType == typeof(object))
            {
                return (T)value;
            }

            var valueType = value.GetType();
            if (valueType == conversionType || conversionType.IsAssignableFrom(valueType))
            {
                return (T)value;
            }

            // Finally we try to duck type
            return DuckType.Create<T>(value);
        }
    }
}
