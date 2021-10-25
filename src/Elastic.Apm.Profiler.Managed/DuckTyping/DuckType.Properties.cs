// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="DuckType.Properties.cs" company="Datadog">
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
    /// Duck Type
    /// </summary>
    public static partial class DuckType
    {
        private static MethodBuilder GetPropertyGetMethod(TypeBuilder proxyTypeBuilder, Type targetType, MemberInfo proxyMember, PropertyInfo targetProperty, FieldInfo instanceField)
        {
            var proxyMemberName = proxyMember.Name;
			var proxyMemberReturnType = typeof(object);
			var proxyParameterTypes = Type.EmptyTypes;
			var targetParametersTypes = GetPropertyGetParametersTypes(proxyTypeBuilder, targetProperty, true).ToArray();

            if (proxyMember is PropertyInfo proxyProperty)
            {
                proxyMemberReturnType = proxyProperty.PropertyType;
                proxyParameterTypes = GetPropertyGetParametersTypes(proxyTypeBuilder, proxyProperty, true).ToArray();
                if (proxyParameterTypes.Length != targetParametersTypes.Length)
                {
                    DuckTypePropertyArgumentsLengthException.Throw(proxyProperty);
                }
            }
            else if (proxyMember is FieldInfo proxyField)
            {
                proxyMemberReturnType = proxyField.FieldType;
                proxyParameterTypes = Type.EmptyTypes;
                if (proxyParameterTypes.Length != targetParametersTypes.Length)
                {
                    DuckTypePropertyArgumentsLengthException.Throw(targetProperty);
                }
            }

			var proxyMethod = proxyTypeBuilder.DefineMethod(
                "get_" + proxyMemberName,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                proxyMemberReturnType,
                proxyParameterTypes);

			var il = new LazyILGenerator(proxyMethod.GetILGenerator());
			var targetMethod = targetProperty.GetMethod;
			var returnType = targetProperty.PropertyType;

            // Load the instance if needed
            if (!targetMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(instanceField.FieldType.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, instanceField);
            }

            // Load the indexer keys to the stack
            for (var pIndex = 0; pIndex < proxyParameterTypes.Length; pIndex++)
            {
				var proxyParamType = proxyParameterTypes[pIndex];
				var targetParamType = targetParametersTypes[pIndex];

                // Check if the type can be converted of if we need to enable duck chaining
                if (NeedsDuckChaining(targetParamType, proxyParamType))
                {
                    // Load the argument and cast it as Duck type
                    il.WriteLoadArgument(pIndex, false);
                    il.Emit(OpCodes.Castclass, typeof(IDuckType));

                    // Call IDuckType.Instance property to get the actual value
                    il.EmitCall(OpCodes.Callvirt, DuckTypeInstancePropertyInfo.GetMethod, null);
                    targetParamType = typeof(object);
                }
                else
                {
                    il.WriteLoadArgument(pIndex, false);
                }

                // If the target parameter type is public or if it's by ref we have to actually use the original target type.
                targetParamType = UseDirectAccessTo(proxyTypeBuilder, targetParamType) || targetParamType.IsByRef ? targetParamType : typeof(object);
                il.WriteTypeConversion(proxyParamType, targetParamType);

                targetParametersTypes[pIndex] = targetParamType;
            }

            // Call the getter method
            if (UseDirectAccessTo(proxyTypeBuilder, targetType))
            {
                // If the instance is public we can emit directly without any dynamic method

                // Method call
                if (targetMethod.IsPublic)
                {
                    // We can emit a normal call if we have a public instance with a public property method.
                    il.EmitCall(targetMethod.IsStatic || instanceField.FieldType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, targetMethod, null);
                }
                else
                {
                    // In case we have a public instance and a non public property method we can use [Calli] with the function pointer
                    il.WriteMethodCalli(targetMethod);
                }
            }
            else
            {
                // If the instance is not public we need to create a Dynamic method to overpass the visibility checks
                // we can't access non public types so we have to cast to object type (in the instance object and the return type).

				var dynMethodName = $"_getNonPublicProperty_{targetProperty.DeclaringType.Name}_{targetProperty.Name}";
                returnType = UseDirectAccessTo(proxyTypeBuilder, targetProperty.PropertyType) ? targetProperty.PropertyType : typeof(object);

                // We create the dynamic method
				var targetParameters = GetPropertyGetParametersTypes(proxyTypeBuilder, targetProperty, false, !targetMethod.IsStatic).ToArray();
				var dynParameters = targetMethod.IsStatic ? targetParametersTypes : (new[] { typeof(object) }).Concat(targetParametersTypes).ToArray();
				var dynMethod = new DynamicMethod(dynMethodName, returnType, dynParameters, proxyTypeBuilder.Module, true);

                // Emit the dynamic method body
				var dynIL = new LazyILGenerator(dynMethod.GetILGenerator());

                if (!targetMethod.IsStatic)
                {
                    dynIL.LoadInstanceArgument(typeof(object), targetProperty.DeclaringType);
                }

                for (var idx = targetMethod.IsStatic ? 0 : 1; idx < dynParameters.Length; idx++)
                {
                    dynIL.WriteLoadArgument(idx, true);
                    dynIL.WriteTypeConversion(dynParameters[idx], targetParameters[idx]);
                }

                dynIL.EmitCall(targetMethod.IsStatic || instanceField.FieldType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, targetMethod, null);
                dynIL.WriteTypeConversion(targetProperty.PropertyType, returnType);
                dynIL.Emit(OpCodes.Ret);
                dynIL.Flush();

                // Emit the call to the dynamic method
                il.WriteDynamicMethodCall(dynMethod, proxyTypeBuilder);
            }

            // Handle the return value
            // Check if the type can be converted or if we need to enable duck chaining
            if (NeedsDuckChaining(targetProperty.PropertyType, proxyMemberReturnType))
            {
                if (UseDirectAccessTo(proxyTypeBuilder, targetProperty.PropertyType) && targetProperty.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, targetProperty.PropertyType);
                }

                // We call DuckType.CreateCache<>.Create()
				var getProxyMethodInfo = typeof(CreateCache<>)
                    .MakeGenericType(proxyMemberReturnType).GetMethod("Create");

                il.Emit(OpCodes.Call, getProxyMethodInfo);
            }
            else if (returnType != proxyMemberReturnType)
            {
                // If the type is not the expected type we try a conversion.
                il.WriteTypeConversion(returnType, proxyMemberReturnType);
            }

            il.Emit(OpCodes.Ret);
            il.Flush();
            _methodBuilderGetToken.Invoke(proxyMethod, null);
            return proxyMethod;
        }

        private static MethodBuilder GetPropertySetMethod(TypeBuilder proxyTypeBuilder, Type targetType, MemberInfo proxyMember, PropertyInfo targetProperty, FieldInfo instanceField)
        {
            string proxyMemberName = null;
			var proxyParameterTypes = Type.EmptyTypes;
			var targetParametersTypes = GetPropertySetParametersTypes(proxyTypeBuilder, targetProperty, true).ToArray();

            if (proxyMember is PropertyInfo proxyProperty)
            {
                proxyMemberName = proxyProperty.Name;
                proxyParameterTypes = GetPropertySetParametersTypes(proxyTypeBuilder, proxyProperty, true).ToArray();
                if (proxyParameterTypes.Length != targetParametersTypes.Length)
                {
                    DuckTypePropertyArgumentsLengthException.Throw(proxyProperty);
                }
            }
            else if (proxyMember is FieldInfo proxyField)
            {
                proxyMemberName = proxyField.Name;
                proxyParameterTypes = new Type[] { proxyField.FieldType };
                if (proxyParameterTypes.Length != targetParametersTypes.Length)
                {
                    DuckTypePropertyArgumentsLengthException.Throw(targetProperty);
                }
            }

			var proxyMethod = proxyTypeBuilder.DefineMethod(
                "set_" + proxyMemberName,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(void),
                proxyParameterTypes);

			var il = new LazyILGenerator(proxyMethod.GetILGenerator());
			var targetMethod = targetProperty.SetMethod;

            // Load the instance if needed
            if (!targetMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(instanceField.FieldType.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, instanceField);
            }

            // Load the indexer keys and set value to the stack
            for (var pIndex = 0; pIndex < proxyParameterTypes.Length; pIndex++)
            {
				var proxyParamType = proxyParameterTypes[pIndex];
				var targetParamType = targetParametersTypes[pIndex];

                // Check if the type can be converted of if we need to enable duck chaining
                if (NeedsDuckChaining(targetParamType, proxyParamType))
                {
                    // Load the argument and cast it as Duck type
                    il.WriteLoadArgument(pIndex, false);
                    il.Emit(OpCodes.Castclass, typeof(IDuckType));

                    // Call IDuckType.Instance property to get the actual value
                    il.EmitCall(OpCodes.Callvirt, DuckTypeInstancePropertyInfo.GetMethod, null);

                    targetParamType = typeof(object);
                }
                else
					il.WriteLoadArgument(pIndex, false);

				// If the target parameter type is public or if it's by ref we have to actually use the original target type.
                targetParamType = UseDirectAccessTo(proxyTypeBuilder, targetParamType) || targetParamType.IsByRef ? targetParamType : typeof(object);
                il.WriteTypeConversion(proxyParamType, targetParamType);

                targetParametersTypes[pIndex] = targetParamType;
            }

            // Call the setter method
            if (UseDirectAccessTo(proxyTypeBuilder, targetType))
            {
                // If the instance is public we can emit directly without any dynamic method

                if (targetMethod.IsPublic)
                {
                    // We can emit a normal call if we have a public instance with a public property method.
                    il.EmitCall(targetMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, targetMethod, null);
                }
                else
                {
                    // In case we have a public instance and a non public property method we can use [Calli] with the function pointer
                    il.WriteMethodCalli(targetMethod);
                }
            }
            else
            {
                // If the instance is not public we need to create a Dynamic method to overpass the visibility checks
                // we can't access non public types so we have to cast to object type (in the instance object and the return type).

				var dynMethodName = $"_setNonPublicProperty+{targetProperty.DeclaringType.Name}.{targetProperty.Name}";

                // We create the dynamic method
				var targetParameters = GetPropertySetParametersTypes(proxyTypeBuilder, targetProperty, false, !targetMethod.IsStatic).ToArray();
				var dynParameters = targetMethod.IsStatic ? targetParametersTypes : (new[] { typeof(object) }).Concat(targetParametersTypes).ToArray();
				var dynMethod = new DynamicMethod(dynMethodName, typeof(void), dynParameters, proxyTypeBuilder.Module, true);

                // Emit the dynamic method body
				var dynIL = new LazyILGenerator(dynMethod.GetILGenerator());

                if (!targetMethod.IsStatic)
                {
                    dynIL.LoadInstanceArgument(typeof(object), targetProperty.DeclaringType);
                }

                for (var idx = targetMethod.IsStatic ? 0 : 1; idx < dynParameters.Length; idx++)
                {
                    dynIL.WriteLoadArgument(idx, true);
                    dynIL.WriteTypeConversion(dynParameters[idx], targetParameters[idx]);
                }

                dynIL.EmitCall(targetMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, targetMethod, null);
                dynIL.Emit(OpCodes.Ret);
                dynIL.Flush();

                // Create and load delegate for the DynamicMethod
                il.WriteDynamicMethodCall(dynMethod, proxyTypeBuilder);
            }

            il.Emit(OpCodes.Ret);
            il.Flush();
            _methodBuilderGetToken.Invoke(proxyMethod, null);
            return proxyMethod;
        }

        private static IEnumerable<Type> GetPropertyGetParametersTypes(TypeBuilder typeBuilder, PropertyInfo property, bool originalTypes, bool isDynamicSignature = false)
        {
            if (isDynamicSignature)
				yield return typeof(object);

			var idxParams = property.GetIndexParameters();
            foreach (var parameter in idxParams)
            {
                if (originalTypes || UseDirectAccessTo(typeBuilder, parameter.ParameterType))
					yield return parameter.ParameterType;
				else
					yield return typeof(object);
			}
        }

        private static IEnumerable<Type> GetPropertySetParametersTypes(TypeBuilder typeBuilder, PropertyInfo property, bool originalTypes, bool isDynamicSignature = false)
        {
            if (isDynamicSignature)
				yield return typeof(object);

			foreach (var indexType in GetPropertyGetParametersTypes(typeBuilder, property, originalTypes))
				yield return indexType;

			if (originalTypes || UseDirectAccessTo(typeBuilder, property.PropertyType))
				yield return property.PropertyType;
			else
				yield return typeof(object);
		}
    }
}
