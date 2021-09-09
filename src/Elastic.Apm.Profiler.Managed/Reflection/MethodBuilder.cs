// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="MethodBuilder.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Elastic.Apm.Logging;
using Elastic.Apm.Profiler.Managed.Core;

namespace Elastic.Apm.Profiler.Managed.Reflection
{
    internal class MethodBuilder<TDelegate>
        where TDelegate : Delegate
    {
        /// <summary>
        /// Global dictionary for caching reflected delegates
        /// </summary>
        private static readonly ConcurrentDictionary<Key, TDelegate> Cache = new ConcurrentDictionary<Key, TDelegate>(new KeyComparer());
		private static readonly IApmLogger Log = Agent.Instance.Logger.Scoped("typeof(MethodBuilder<TDelegate>)");

        /// <summary>
        /// Feature flag used primarily for forcing testing of the token lookup strategy.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        private static readonly bool ForceMdTokenLookup;

        /// <summary>
        /// Feature flag used primarily for forcing testing of the fallback lookup strategy.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        private static readonly bool ForceFallbackLookup;

        private readonly Module _resolutionModule;
        private readonly int _mdToken;
        private readonly int _originalOpCodeValue;
        private readonly OpCodeValue _opCode;
        private readonly string _methodName;
        private readonly Guid? _moduleVersionId;

        private Type _returnType;
        private MethodBase _methodBase;
        private Type _concreteType;
        private string _concreteTypeName;
        private Type[] _parameters = Array.Empty<Type>();
        private Type[] _explicitParameterTypes = null;
        private string[] _namespaceAndNameFilter = null;
        private Type[] _declaringTypeGenerics;
        private Type[] _methodGenerics;
        private bool _forceMethodDefResolve;

        static MethodBuilder()
        {
			// TODO: implement environment variables for these?
            // ForceMdTokenLookup = bool.TryParse(EnvironmentHelpers.GetEnvironmentVariable(ConfigurationKeys.Debug.ForceMdTokenLookup), out bool result)
            //         ? result
            //         : false;
            // ForceFallbackLookup = bool.TryParse(EnvironmentHelpers.GetEnvironmentVariable(ConfigurationKeys.Debug.ForceFallbackLookup), out result)
            //         ? result && !ForceMdTokenLookup
            //         : false;
        }

        private MethodBuilder(Guid moduleVersionId, int mdToken, int opCode, string methodName)
            : this(ModuleLookup.Get(moduleVersionId), mdToken, opCode, methodName) =>
			// Save the Guid for logging purposes
			_moduleVersionId = moduleVersionId;

		private MethodBuilder(Module resolutionModule, int mdToken, int opCode, string methodName)
        {
            _resolutionModule = resolutionModule;
            _mdToken = mdToken;
            _opCode = (OpCodeValue)opCode;
            _originalOpCodeValue = opCode;
            _methodName = methodName;
            _forceMethodDefResolve = false;
        }

        public static MethodBuilder<TDelegate> Start(Guid moduleVersionId, int mdToken, int opCode, string methodName) =>
			new MethodBuilder<TDelegate>(moduleVersionId, mdToken, opCode, methodName);

		public static MethodBuilder<TDelegate> Start(Module module, int mdToken, int opCode, string methodName) =>
			new MethodBuilder<TDelegate>(
				module,
				mdToken,
				opCode,
				methodName);

		public static MethodBuilder<TDelegate> Start(long moduleVersionPtr, int mdToken, int opCode, string methodName) =>
			new MethodBuilder<TDelegate>(
				Marshal.PtrToStructure<Guid>(new IntPtr(moduleVersionPtr)),
				mdToken,
				opCode,
				methodName);

		public MethodBuilder<TDelegate> WithConcreteType(Type type)
        {
            _concreteType = type;
            _concreteTypeName = type?.FullName;
            return this;
        }

        public MethodBuilder<TDelegate> WithNamespaceAndNameFilters(params string[] namespaceNameFilters)
        {
            _namespaceAndNameFilter = namespaceNameFilters;
            return this;
        }

        public MethodBuilder<TDelegate> WithParameters(params Type[] parameters)
        {
			_parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            return this;
        }

        public MethodBuilder<TDelegate> WithParameters(params object[] parameters)
        {
            if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			return WithParameters(Interception.ParamsToTypes(parameters));
        }

        public MethodBuilder<TDelegate> WithParameters<TParam>(TParam param1)
        {
            var types = new[] { param1?.GetType() };

            return WithParameters(types);
        }

        public MethodBuilder<TDelegate> WithParameters<TParam1, TParam2>(TParam1 param1, TParam2 param2)
        {
            var types = new[] { param1?.GetType(), param2?.GetType() };

            return WithParameters(types);
        }

        public MethodBuilder<TDelegate> WithParameters<TParam1, TParam2, TParam3>(TParam1 param1, TParam2 param2, TParam3 param3)
        {
            var types = new[] { param1?.GetType(), param2?.GetType(), param3?.GetType() };

            return WithParameters(types);
        }

        public MethodBuilder<TDelegate> WithParameters<TParam1, TParam2, TParam3, TParam4>(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4)
        {
            var types = new[] { param1?.GetType(), param2?.GetType(), param3?.GetType(), param4?.GetType() };

            return WithParameters(types);
        }

        public MethodBuilder<TDelegate> WithExplicitParameterTypes(params Type[] types)
        {
            _explicitParameterTypes = types;
            return this;
        }

        public MethodBuilder<TDelegate> WithMethodGenerics(params Type[] generics)
        {
            _methodGenerics = generics;
            return this;
        }

        public MethodBuilder<TDelegate> WithDeclaringTypeGenerics(params Type[] generics)
        {
            _declaringTypeGenerics = generics;
            return this;
        }

        public MethodBuilder<TDelegate> ForceMethodDefinitionResolution()
        {
            _forceMethodDefResolve = true;
            return this;
        }

        public MethodBuilder<TDelegate> WithReturnType(Type returnType)
        {
            _returnType = returnType;
            return this;
        }

        public TDelegate Build()
        {
            var cacheKey = new Key(
                this,
                callingModule: _resolutionModule);

            return Cache.GetOrAdd(cacheKey, key =>
            {
                // Validate requirements at the last possible moment
                // Don't do more than needed before checking the cache
                key.Builder.ValidateRequirements();
                return key.Builder.EmitDelegate();
            });
        }

        private TDelegate EmitDelegate()
        {
            var requiresBestEffortMatching = false;

            if (_resolutionModule != null)
            {
                try
                {
                    // Don't resolve until we build, as it may be an unnecessary lookup because of the cache
                    // We also may need the generics which were specified
                    if (_forceMethodDefResolve || (_declaringTypeGenerics == null && _methodGenerics == null))
                    {
                        _methodBase =
                            _resolutionModule.ResolveMethod(metadataToken: _mdToken);
                    }
                    else
                    {
                        _methodBase =
                            _resolutionModule.ResolveMethod(
                                metadataToken: _mdToken,
                                genericTypeArguments: _declaringTypeGenerics,
                                genericMethodArguments: _methodGenerics);
                    }
                }
                catch (Exception ex)
                {
					Log.Error()?.LogException(ex, "Unable to resolve method {ConcreteTypeName}.{MethodName} by metadata token: {mdToken}",
						_concreteType,
						_methodName,
						_mdToken);
                    requiresBestEffortMatching = true;
                }
            }
            else
            {
                Log.Warning()?.Log("Unable to resolve module version id {ModuleVersionId}. Using method builder fallback.", _moduleVersionId);
            }

            MethodInfo methodInfo = null;

            if (!requiresBestEffortMatching && _methodBase is MethodInfo info)
            {
                if (info.IsGenericMethodDefinition)
                {
                    info = MakeGenericMethod(info);
                }

                methodInfo = VerifyMethodFromToken(info);
            }

            if (methodInfo == null && ForceMdTokenLookup)
            {
                throw new Exception($"Unable to resolve method {_concreteTypeName}.{_methodName} by metadata token: {_mdToken}. Exiting because {nameof(ForceMdTokenLookup)}() is true.");
            }
            else if (methodInfo == null || ForceFallbackLookup)
            {
                // mdToken didn't work out, fallback
                methodInfo = TryFindMethod();
            }

            var delegateType = typeof(TDelegate);
            var delegateGenericArgs = delegateType.GenericTypeArguments;

            Type[] delegateParameterTypes;
            Type returnType;

            if (delegateType.Name.StartsWith("Func`"))
            {
                // last generic type argument is the return type
                var parameterCount = delegateGenericArgs.Length - 1;
                delegateParameterTypes = new Type[parameterCount];
                Array.Copy(delegateGenericArgs, delegateParameterTypes, parameterCount);

                returnType = delegateGenericArgs[parameterCount];
            }
            else if (delegateType.Name.StartsWith("Action`"))
            {
                delegateParameterTypes = delegateGenericArgs;
                returnType = typeof(void);
            }
            else
            {
                throw new Exception($"Only Func<> or Action<> are supported in {nameof(MethodBuilder)}.");
            }

            if (methodInfo.IsGenericMethodDefinition)
            {
                methodInfo = MakeGenericMethod(methodInfo);
            }

            Type[] effectiveParameterTypes;

            var reflectedParameterTypes =
                methodInfo.GetParameters().Select(p => p.ParameterType);

            if (methodInfo.IsStatic)
            {
                effectiveParameterTypes = reflectedParameterTypes.ToArray();
            }
            else
            {
                // for instance methods, insert object's type as first element in array
                effectiveParameterTypes = new[] { _concreteType }
                                         .Concat(reflectedParameterTypes)
                                         .ToArray();
            }

            var dynamicMethod = new DynamicMethod(methodInfo.Name, returnType, delegateParameterTypes, ObjectExtensions.Module, skipVisibility: true);
            var il = dynamicMethod.GetILGenerator();

            // load each argument and cast or unbox as necessary
            for (ushort argumentIndex = 0; argumentIndex < delegateParameterTypes.Length; argumentIndex++)
            {
                var delegateParameterType = delegateParameterTypes[argumentIndex];
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

            if (_opCode == OpCodeValue.Call || methodInfo.IsStatic)
            {
                // non-virtual call (e.g. static method, or method override calling overriden implementation)
                il.Emit(OpCodes.Call, methodInfo);
            }
            else if (_opCode == OpCodeValue.Callvirt)
            {
                // Note: C# compiler uses CALLVIRT for non-virtual
                // instance methods to get the cheap null check
                il.Emit(OpCodes.Callvirt, methodInfo);
            }
            else
            {
                throw new NotSupportedException($"OpCode {_originalOpCodeValue} not supported when calling a method.");
            }

            if (methodInfo.ReturnType.IsValueType && !returnType.IsValueType)
            {
                il.Emit(OpCodes.Box, methodInfo.ReturnType);
            }
            else if (methodInfo.ReturnType.IsValueType && returnType.IsValueType && methodInfo.ReturnType != returnType)
            {
                throw new ArgumentException($"Cannot convert the target method's return type {methodInfo.ReturnType.FullName} (value type) to the delegate method's return type {returnType.FullName} (value type)");
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
            return (TDelegate)dynamicMethod.CreateDelegate(typeof(TDelegate));
        }

        private MethodInfo MakeGenericMethod(MethodInfo methodInfo)
        {
            if (_methodGenerics == null || _methodGenerics.Length == 0)
            {
                throw new ArgumentException($"Must specify {nameof(_methodGenerics)} for a generic method.");
            }

            return methodInfo.MakeGenericMethod(_methodGenerics);
        }

        private MethodInfo VerifyMethodFromToken(MethodInfo methodInfo)
        {
            // Verify baselines to ensure this isn't the wrong method somehow
            var detailMessage = $"Unexpected method: {_concreteTypeName}.{_methodName} received for mdToken: {_mdToken} in module: {_resolutionModule?.FullyQualifiedName ?? "NULL"}, {_resolutionModule?.ModuleVersionId ?? _moduleVersionId}";

            if (!string.Equals(_methodName, methodInfo.Name))
            {
                Log.Warning()?.Log($"Method name mismatch: {detailMessage}");
                return null;
            }

            if (!GenericsAreViable(methodInfo))
            {
                Log.Warning()?.Log($"Generics not viable: {detailMessage}");
                return null;
            }

            if (!ParametersAreViable(methodInfo))
            {
                Log.Warning()?.Log($"Parameters not viable: {detailMessage}");
                return null;
            }

			if (!methodInfo.IsStatic && !methodInfo.ReflectedType.IsAssignableFrom(_concreteType))
			{
				Log.Warning()?.Log($"{_concreteType} cannot be assigned to the type containing the MethodInfo representing the instance method: {detailMessage}");
				return null;
			}

            return methodInfo;
        }

        private void ValidateRequirements()
        {
            if (_concreteType == null)
            {
                throw new ArgumentException($"{nameof(_concreteType)} must be specified.");
            }

            if (string.IsNullOrWhiteSpace(_methodName))
            {
                throw new ArgumentException($"There must be a {nameof(_methodName)} specified to ensure fallback {nameof(TryFindMethod)} is viable.");
            }

            if (_namespaceAndNameFilter != null && _namespaceAndNameFilter.Length != _parameters.Length + 1)
            {
                throw new ArgumentException($"The length of {nameof(_namespaceAndNameFilter)} must match the length of {nameof(_parameters)} + 1 for the return type.");
            }

            if (_explicitParameterTypes != null)
            {
                if (_explicitParameterTypes.Length != _parameters.Length)
                {
                    throw new ArgumentException($"The {nameof(_explicitParameterTypes)} must match the {_parameters} count.");
                }

                for (var i = 0; i < _explicitParameterTypes.Length; i++)
                {
                    var explicitType = _explicitParameterTypes[i];
                    var parameterType = _parameters[i];

                    if (parameterType == null)
                    {
                        // Nothing to check
                        continue;
                    }

                    if (!explicitType.IsAssignableFrom(parameterType))
                    {
                        throw new ArgumentException($"Parameter Index {i}: Explicit type {explicitType.FullName} is not assignable from {parameterType}");
                    }
                }
            }
        }

        private MethodInfo TryFindMethod()
        {
            var logDetail = $"mdToken {_mdToken} on {_concreteTypeName}.{_methodName} in {_resolutionModule?.FullyQualifiedName ?? "NULL"}, {_resolutionModule?.ModuleVersionId ?? _moduleVersionId}";
            Log.Warning()?.Log($"Using fallback method matching ({logDetail})");

            var methods =
                _concreteType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            // A legacy fallback attempt to match on the concrete type
            methods =
                methods
                   .Where(mi => mi.Name == _methodName && (_returnType == null || mi.ReturnType == _returnType))
                   .ToArray();

            var matchesOnNameAndReturn = methods.Length;

            if (_namespaceAndNameFilter != null)
            {
                methods = methods.Where(m =>
                {
                    var parameters = m.GetParameters();

                    if ((parameters.Length + 1) != _namespaceAndNameFilter.Length)
                    {
                        return false;
                    }

                    var typesToCheck = new Type[] { m.ReturnType }.Concat(m.GetParameters().Select(p => p.ParameterType)).ToArray();
                    for (var i = 0; i < typesToCheck.Length; i++)
                    {
                        if (_namespaceAndNameFilter[i] == ClrTypeNames.Ignore)
                        {
                            // Allow for not specifying
                            continue;
                        }

                        if ($"{typesToCheck[i].Namespace}.{typesToCheck[i].Name}" != _namespaceAndNameFilter[i])
                        {
                            return false;
                        }
                    }

                    return true;
                }).ToArray();
            }

            if (methods.Length == 1)
            {
                Log.Info()?.Log($"Resolved by name and namespaceName filters ({logDetail})");
                return methods[0];
            }

            methods =
                methods
                   .Where(ParametersAreViable)
                   .ToArray();

            if (methods.Length == 1)
            {
                Log.Info()?.Log($"Resolved by viable parameters ({logDetail})");
                return methods[0];
            }

            methods =
                methods
                   .Where(GenericsAreViable)
                   .ToArray();

            if (methods.Length == 1)
            {
                Log.Info()?.Log($"Resolved by viable generics ({logDetail})");
                return methods[0];
            }

            // Attempt to trim down further
            methods = methods.Where(ParametersAreExact).ToArray();

            if (methods.Length > 1)
            {
                throw new ArgumentException($"Unable to safely resolve method, found {methods.Length} matches ({logDetail})");
            }

            var methodInfo = methods.SingleOrDefault();

            if (methodInfo == null)
            {
                throw new ArgumentException($"Unable to resolve method, started with {matchesOnNameAndReturn} by name match ({logDetail})");
            }

            return methodInfo;
        }

        private bool ParametersAreViable(MethodInfo mi)
        {
            var parameters = mi.GetParameters();

            if (parameters.Length != _parameters.Length)
            {
                // expected parameters don't match actual count
                return false;
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                var candidateParameter = parameters[i];

                var parameterType = candidateParameter.ParameterType;

                var expectedParameterType = GetExpectedParameterTypeByIndex(i);

                if (expectedParameterType == null)
                {
                    // Skip the rest of this check, as we can't know the type
                    continue;
                }

                if (parameterType.IsGenericParameter)
                {
                    // This requires different evaluation
                    if (MeetsGenericArgumentRequirements(parameterType, expectedParameterType))
                    {
                        // Good to go
                        continue;
                    }

                    // We didn't meet this generic argument's requirements
                    return false;
                }

                if (!parameterType.IsAssignableFrom(expectedParameterType))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ParametersAreExact(MethodInfo mi)
        {
            // We can already assume that the counts match by this point
            var parameters = mi.GetParameters();

            for (var i = 0; i < parameters.Length; i++)
            {
                var candidateParameter = parameters[i];

                var parameterType = candidateParameter.ParameterType;

                var actualArgumentType = GetExpectedParameterTypeByIndex(i);

                if (actualArgumentType == null)
                {
                    // Skip the rest of this check, as we can't know the type
                    continue;
                }

                if (parameterType != actualArgumentType)
                {
                    return false;
                }
            }

            return true;
        }

        private Type GetExpectedParameterTypeByIndex(int i) =>
			_explicitParameterTypes != null
				? _explicitParameterTypes[i]
				: _parameters[i];

		private bool GenericsAreViable(MethodInfo mi)
        {
            // Non-Generic Method - { IsGenericMethod: false, ContainsGenericParameters: false, IsGenericMethodDefinition: false }
            // Generic Method Definition - { IsGenericMethod: true, ContainsGenericParameters: true, IsGenericMethodDefinition: true }
            // Open Constructed Method - { IsGenericMethod: true, ContainsGenericParameters: true, IsGenericMethodDefinition: false }
            // Closed Constructed Method - { IsGenericMethod: true, ContainsGenericParameters: false, IsGenericMethodDefinition: false }

            if (_methodGenerics == null)
            {
                // We expect no generic arguments for this method
                return mi.ContainsGenericParameters == false;
            }

            if (!mi.IsGenericMethod)
            {
                // There is really nothing to compare here
                // Make sure we aren't looking for generics where there aren't
                return _methodGenerics?.Length == 0;
            }

            var genericArgs = mi.GetGenericArguments();

            if (genericArgs.Length != _methodGenerics.Length)
            {
                // Count of arguments mismatch
                return false;
            }

            foreach (var actualGenericArg in genericArgs)
            {
                if (actualGenericArg.IsGenericParameter)
                {
                    var expectedGenericArg = _methodGenerics[actualGenericArg.GenericParameterPosition];

                    if (!MeetsGenericArgumentRequirements(actualGenericArg, expectedGenericArg))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool MeetsGenericArgumentRequirements(Type actualGenericArg, Type expectedArg)
        {
            var constraints = actualGenericArg.GetGenericParameterConstraints();

            if (constraints.Any(constraint => !constraint.IsAssignableFrom(expectedArg)))
            {
                // We have failed to meet a constraint
                return false;
            }

            return true;
        }

        private struct Key
        {
            public readonly int CallingModuleMetadataToken;
            public readonly MethodBuilder<TDelegate> Builder;

            public Key(
                MethodBuilder<TDelegate> builder,
                Module callingModule)
            {
                Builder = builder;
                CallingModuleMetadataToken = callingModule.MetadataToken;
            }

            public Type[] ExplicitParams => Builder._explicitParameterTypes ?? Builder._parameters;
        }

        private class KeyComparer : IEqualityComparer<Key>
        {
            public bool Equals(Key x, Key y)
            {
                if (x.CallingModuleMetadataToken != y.CallingModuleMetadataToken)
                {
                    return false;
                }

                var builder1 = x.Builder;
                var builder2 = y.Builder;

                if (builder1._mdToken != builder2._mdToken)
                {
                    return false;
                }

                if (builder1._opCode != builder2._opCode)
                {
                    return false;
                }

                if (builder1._concreteType != builder2._concreteType)
                {
                    return false;
                }

                if (!ArrayEquals(x.ExplicitParams, y.ExplicitParams))
                {
                    return false;
                }

                if (!ArrayEquals(builder1._methodGenerics, builder2._methodGenerics))
                {
                    return false;
                }

                if (!ArrayEquals(builder1._declaringTypeGenerics, builder2._declaringTypeGenerics))
                {
                    return false;
                }

                return true;
            }

            public int GetHashCode(Key obj)
            {
                unchecked
                {
                    var builder = obj.Builder;

                    var hash = 17;
                    hash = (hash * 23) + obj.CallingModuleMetadataToken.GetHashCode();
                    hash = (hash * 23) + builder._mdToken.GetHashCode();
                    hash = (hash * 23) + ((short)builder._opCode).GetHashCode();
                    hash = (hash * 23) + builder._concreteType.GetHashCode();
                    hash = (hash * 23) + GetHashCode(builder._methodGenerics);
                    hash = (hash * 23) + GetHashCode(obj.ExplicitParams);
                    hash = (hash * 23) + GetHashCode(builder._declaringTypeGenerics);
                    return hash;
                }
            }

            private static int GetHashCode(Type[] array)
            {
                if (array == null)
                {
                    return 0;
                }

                var value = array.Length;

                for (var i = 0; i < array.Length; i++)
                {
                    value = unchecked((value * 31) + array[i]?.GetHashCode() ?? 0);
                }

                return value;
            }

            private static bool ArrayEquals(Type[] array1, Type[] array2)
            {
                if (array1 == null)
                {
                    return array2 == null;
                }

                if (array2 == null)
                {
                    return false;
                }

                if (array1.Length != array2.Length)
                {
                    return false;
                }

                for (var i = 0; i < array1.Length; i++)
                {
                    if (array1[i] != array2[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
