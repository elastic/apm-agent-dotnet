// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="DuckTypeExceptions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics;
using System.Reflection;

#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1402 // File may only contain a single class

namespace Elastic.Apm.Profiler.Managed.DuckTyping
{
    /// <summary>
    /// DuckType Exception
    /// </summary>
    public class DuckTypeException : Exception
    {
        internal DuckTypeException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// DuckType proxy type definition is null
    /// </summary>
    public class DuckTypeProxyTypeDefinitionIsNull : DuckTypeException
    {
        private DuckTypeProxyTypeDefinitionIsNull()
            : base($"The proxy type definition is null.")
        {
        }

        [DebuggerHidden]
        internal static void Throw() => throw new DuckTypeProxyTypeDefinitionIsNull();
	}

    /// <summary>
    /// DuckType target object instance is null
    /// </summary>
    public class DuckTypeTargetObjectInstanceIsNull : DuckTypeException
    {
        private DuckTypeTargetObjectInstanceIsNull()
            : base($"The target object instance is null.")
        {
        }

        [DebuggerHidden]
        internal static void Throw() => throw new DuckTypeTargetObjectInstanceIsNull();
	}

    /// <summary>
    /// DuckType invalid type conversion exception
    /// </summary>
    public class DuckTypeInvalidTypeConversionException : DuckTypeException
    {
        private DuckTypeInvalidTypeConversionException(Type actualType, Type expectedType)
            : base($"Invalid type conversion from {actualType.FullName} to {expectedType.FullName}")
        {
        }

        [DebuggerHidden]
        internal static void Throw(Type actualType, Type expectedType) => throw new DuckTypeInvalidTypeConversionException(actualType, expectedType);
	}

    /// <summary>
    /// DuckType property can't be read
    /// </summary>
    public class DuckTypePropertyCantBeReadException : DuckTypeException
    {
        private DuckTypePropertyCantBeReadException(PropertyInfo property)
            : base($"The property '{property.Name}' can't be read, you should remove the getter from the proxy definition base type class or interface.")
        {
        }

        [DebuggerHidden]
        internal static void Throw(PropertyInfo property) => throw new DuckTypePropertyCantBeReadException(property);
	}

    /// <summary>
    /// DuckType property can't be written
    /// </summary>
    public class DuckTypePropertyCantBeWrittenException : DuckTypeException
    {
        private DuckTypePropertyCantBeWrittenException(PropertyInfo property)
            : base($"The property '{property.Name}' can't be written, you should remove the setter from the proxy definition base type class or interface.")
        {
        }

        [DebuggerHidden]
        internal static void Throw(PropertyInfo property) => throw new DuckTypePropertyCantBeWrittenException(property);
	}

    /// <summary>
    /// DuckType property argument doesn't have the same argument length
    /// </summary>
    public class DuckTypePropertyArgumentsLengthException : DuckTypeException
    {
        private DuckTypePropertyArgumentsLengthException(PropertyInfo property)
            : base($"The property '{property.Name}' doesn't have the same number of arguments as the original property.")
        {
        }

        [DebuggerHidden]
        internal static void Throw(PropertyInfo property) => throw new DuckTypePropertyArgumentsLengthException(property);
	}

    /// <summary>
    /// DuckType field is readonly
    /// </summary>
    public class DuckTypeFieldIsReadonlyException : DuckTypeException
    {
        private DuckTypeFieldIsReadonlyException(FieldInfo field)
            : base($"The field '{field.Name}' is marked as readonly, you should remove the setter from the base type class or interface.")
        {
        }

        [DebuggerHidden]
        internal static void Throw(FieldInfo field) => throw new DuckTypeFieldIsReadonlyException(field);
	}

    /// <summary>
    /// DuckType property or field not found
    /// </summary>
    public class DuckTypePropertyOrFieldNotFoundException : DuckTypeException
    {
        private DuckTypePropertyOrFieldNotFoundException(string name, string duckAttributeName)
            : base($"The property or field '{duckAttributeName}' for the proxy property '{name}' was not found in the instance.")
        {
        }

        [DebuggerHidden]
        internal static void Throw(string name, string duckAttributeName) => throw new DuckTypePropertyOrFieldNotFoundException(name, duckAttributeName);
	}

