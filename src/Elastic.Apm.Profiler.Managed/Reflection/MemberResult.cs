// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="MemberResult.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;

namespace Elastic.Apm.Profiler.Managed.Reflection
{
    internal readonly struct MemberResult<T>
    {
        /// <summary>
        /// A static value used to represent a member that was not found.
        /// </summary>
        public static readonly MemberResult<T> NotFound = default;

        public readonly bool HasValue;

        private readonly T _value;

        public MemberResult(T value)
        {
            _value = value;
            HasValue = true;
        }

        public T Value =>
            HasValue
                ? _value
                : throw new InvalidOperationException("Reflected member not found.");

        public T GetValueOrDefault() => _value;

		public MemberResult<TResult> GetProperty<TResult>(string propertyName)
        {
            if (!HasValue || Value == null || !Value.TryGetPropertyValue(propertyName, out TResult result))
            {
                return MemberResult<TResult>.NotFound;
            }

            return new MemberResult<TResult>(result);
        }

        public MemberResult<object> GetProperty(string propertyName) => GetProperty<object>(propertyName);

		public MemberResult<TResult> GetField<TResult>(string fieldName)
        {
            if (!HasValue || Value == null || !Value.TryGetFieldValue(fieldName, out TResult result))
            {
                return MemberResult<TResult>.NotFound;
            }

            return new MemberResult<TResult>(result);
        }

        public MemberResult<object> GetField(string fieldName) => GetField<object>(fieldName);

		public MemberResult<TResult> CallMethod<TArg1, TResult>(string methodName, TArg1 arg1)
        {
            if (!HasValue || Value == null || !Value.TryCallMethod(methodName, arg1, out TResult result))
            {
                return MemberResult<TResult>.NotFound;
            }

            return new MemberResult<TResult>(result);
        }

        public MemberResult<object> CallMethod<TArg1>(string methodName, TArg1 arg1) => CallMethod<TArg1, object>(methodName, arg1);

		public override string ToString()
        {
            if (!HasValue || Value == null)
            {
                return string.Empty;
            }

            return Value.ToString();
        }
    }
}
