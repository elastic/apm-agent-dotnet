// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="ObjectExtensions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Elastic.Apm.Profiler.Managed.Reflection
{
    /// <summary>
    /// Provides helper methods to access object members by emitting IL dynamically.
    /// </summary>
    internal static class ObjectExtensions
    {
        // A new module to be emitted in the current AppDomain which will contain DynamicMethods
        // and have same evidence/permissions as this AppDomain
        internal static readonly ModuleBuilder Module;

        private static readonly ConcurrentDictionary<PropertyFetcherCacheKey, object> Cache = new ConcurrentDictionary<PropertyFetcherCacheKey, object>();
        private static readonly ConcurrentDictionary<PropertyFetcherCacheKey, PropertyFetcher> PropertyFetcherCache = new ConcurrentDictionary<PropertyFetcherCacheKey, PropertyFetcher>();

        static ObjectExtensions()
        {
#if NETFRAMEWORK
            var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("Elastic.Apm.Profiler.Managed.DynamicAssembly"), AssemblyBuilderAccess.Run);
#else
            var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Elastic.Apm.Profiler.Managed.DynamicAssembly"), AssemblyBuilderAccess.Run);
#endif
            Module = asm.DefineDynamicModule("DynamicModule");
        }

        /// <summary>
        /// Tries to call an instance method with the specified name, a single parameter, and a return value.
        /// </summary>
        /// <typeparam name="TArg1">The type of the method's single parameter.</typeparam>
        /// <typeparam name="TResult">The type of the method's result value.</typeparam>
        /// <param name="source">The object to call the method on.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="arg1">The value to pass as the method's single argument.</param>
        /// <param name="value">The value returned by the method.</param>
        /// <returns><c>true</c> if the method was found, <c>false</c> otherwise.</returns>
        public static bool TryCallMethod<TArg1, TResult>(this object source, string methodName, TArg1 arg1, out TResult value)
        {
            var type = source.GetType();
            var paramType1 = typeof(TArg1);
            var returnType = typeof(TResult);

            var cachedItem = Cache.GetOrAdd(
                new PropertyFetcherCacheKey(type, paramType1, returnType, methodName),
                key =>
                    DynamicMethodBuilder<Func<object, TArg1, TResult>>
                       .CreateMethodCallDelegate(
                            key.Type1,
                            key.Name,
                            OpCodeValue.Callvirt,
                            methodParameterTypes: new[] { key.Type2 }));

            if (cachedItem is Func<object, TArg1, TResult> func)
            {
                value = func(source, arg1);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to call an instance method with the specified name, two parameters, and no return value.
        /// </summary>
        /// <typeparam name="TArg1">The type of the method's first parameter.</typeparam>
        /// <typeparam name="TArg2">The type of the method's second parameter.</typeparam>
        /// <param name="source">The object to call the method on.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="arg1">The value to pass as the method's first argument.</param>
        /// <param name="arg2">The value to pass as the method's second argument.</param>
        /// <returns><c>true</c> if the method was found, <c>false</c> otherwise.</returns>
        public static bool TryCallVoidMethod<TArg1, TArg2>(this object source, string methodName, TArg1 arg1, TArg2 arg2)
        {
            var type = source.GetType();
            var paramType1 = typeof(TArg1);
            var paramType2 = typeof(TArg2);

            var cachedItem = Cache.GetOrAdd(
                new PropertyFetcherCacheKey(type, paramType1, paramType2, methodName),
                key =>
                    DynamicMethodBuilder<Action<object, TArg1, TArg2>>
                       .CreateMethodCallDelegate(
                            key.Type1,
                            key.Name,
                            OpCodeValue.Callvirt,
                            methodParameterTypes: new[] { key.Type2, key.Type3 }));

            if (cachedItem is Action<object, TArg1, TArg2> func)
            {
                func(source, arg1, arg2);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to call an instance method with the specified name and a return value.
        /// </summary>
        /// <typeparam name="TResult">The type of the method's result value.</typeparam>
        /// <param name="source">The object to call the method on.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="value">The value returned by the method.</param>
        /// <returns><c>true</c> if the method was found, <c>false</c> otherwise.</returns>
        public static bool TryCallMethod<TResult>(this object source, string methodName, out TResult value)
        {
            var type = source.GetType();
            var returnType = typeof(TResult);

            var cachedItem = Cache.GetOrAdd(
                new PropertyFetcherCacheKey(type, returnType, methodName),
                key =>
                    DynamicMethodBuilder<Func<object, TResult>>
                       .CreateMethodCallDelegate(
                            key.Type1,
                            key.Name,
                            OpCodeValue.Callvirt));

            if (cachedItem is Func<object, TResult> func)
            {
                value = func(source);
                return true;
            }

            value = default;
            return false;
        }

        public static MemberResult<TResult> CallMethod<TArg1, TResult>(this object source, string methodName, TArg1 arg1) =>
			source.TryCallMethod(methodName, arg1, out TResult result)
				? new MemberResult<TResult>(result)
				: MemberResult<TResult>.NotFound;

		public static MemberResult<TResult> CallMethod<TResult>(this object source, string methodName) =>
			source.TryCallMethod(methodName, out TResult result)
				? new MemberResult<TResult>(result)
				: MemberResult<TResult>.NotFound;

		public static MemberResult<object> CallVoidMethod<TArg1, TArg2>(this object source, string methodName, TArg1 arg1, TArg2 arg2) =>
			source.TryCallVoidMethod(methodName, arg1, arg2)
				? new MemberResult<object>(null)
				: MemberResult<object>.NotFound;

		/// <summary>
        /// Tries to get the value of an instance property with the specified name.
        /// </summary>
        /// <typeparam name="TResult">The type of the property.</typeparam>
        /// <param name="source">The value that contains the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value of the property, or <c>null</c> if the property is not found.</param>
        /// <returns><c>true</c> if the property exists, otherwise <c>false</c>.</returns>
        public static bool TryGetPropertyValue<TResult>(this object source, string propertyName, out TResult value)
        {
            if (source != null)
            {
                var type = source.GetType();

                var fetcher = PropertyFetcherCache.GetOrAdd(
                    GetKey<TResult>(propertyName, type),
                    key => new PropertyFetcher(key.Name));

                if (fetcher != null)
                {
                    value = fetcher.Fetch<TResult>(source, type);
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static MemberResult<TResult> GetProperty<TResult>(this object source, string propertyName)
        {
            if (source == null)
            {
                return MemberResult<TResult>.NotFound;
            }

            return source.TryGetPropertyValue(propertyName, out TResult result)
                       ? new MemberResult<TResult>(result)
                       : MemberResult<TResult>.NotFound;
        }

        public static MemberResult<object> GetProperty(this object source, string propertyName) =>
			GetProperty<object>(source, propertyName);

		/// <summary>
        /// Tries to get the value of an instance field with the specified name.
        /// </summary>
        /// <typeparam name="TResult">The type of the field.</typeparam>
        /// <param name="source">The value that contains the field.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value of the field, or <c>null</c> if the field is not found.</param>
        /// <returns><c>true</c> if the field exists, otherwise <c>false</c>.</returns>
        public static bool TryGetFieldValue<TResult>(this object source, string fieldName, out TResult value)
        {
            var type = source.GetType();

            var cachedItem = Cache.GetOrAdd(
                GetKey<TResult>(fieldName, type),
                key => CreateFieldDelegate<TResult>(key.Type1, key.Name));

            if (cachedItem is Func<object, TResult> func)
            {
                value = func(source);
                return true;
            }

            value = default;
            return false;
        }

        public static MemberResult<TResult> GetField<TResult>(this object source, string fieldName) =>
			source.TryGetFieldValue(fieldName, out TResult result)
				? new MemberResult<TResult>(result)
				: MemberResult<TResult>.NotFound;

		private static PropertyFetcherCacheKey GetKey<TResult>(string name, Type type) =>
			new PropertyFetcherCacheKey(type, typeof(TResult), name);

		private static Func<object, TResult> CreateFieldDelegate<TResult>(Type containerType, string fieldName)
        {
            var fieldInfo = containerType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null)
            {
                return null;
            }

            var dynamicMethod = new DynamicMethod($"{containerType.FullName}.{fieldName}", typeof(TResult), new Type[] { typeof(object) }, ObjectExtensions.Module, skipVisibility: true);
            var il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);

            if (containerType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, containerType);
            }
            else
            {
                il.Emit(OpCodes.Castclass, containerType);
            }

            il.Emit(OpCodes.Ldfld, fieldInfo);

            if (fieldInfo.FieldType.IsValueType && typeof(TResult) == typeof(object))
            {
                il.Emit(OpCodes.Box, fieldInfo.FieldType);
            }
            else if (fieldInfo.FieldType != typeof(TResult))
            {
                il.Emit(OpCodes.Castclass, typeof(TResult));
            }

            il.Emit(OpCodes.Ret);
            return (Func<object, TResult>)dynamicMethod.CreateDelegate(typeof(Func<object, TResult>));
        }

        private readonly struct PropertyFetcherCacheKey : IEquatable<PropertyFetcherCacheKey>
        {
            public readonly Type Type1;
            public readonly Type Type2;
            public readonly Type Type3;
            public readonly string Name;

            public PropertyFetcherCacheKey(Type type1, Type type2, string name)
                : this(type1, type2, null, name)
            {
            }

            public PropertyFetcherCacheKey(Type type1, Type type2, Type type3, string name)
            {
                Type1 = type1 ?? throw new ArgumentNullException(nameof(type1));
                Type2 = type2;
                Type3 = type3;
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public bool Equals(PropertyFetcherCacheKey other) =>
				Equals(Type1, other.Type1) && Equals(Type2, other.Type2) && Equals(Type3, other.Type3) && Name == other.Name;

			public override bool Equals(object obj) =>
				obj is PropertyFetcherCacheKey other && Equals(other);

			public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Type1.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Type2 != null ? Type2.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Type3 != null ? Type3.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ Name.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}