    /// <summary>
    /// DuckType type is not public exception
    /// </summary>
    public class DuckTypeTypeIsNotPublicException : DuckTypeException
    {
        private DuckTypeTypeIsNotPublicException(Type type, string argumentName)
            : base($"The type '{type.FullName}' must be public, argument: '{argumentName}'")
        {
        }

        [DebuggerHidden]
        internal static void Throw(Type type, string argumentName) => throw new DuckTypeTypeIsNotPublicException(type, argumentName);
	}

    /// <summary>
    /// DuckType struct members cannot be changed exception
    /// </summary>
    public class DuckTypeStructMembersCannotBeChangedException : DuckTypeException
    {
        private DuckTypeStructMembersCannotBeChangedException(Type type)
            : base($"Modifying struct members is not supported. [{type.FullName}]")
        {
        }

        [DebuggerHidden]
        internal static void Throw(Type type) => throw new DuckTypeStructMembersCannotBeChangedException(type);
	}

    /// <summary>
    /// DuckType target method can not be found exception
    /// </summary>
    public class DuckTypeTargetMethodNotFoundException : DuckTypeException
    {
        private DuckTypeTargetMethodNotFoundException(MethodInfo method)
            : base($"The target method for the proxy method '{method}' was not found.")
        {
        }

        [DebuggerHidden]
        internal static void Throw(MethodInfo method) => throw new DuckTypeTargetMethodNotFoundException(method);
	}

    /// <summary>
    /// DuckType proxy method parameter is missing exception
    /// </summary>
    public class DuckTypeProxyMethodParameterIsMissingException : DuckTypeException
    {
        private DuckTypeProxyMethodParameterIsMissingException(MethodInfo proxyMethod, ParameterInfo targetParameterInfo)
            : base($"The proxy method '{proxyMethod.Name}' is missing parameter '{targetParameterInfo.Name}' declared in the target method.")
        {
        }

        [DebuggerHidden]
        internal static void Throw(MethodInfo proxyMethod, ParameterInfo targetParameterInfo) => throw new DuckTypeProxyMethodParameterIsMissingException(proxyMethod, targetParameterInfo);
	}

    /// <summary>
    /// DuckType parameter signature mismatch between proxy and target method
    /// </summary>
    public class DuckTypeProxyAndTargetMethodParameterSignatureMismatchException : DuckTypeException
    {
        private DuckTypeProxyAndTargetMethodParameterSignatureMismatchException(MethodInfo proxyMethod, MethodInfo targetMethod)
            : base($"Parameter signature mismatch between proxy '{proxyMethod}' and target method '{targetMethod}'")
        {
        }

        [DebuggerHidden]
        internal static void Throw(MethodInfo proxyMethod, MethodInfo targetMethod) => throw new DuckTypeProxyAndTargetMethodParameterSignatureMismatchException(proxyMethod, targetMethod);
	}

    /// <summary>
    /// DuckType proxy methods with generic parameters are not supported in non public instances exception
    /// </summary>
    public class DuckTypeProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException : DuckTypeException
    {
        private DuckTypeProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException(MethodInfo proxyMethod)
            : base($"The proxy method with generic parameters '{proxyMethod}' are not supported on non public instances")
        {
        }

        [DebuggerHidden]
        internal static void Throw(MethodInfo proxyMethod) => throw new DuckTypeProxyMethodsWithGenericParametersNotSupportedInNonPublicInstancesException(proxyMethod);
	}

    /// <summary>
    /// DuckType proxy method has an ambiguous match in the target type exception
    /// </summary>
    public class DuckTypeTargetMethodAmbiguousMatchException : DuckTypeException
    {
        private DuckTypeTargetMethodAmbiguousMatchException(MethodInfo proxyMethod, MethodInfo targetMethod, MethodInfo targetMethod2)
            : base($"The proxy method '{proxyMethod}' matches at least two methods in the target type. Method1 = '{targetMethod}' and Method2 = '{targetMethod2}'")
        {
        }

        [DebuggerHidden]
        internal static void Throw(MethodInfo proxyMethod, MethodInfo targetMethod, MethodInfo targetMethod2) => throw new DuckTypeTargetMethodAmbiguousMatchException(proxyMethod, targetMethod, targetMethod2);
	}
}
