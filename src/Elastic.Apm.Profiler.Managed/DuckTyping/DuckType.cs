// <copyright file="DuckType.cs" company="Datadog">
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
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Elastic.Apm.Profiler.Managed.DuckTyping
{
    /// <summary>
    /// Create struct proxy instance delegate
    /// </summary>
    /// <typeparam name="T">Type of struct</typeparam>
    /// <param name="instance">Object instance</param>
    /// <returns>Proxy instance</returns>
    public delegate T CreateProxyInstance<T>(object instance);

    /// <summary>
    /// Duck Type
    /// </summary>
    public static partial class DuckType
    {
        /// <summary>
        /// Create duck type proxy using a base type
        /// </summary>
        /// <param name="instance">Instance object</param>
        /// <typeparam name="T">Duck type</typeparam>
        /// <returns>Duck type proxy</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Create<T>(object instance) => CreateCache<T>.Create(instance);

		/// <summary>
        /// Create duck type proxy using a base type
        /// </summary>
        /// <param name="proxyType">Duck type</param>
        /// <param name="instance">Instance object</param>
        /// <returns>Duck Type proxy</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDuckType Create(Type proxyType, object instance)
        {
            // Validate arguments
            EnsureArguments(proxyType, instance);

            // Create Type
            var result = GetOrCreateProxyType(proxyType, instance.GetType());

            // Create instance
            return result.CreateInstance(instance);
        }

        /// <summary>
        /// Gets if a proxy can be created
        /// </summary>
        /// <param name="instance">Instance object</param>
        /// <typeparam name="T">Duck type</typeparam>
        /// <returns>true if the proxy can be created; otherwise, false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanCreate<T>(object instance) => CreateCache<T>.CanCreate(instance);

		/// <summary>
        /// Gets if a proxy can be created
        /// </summary>
        /// <param name="proxyType">Duck type</param>
        /// <param name="instance">Instance object</param>
        /// <returns>true if the proxy can be created; otherwise, false</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanCreate(Type proxyType, object instance)
        {
            // Validate arguments
            EnsureArguments(proxyType, instance);

            // Create Type
            var result = GetOrCreateProxyType(proxyType, instance.GetType());

            // Create instance
            return result.CanCreate();
        }

        /// <summary>
        /// Gets or create a new proxy type for ducktyping
        /// </summary>
        /// <param name="proxyType">ProxyType interface</param>
        /// <param name="targetType">Target type</param>
        /// <returns>CreateTypeResult instance</returns>
        public static CreateTypeResult GetOrCreateProxyType(Type proxyType, Type targetType) =>
			DuckTypeCache.GetOrAdd(
					new TypesTuple(proxyType, targetType),
					key => new Lazy<CreateTypeResult>(() => CreateProxyType(key.ProxyDefinitionType, key.TargetType)))
				.Value;

		private static CreateTypeResult CreateProxyType(Type proxyDefinitionType, Type targetType)
        {
            lock (_locker)
            {
                try
                {
                    // Define parent type, interface types
                    Type parentType;
                    TypeAttributes typeAttributes;
                    Type[] interfaceTypes;
                    if (proxyDefinitionType.IsInterface || proxyDefinitionType.IsValueType)
                    {
                        // If the proxy type definition is an interface we create an struct proxy
                        // If the proxy type definition is an struct then we use that struct to copy the values from the target type
                        parentType = typeof(ValueType);
                        typeAttributes = TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.Serializable;
                        if (proxyDefinitionType.IsInterface)
							interfaceTypes = new[] { proxyDefinitionType, typeof(IDuckType) };
						else
							interfaceTypes = new[] { typeof(IDuckType) };
					}
                    else
                    {
                        // If the proxy type definition is a class then we create a class proxy
                        parentType = proxyDefinitionType;
                        typeAttributes = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.Sealed;
                        interfaceTypes = new[] { typeof(IDuckType) };
                    }

                    // Gets the module builder
                    var moduleBuilder = GetModuleBuilder(targetType, (targetType.IsPublic || targetType.IsNestedPublic) && (proxyDefinitionType.IsPublic || proxyDefinitionType.IsNestedPublic));

                    // Ensure visibility
                    EnsureTypeVisibility(moduleBuilder, targetType);
                    EnsureTypeVisibility(moduleBuilder, proxyDefinitionType);

                    var assembly = string.Empty;
                    if (targetType.Assembly != null)
                    {
                        // Include target assembly name and public token.
                        var asmName = targetType.Assembly.GetName();
                        assembly = asmName.Name;
                        var pbToken = asmName.GetPublicKeyToken();
                        assembly += "__" + BitConverter.ToString(pbToken).Replace("-", string.Empty);
                        assembly = assembly.Replace(".", "_").Replace("+", "__");
                    }

                    // Create a valid type name that can be used as a member of a class. (BenchmarkDotNet fails if is an invalid name)
                    var proxyTypeName = $"{assembly}.{targetType.FullName.Replace(".", "_").Replace("+", "__")}.{proxyDefinitionType.FullName.Replace(".", "_").Replace("+", "__")}_{++_typeCount}";

                    // Create Type
                    var proxyTypeBuilder = moduleBuilder.DefineType(
                        proxyTypeName,
                        typeAttributes,
                        parentType,
                        interfaceTypes);

                    // Create IDuckType and IDuckTypeSetter implementations
                    var instanceField = CreateIDuckTypeImplementation(proxyTypeBuilder, targetType);

                    // Define .ctor to store the instance field
                    var ctorBuilder = proxyTypeBuilder.DefineConstructor(
                        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                        CallingConventions.Standard,
                        new[] { instanceField.FieldType });
                    var ctorIL = ctorBuilder.GetILGenerator();
                    ctorIL.Emit(OpCodes.Ldarg_0);
                    ctorIL.Emit(OpCodes.Ldarg_1);
                    ctorIL.Emit(OpCodes.Stfld, instanceField);
                    ctorIL.Emit(OpCodes.Ret);

                    if (proxyDefinitionType.IsValueType)
                    {
                        // Create Fields and Properties from the struct information
                        CreatePropertiesFromStruct(proxyTypeBuilder, proxyDefinitionType, targetType, instanceField);

                        // Create Type
                        var proxyType = proxyTypeBuilder.CreateTypeInfo().AsType();
                        return new CreateTypeResult(proxyDefinitionType, proxyType, targetType, CreateStructCopyMethod(moduleBuilder, proxyDefinitionType, proxyType, targetType), null);
                    }
                    else
                    {
                        // Create Fields and Properties
                        CreateProperties(proxyTypeBuilder, proxyDefinitionType, targetType, instanceField);

                        // Create Methods
                        CreateMethods(proxyTypeBuilder, proxyDefinitionType, targetType, instanceField);

                        // Create Type
                        var proxyType = proxyTypeBuilder.CreateTypeInfo().AsType();
                        return new CreateTypeResult(proxyDefinitionType, proxyType, targetType, GetCreateProxyInstanceDelegate(moduleBuilder, proxyDefinitionType, proxyType, targetType), null);
                    }
                }
                catch (Exception ex)
                {
                    return new CreateTypeResult(proxyDefinitionType, null, targetType, null, ExceptionDispatchInfo.Capture(ex));
                }
            }
        }

        private static FieldInfo CreateIDuckTypeImplementation(TypeBuilder proxyTypeBuilder, Type targetType)
        {
            var instanceType = targetType;
            if (!UseDirectAccessTo(proxyTypeBuilder, targetType))
            {
                instanceType = typeof(object);
            }

            var instanceField = proxyTypeBuilder.DefineField("_currentInstance", instanceType, FieldAttributes.Private | FieldAttributes.InitOnly);

            var propInstance = proxyTypeBuilder.DefineProperty("Instance", PropertyAttributes.None, typeof(object), null);
            var getPropInstance = proxyTypeBuilder.DefineMethod(
                "get_Instance",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(object),
                Type.EmptyTypes);
            var il = getPropInstance.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, instanceField);
            if (instanceType.IsValueType)
            {
                il.Emit(OpCodes.Box, instanceType);
            }

            il.Emit(OpCodes.Ret);
            propInstance.SetGetMethod(getPropInstance);

            var propType = proxyTypeBuilder.DefineProperty("Type", PropertyAttributes.None, typeof(Type), null);
            var getPropType = proxyTypeBuilder.DefineMethod(
                "get_Type",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(Type),
                Type.EmptyTypes);
            il = getPropType.GetILGenerator();
            il.Emit(OpCodes.Ldtoken, targetType);
            il.EmitCall(OpCodes.Call, GetTypeFromHandleMethodInfo, null);
            il.Emit(OpCodes.Ret);
            propType.SetGetMethod(getPropType);

            return instanceField;
        }

        private static List<PropertyInfo> GetProperties(Type proxyDefinitionType)
        {
            var selectedProperties = new List<PropertyInfo>(proxyDefinitionType.IsInterface ? proxyDefinitionType.GetProperties() : GetBaseProperties(proxyDefinitionType));
            var implementedInterfaces = proxyDefinitionType.GetInterfaces();
            foreach (var imInterface in implementedInterfaces)
            {
                if (imInterface == typeof(IDuckType))
                {
                    continue;
                }

                var newProps = imInterface.GetProperties().Where(p => selectedProperties.All(i => i.Name != p.Name));
                selectedProperties.AddRange(newProps);
            }

            return selectedProperties;

            static IEnumerable<PropertyInfo> GetBaseProperties(Type baseType)
            {
                foreach (var prop in baseType.GetProperties())
                {
                    if (prop.CanRead && (prop.GetMethod.IsAbstract || prop.GetMethod.IsVirtual))
						yield return prop;
					else if (prop.CanWrite && (prop.SetMethod.IsAbstract || prop.SetMethod.IsVirtual))
						yield return prop;
				}
            }
        }

        private static void CreateProperties(TypeBuilder proxyTypeBuilder, Type proxyDefinitionType, Type targetType, FieldInfo instanceField)
        {
            // Gets all properties to be implemented
            var proxyTypeProperties = GetProperties(proxyDefinitionType);

            foreach (var proxyProperty in proxyTypeProperties)
            {
                // Ignore the properties marked with `DuckIgnore` attribute
                if (proxyProperty.GetCustomAttribute<DuckIgnoreAttribute>(true) is not null)
                {
                    continue;
                }

                PropertyBuilder propertyBuilder = null;

                // If the property is abstract or interface we make sure that we have the property defined in the new class
                if ((proxyProperty.CanRead && proxyProperty.GetMethod.IsAbstract) || (proxyProperty.CanWrite && proxyProperty.SetMethod.IsAbstract))
                {
                    propertyBuilder = proxyTypeBuilder.DefineProperty(proxyProperty.Name, PropertyAttributes.None, proxyProperty.PropertyType, null);
                }

                var duckAttribute = proxyProperty.GetCustomAttribute<DuckAttribute>(true) ?? new DuckAttribute();
                duckAttribute.Name ??= proxyProperty.Name;

                switch (duckAttribute.Kind)
                {
                    case DuckKind.Property:
                        PropertyInfo targetProperty = null;
                        try
                        {
                            targetProperty = targetType.GetProperty(duckAttribute.Name, duckAttribute.BindingFlags);
                        }
                        catch
                        {
                            // This will run only when multiple indexers are defined in a class, that way we can end up with multiple properties with the same name.
                            // In this case we make sure we select the indexer we want
                            targetProperty = targetType.GetProperty(duckAttribute.Name, proxyProperty.PropertyType, proxyProperty.GetIndexParameters().Select(i => i.ParameterType).ToArray());
                        }

                        if (targetProperty is null)
                        {
                            break;
                        }

                        propertyBuilder ??= proxyTypeBuilder.DefineProperty(proxyProperty.Name, PropertyAttributes.None, proxyProperty.PropertyType, null);

                        if (proxyProperty.CanRead)
                        {
                            // Check if the target property can be read
                            if (!targetProperty.CanRead)
                            {
                                DuckTypePropertyCantBeReadException.Throw(targetProperty);
                            }

                            propertyBuilder.SetGetMethod(GetPropertyGetMethod(proxyTypeBuilder, targetType, proxyProperty, targetProperty, instanceField));
                        }

                        if (proxyProperty.CanWrite)
                        {
                            // Check if the target property can be written
                            if (!targetProperty.CanWrite)
                            {
                                DuckTypePropertyCantBeWrittenException.Throw(targetProperty);
                            }

                            // Check if the target property declaring type is an struct (structs modification is not supported)
                            if (targetProperty.DeclaringType.IsValueType)
                            {
                                DuckTypeStructMembersCannotBeChangedException.Throw(targetProperty.DeclaringType);
                            }

                            propertyBuilder.SetSetMethod(GetPropertySetMethod(proxyTypeBuilder, targetType, proxyProperty, targetProperty, instanceField));
                        }

                        break;

                    case DuckKind.Field:
                        var targetField = targetType.GetField(duckAttribute.Name, duckAttribute.BindingFlags);
                        if (targetField is null)
                        {
                            break;
                        }

                        propertyBuilder ??= proxyTypeBuilder.DefineProperty(proxyProperty.Name, PropertyAttributes.None, proxyProperty.PropertyType, null);

                        if (proxyProperty.CanRead)
                        {
                            propertyBuilder.SetGetMethod(GetFieldGetMethod(proxyTypeBuilder, targetType, proxyProperty, targetField, instanceField));
                        }

                        if (proxyProperty.CanWrite)
                        {
                            // Check if the target field is marked as InitOnly (readonly) and throw an exception in that case
                            if ((targetField.Attributes & FieldAttributes.InitOnly) != 0)
                            {
                                DuckTypeFieldIsReadonlyException.Throw(targetField);
                            }

                            // Check if the target field declaring type is an struct (structs modification is not supported)
                            if (targetField.DeclaringType.IsValueType)
                            {
                                DuckTypeStructMembersCannotBeChangedException.Throw(targetField.DeclaringType);
                            }

                            propertyBuilder.SetSetMethod(GetFieldSetMethod(proxyTypeBuilder, targetType, proxyProperty, targetField, instanceField));
                        }

                        break;
                }

                if (propertyBuilder is null)
                {
                    continue;
                }

                if (proxyProperty.CanRead && propertyBuilder.GetMethod is null)
                {
                    DuckTypePropertyOrFieldNotFoundException.Throw(proxyProperty.Name, duckAttribute.Name);
                }

                if (proxyProperty.CanWrite && propertyBuilder.SetMethod is null)
                {
                    DuckTypePropertyOrFieldNotFoundException.Throw(proxyProperty.Name, duckAttribute.Name);
                }
            }
        }

        private static void CreatePropertiesFromStruct(TypeBuilder proxyTypeBuilder, Type proxyDefinitionType, Type targetType, FieldInfo instanceField)
        {
            // Gets all fields to be copied
            foreach (var proxyFieldInfo in proxyDefinitionType.GetFields())
            {
                // Skip readonly fields
                if ((proxyFieldInfo.Attributes & FieldAttributes.InitOnly) != 0)
                {
                    continue;
                }

                // Ignore the fields marked with `DuckIgnore` attribute
                if (proxyFieldInfo.GetCustomAttribute<DuckIgnoreAttribute>(true) is not null)
                {
                    continue;
                }

                PropertyBuilder propertyBuilder = null;

                var duckAttribute = proxyFieldInfo.GetCustomAttribute<DuckAttribute>(true) ?? new DuckAttribute();
                duckAttribute.Name ??= proxyFieldInfo.Name;

                switch (duckAttribute.Kind)
                {
                    case DuckKind.Property:
                        var targetProperty = targetType.GetProperty(duckAttribute.Name, duckAttribute.BindingFlags);
                        if (targetProperty is null)
                        {
                            break;
                        }

                        // Check if the target property can be read
                        if (!targetProperty.CanRead)
                        {
                            DuckTypePropertyCantBeReadException.Throw(targetProperty);
                        }

                        propertyBuilder = proxyTypeBuilder.DefineProperty(proxyFieldInfo.Name, PropertyAttributes.None, proxyFieldInfo.FieldType, null);
                        propertyBuilder.SetGetMethod(GetPropertyGetMethod(proxyTypeBuilder, targetType, proxyFieldInfo, targetProperty, instanceField));
                        break;

                    case DuckKind.Field:
                        var targetField = targetType.GetField(duckAttribute.Name, duckAttribute.BindingFlags);
                        if (targetField is null)
                        {
                            break;
                        }

                        propertyBuilder = proxyTypeBuilder.DefineProperty(proxyFieldInfo.Name, PropertyAttributes.None, proxyFieldInfo.FieldType, null);
                        propertyBuilder.SetGetMethod(GetFieldGetMethod(proxyTypeBuilder, targetType, proxyFieldInfo, targetField, instanceField));
                        break;
                }

                if (propertyBuilder is null)
                {
                    DuckTypePropertyOrFieldNotFoundException.Throw(proxyFieldInfo.Name, duckAttribute.Name);
                }
            }
        }

        private static Delegate GetCreateProxyInstanceDelegate(ModuleBuilder moduleBuilder, Type proxyDefinitionType, Type proxyType, Type targetType)
        {
            var ctor = proxyType.GetConstructors()[0];

            var createProxyMethod = new DynamicMethod(
                $"CreateProxyInstance<{proxyType.Name}>",
                proxyDefinitionType,
                new[] { typeof(object) },
                typeof(DuckType).Module,
                true);
            var il = createProxyMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (UseDirectAccessTo(moduleBuilder, targetType))
            {
                if (targetType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, targetType);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, targetType);
                }
            }

            il.Emit(OpCodes.Newobj, ctor);

            if (proxyType.IsValueType)
            {
                il.Emit(OpCodes.Box, proxyType);
            }

            il.Emit(OpCodes.Ret);
            var delegateType = typeof(CreateProxyInstance<>).MakeGenericType(proxyDefinitionType);
            return createProxyMethod.CreateDelegate(delegateType);
        }

        private static Delegate CreateStructCopyMethod(ModuleBuilder moduleBuilder, Type proxyDefinitionType, Type proxyType, Type targetType)
        {
            var ctor = proxyType.GetConstructors()[0];

            var createStructMethod = new DynamicMethod(
                $"CreateStructInstance<{proxyType.Name}>",
                proxyDefinitionType,
                new[] { typeof(object) },
                typeof(DuckType).Module,
                true);
            var il = createStructMethod.GetILGenerator();

            // First we declare the locals
            var proxyLocal = il.DeclareLocal(proxyType);
            var structLocal = il.DeclareLocal(proxyDefinitionType);

            // We create an instance of the proxy type
            il.Emit(OpCodes.Ldloca_S, proxyLocal.LocalIndex);
            il.Emit(OpCodes.Ldarg_0);
            if (UseDirectAccessTo(moduleBuilder, targetType))
            {
                if (targetType.IsValueType)
					il.Emit(OpCodes.Unbox_Any, targetType);
				else
					il.Emit(OpCodes.Castclass, targetType);
			}

            il.Emit(OpCodes.Call, ctor);

            // Create the destination structure
            il.Emit(OpCodes.Ldloca_S, structLocal.LocalIndex);
            il.Emit(OpCodes.Initobj, proxyDefinitionType);

            // Start copy properties from the proxy to the structure
            foreach (var finfo in proxyDefinitionType.GetFields())
            {
                // Skip readonly fields
                if ((finfo.Attributes & FieldAttributes.InitOnly) != 0)
                {
                    continue;
                }

                // Ignore the fields marked with `DuckIgnore` attribute
                if (finfo.GetCustomAttribute<DuckIgnoreAttribute>(true) is not null)
                {
                    continue;
                }

                var prop = proxyType.GetProperty(finfo.Name);
                il.Emit(OpCodes.Ldloca_S, structLocal.LocalIndex);
                il.Emit(OpCodes.Ldloca_S, proxyLocal.LocalIndex);
                il.EmitCall(OpCodes.Call, prop.GetMethod, null);
                il.Emit(OpCodes.Stfld, finfo);
            }

            // Return
            il.WriteLoadLocal(structLocal.LocalIndex);
            il.Emit(OpCodes.Ret);

            var delegateType = typeof(CreateProxyInstance<>).MakeGenericType(proxyDefinitionType);
            return createStructMethod.CreateDelegate(delegateType);
        }

        /// <summary>
        /// Struct to store the result of creating a proxy type
        /// </summary>
        public readonly struct CreateTypeResult
        {
            /// <summary>
            /// Gets if the proxy type creation was successful
            /// </summary>
            public readonly bool Success;

            /// <summary>
            /// Target type
            /// </summary>
            public readonly Type TargetType;

            private readonly Type _proxyType;
            private readonly ExceptionDispatchInfo _exceptionInfo;
            private readonly Delegate _activator;

            /// <summary>
            /// Initializes a new instance of the <see cref="CreateTypeResult"/> struct.
            /// </summary>
            /// <param name="proxyTypeDefinition">Proxy type definition</param>
            /// <param name="proxyType">Proxy type</param>
            /// <param name="targetType">Target type</param>
            /// <param name="activator">Proxy activator</param>
            /// <param name="exceptionInfo">Exception dispatch info instance</param>
            internal CreateTypeResult(Type proxyTypeDefinition, Type proxyType, Type targetType, Delegate activator, ExceptionDispatchInfo exceptionInfo)
            {
                TargetType = targetType;
                _proxyType = proxyType;
                _activator = activator;
                _exceptionInfo = exceptionInfo;
                Success = proxyType != null && exceptionInfo == null;
                if (exceptionInfo != null)
                {
                    var methodInfo = typeof(CreateTypeResult).GetMethod(nameof(ThrowOnError), BindingFlags.NonPublic | BindingFlags.Instance);
                    _activator = methodInfo
                        .MakeGenericMethod(proxyTypeDefinition)
                        .CreateDelegate(
                        typeof(CreateProxyInstance<>).MakeGenericType(proxyTypeDefinition),
                        this);
                }
            }

            /// <summary>
            /// Gets the Proxy type
            /// </summary>
            public Type ProxyType
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    _exceptionInfo?.Throw();
                    return _proxyType;
                }
            }

            /// <summary>
            /// Create a new proxy instance from a target instance
            /// </summary>
            /// <typeparam name="T">Type of the return value</typeparam>
            /// <param name="instance">Target instance value</param>
            /// <returns>Proxy instance</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T CreateInstance<T>(object instance) => ((CreateProxyInstance<T>)_activator)(instance);

			/// <summary>
            /// Get if the proxy instance can be created
            /// </summary>
            /// <returns>true if the proxy can be created; otherwise, false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool CanCreate() => _exceptionInfo == null;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal IDuckType CreateInstance(object instance) => (IDuckType)_activator.DynamicInvoke(instance);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
            private T ThrowOnError<T>(object instance)
            {
                _exceptionInfo.Throw();
                return default;
            }
        }

        /// <summary>
        /// Generics Create Cache FastPath
        /// </summary>
        /// <typeparam name="T">Type of proxy definition</typeparam>
        public static class CreateCache<T>
        {
            /// <summary>
            /// Gets the type of T
            /// </summary>
            public static readonly Type Type = typeof(T);

            /// <summary>
            /// Gets if the T type is visible
            /// </summary>
            public static readonly bool IsVisible = Type.IsPublic || Type.IsNestedPublic;

            private static CreateTypeResult _fastPath = default;

            /// <summary>
            /// Gets the proxy type for a target type using the T proxy definition
            /// </summary>
            /// <param name="targetType">Target type</param>
            /// <returns>CreateTypeResult instance</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static CreateTypeResult GetProxy(Type targetType)
            {
                // We set a fast path for the first proxy type for a proxy definition. (It's likely to have a proxy definition just for one target type)
                var fastPath = _fastPath;
                if (fastPath.TargetType == targetType)
                {
                    return fastPath;
                }

                var result = GetProxySlow(targetType);

                fastPath = _fastPath;
                if (fastPath.TargetType is null)
                {
                    _fastPath = result;
                }

                return result;
            }

            /// <summary>
            /// Create a new instance of a proxy type for a target instance using the T proxy definition
            /// </summary>
            /// <param name="instance">Object instance</param>
            /// <returns>Proxy instance</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Create(object instance)
            {
                if (instance is null)
                {
                    return default;
                }

                return GetProxy(instance.GetType()).CreateInstance<T>(instance);
            }

            /// <summary>
            /// Get if the proxy instance can be created
            /// </summary>
            /// <param name="instance">Object instance</param>
            /// <returns>true if a proxy can be created; otherwise, false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool CanCreate(object instance)
            {
                if (instance is null)
                {
                    return false;
                }

                return GetProxy(instance.GetType()).CanCreate();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static CreateTypeResult GetProxySlow(Type targetType) => GetOrCreateProxyType(Type, targetType);
		}
    }
}
