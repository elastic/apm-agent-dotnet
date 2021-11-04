// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ValueTypeFieldTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using Elastic.Apm.Profiler.Managed.DuckTyping;
using Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Fields.ValueType.ProxiesDefinitions;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Fields.ValueType
{
    public class ValueTypeFieldTests
    {
        public static IEnumerable<object[]> Data()
        {
            return new[]
            {
                new object[] { ObscureObject.GetFieldPublicObject() },
                new object[] { ObscureObject.GetFieldInternalObject() },
                new object[] { ObscureObject.GetFieldPrivateObject() },
            };
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StaticReadonlyFieldsSetException(object obscureObject)
        {
            Assert.Throws<DuckTypeFieldIsReadonlyException>(() =>
            {
                obscureObject.DuckCast<IObscureStaticReadonlyErrorDuckType>();
            });
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void ReadonlyFieldsSetException(object obscureObject)
        {
            Assert.Throws<DuckTypeFieldIsReadonlyException>(() =>
            {
                obscureObject.DuckCast<IObscureReadonlyErrorDuckType>();
            });
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StaticReadonlyFields(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // *
            Assert.Equal(10, duckInterface.PublicStaticReadonlyValueTypeField);
            Assert.Equal(10, duckAbstract.PublicStaticReadonlyValueTypeField);
            Assert.Equal(10, duckVirtual.PublicStaticReadonlyValueTypeField);

            // *
            Assert.Equal(11, duckInterface.InternalStaticReadonlyValueTypeField);
            Assert.Equal(11, duckAbstract.InternalStaticReadonlyValueTypeField);
            Assert.Equal(11, duckVirtual.InternalStaticReadonlyValueTypeField);

            // *
            Assert.Equal(12, duckInterface.ProtectedStaticReadonlyValueTypeField);
            Assert.Equal(12, duckAbstract.ProtectedStaticReadonlyValueTypeField);
            Assert.Equal(12, duckVirtual.ProtectedStaticReadonlyValueTypeField);

            // *
            Assert.Equal(13, duckInterface.PrivateStaticReadonlyValueTypeField);
            Assert.Equal(13, duckAbstract.PrivateStaticReadonlyValueTypeField);
            Assert.Equal(13, duckVirtual.PrivateStaticReadonlyValueTypeField);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StaticFields(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Equal(20, duckInterface.PublicStaticValueTypeField);
            Assert.Equal(20, duckAbstract.PublicStaticValueTypeField);
            Assert.Equal(20, duckVirtual.PublicStaticValueTypeField);

            duckInterface.PublicStaticValueTypeField = 42;
            Assert.Equal(42, duckInterface.PublicStaticValueTypeField);
            Assert.Equal(42, duckAbstract.PublicStaticValueTypeField);
            Assert.Equal(42, duckVirtual.PublicStaticValueTypeField);

            duckAbstract.PublicStaticValueTypeField = 50;
            Assert.Equal(50, duckInterface.PublicStaticValueTypeField);
            Assert.Equal(50, duckAbstract.PublicStaticValueTypeField);
            Assert.Equal(50, duckVirtual.PublicStaticValueTypeField);

            duckVirtual.PublicStaticValueTypeField = 60;
            Assert.Equal(60, duckInterface.PublicStaticValueTypeField);
            Assert.Equal(60, duckAbstract.PublicStaticValueTypeField);
            Assert.Equal(60, duckVirtual.PublicStaticValueTypeField);

            // *

            Assert.Equal(21, duckInterface.InternalStaticValueTypeField);
            Assert.Equal(21, duckAbstract.InternalStaticValueTypeField);
            Assert.Equal(21, duckVirtual.InternalStaticValueTypeField);

            duckInterface.InternalStaticValueTypeField = 42;
            Assert.Equal(42, duckInterface.InternalStaticValueTypeField);
            Assert.Equal(42, duckAbstract.InternalStaticValueTypeField);
            Assert.Equal(42, duckVirtual.InternalStaticValueTypeField);

            duckAbstract.InternalStaticValueTypeField = 50;
            Assert.Equal(50, duckInterface.InternalStaticValueTypeField);
            Assert.Equal(50, duckAbstract.InternalStaticValueTypeField);
            Assert.Equal(50, duckVirtual.InternalStaticValueTypeField);

            duckVirtual.InternalStaticValueTypeField = 60;
            Assert.Equal(60, duckInterface.InternalStaticValueTypeField);
            Assert.Equal(60, duckAbstract.InternalStaticValueTypeField);
            Assert.Equal(60, duckVirtual.InternalStaticValueTypeField);

            // *

            Assert.Equal(22, duckInterface.ProtectedStaticValueTypeField);
            Assert.Equal(22, duckAbstract.ProtectedStaticValueTypeField);
            Assert.Equal(22, duckVirtual.ProtectedStaticValueTypeField);

            duckInterface.ProtectedStaticValueTypeField = 42;
            Assert.Equal(42, duckInterface.ProtectedStaticValueTypeField);
            Assert.Equal(42, duckAbstract.ProtectedStaticValueTypeField);
            Assert.Equal(42, duckVirtual.ProtectedStaticValueTypeField);

            duckAbstract.ProtectedStaticValueTypeField = 50;
            Assert.Equal(50, duckInterface.ProtectedStaticValueTypeField);
            Assert.Equal(50, duckAbstract.ProtectedStaticValueTypeField);
            Assert.Equal(50, duckVirtual.ProtectedStaticValueTypeField);

            duckVirtual.ProtectedStaticValueTypeField = 60;
            Assert.Equal(60, duckInterface.ProtectedStaticValueTypeField);
            Assert.Equal(60, duckAbstract.ProtectedStaticValueTypeField);
            Assert.Equal(60, duckVirtual.ProtectedStaticValueTypeField);

            // *

            Assert.Equal(23, duckInterface.PrivateStaticValueTypeField);
            Assert.Equal(23, duckAbstract.PrivateStaticValueTypeField);
            Assert.Equal(23, duckVirtual.PrivateStaticValueTypeField);

            duckInterface.PrivateStaticValueTypeField = 42;
            Assert.Equal(42, duckInterface.PrivateStaticValueTypeField);
            Assert.Equal(42, duckAbstract.PrivateStaticValueTypeField);
            Assert.Equal(42, duckVirtual.PrivateStaticValueTypeField);

            duckAbstract.PrivateStaticValueTypeField = 50;
            Assert.Equal(50, duckInterface.PrivateStaticValueTypeField);
            Assert.Equal(50, duckAbstract.PrivateStaticValueTypeField);
            Assert.Equal(50, duckVirtual.PrivateStaticValueTypeField);

            duckVirtual.PrivateStaticValueTypeField = 60;
            Assert.Equal(60, duckInterface.PrivateStaticValueTypeField);
            Assert.Equal(60, duckAbstract.PrivateStaticValueTypeField);
            Assert.Equal(60, duckVirtual.PrivateStaticValueTypeField);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void ReadonlyFields(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // *
            Assert.Equal(30, duckInterface.PublicReadonlyValueTypeField);
            Assert.Equal(30, duckAbstract.PublicReadonlyValueTypeField);
            Assert.Equal(30, duckVirtual.PublicReadonlyValueTypeField);

            // *
            Assert.Equal(31, duckInterface.InternalReadonlyValueTypeField);
            Assert.Equal(31, duckAbstract.InternalReadonlyValueTypeField);
            Assert.Equal(31, duckVirtual.InternalReadonlyValueTypeField);

            // *
            Assert.Equal(32, duckInterface.ProtectedReadonlyValueTypeField);
            Assert.Equal(32, duckAbstract.ProtectedReadonlyValueTypeField);
            Assert.Equal(32, duckVirtual.ProtectedReadonlyValueTypeField);

            // *
            Assert.Equal(33, duckInterface.PrivateReadonlyValueTypeField);
            Assert.Equal(33, duckAbstract.PrivateReadonlyValueTypeField);
            Assert.Equal(33, duckVirtual.PrivateReadonlyValueTypeField);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Fields(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Equal(40, duckInterface.PublicValueTypeField);
            Assert.Equal(40, duckAbstract.PublicValueTypeField);
            Assert.Equal(40, duckVirtual.PublicValueTypeField);

            duckInterface.PublicValueTypeField = 42;
            Assert.Equal(42, duckInterface.PublicValueTypeField);
            Assert.Equal(42, duckAbstract.PublicValueTypeField);
            Assert.Equal(42, duckVirtual.PublicValueTypeField);

            duckAbstract.PublicValueTypeField = 50;
            Assert.Equal(50, duckInterface.PublicValueTypeField);
            Assert.Equal(50, duckAbstract.PublicValueTypeField);
            Assert.Equal(50, duckVirtual.PublicValueTypeField);

            duckVirtual.PublicValueTypeField = 60;
            Assert.Equal(60, duckInterface.PublicValueTypeField);
            Assert.Equal(60, duckAbstract.PublicValueTypeField);
            Assert.Equal(60, duckVirtual.PublicValueTypeField);

            // *

            Assert.Equal(41, duckInterface.InternalValueTypeField);
            Assert.Equal(41, duckAbstract.InternalValueTypeField);
            Assert.Equal(41, duckVirtual.InternalValueTypeField);

            duckInterface.InternalValueTypeField = 42;
            Assert.Equal(42, duckInterface.InternalValueTypeField);
            Assert.Equal(42, duckAbstract.InternalValueTypeField);
            Assert.Equal(42, duckVirtual.InternalValueTypeField);

            duckAbstract.InternalValueTypeField = 50;
            Assert.Equal(50, duckInterface.InternalValueTypeField);
            Assert.Equal(50, duckAbstract.InternalValueTypeField);
            Assert.Equal(50, duckVirtual.InternalValueTypeField);

            duckVirtual.InternalValueTypeField = 60;
            Assert.Equal(60, duckInterface.InternalValueTypeField);
            Assert.Equal(60, duckAbstract.InternalValueTypeField);
            Assert.Equal(60, duckVirtual.InternalValueTypeField);

            // *

            Assert.Equal(42, duckInterface.ProtectedValueTypeField);
            Assert.Equal(42, duckAbstract.ProtectedValueTypeField);
            Assert.Equal(42, duckVirtual.ProtectedValueTypeField);

            duckInterface.ProtectedValueTypeField = 45;
            Assert.Equal(45, duckInterface.ProtectedValueTypeField);
            Assert.Equal(45, duckAbstract.ProtectedValueTypeField);
            Assert.Equal(45, duckVirtual.ProtectedValueTypeField);

            duckAbstract.ProtectedValueTypeField = 50;
            Assert.Equal(50, duckInterface.ProtectedValueTypeField);
            Assert.Equal(50, duckAbstract.ProtectedValueTypeField);
            Assert.Equal(50, duckVirtual.ProtectedValueTypeField);

            duckVirtual.ProtectedValueTypeField = 60;
            Assert.Equal(60, duckInterface.ProtectedValueTypeField);
            Assert.Equal(60, duckAbstract.ProtectedValueTypeField);
            Assert.Equal(60, duckVirtual.ProtectedValueTypeField);

            // *

            Assert.Equal(43, duckInterface.PrivateValueTypeField);
            Assert.Equal(43, duckAbstract.PrivateValueTypeField);
            Assert.Equal(43, duckVirtual.PrivateValueTypeField);

            duckInterface.PrivateValueTypeField = 42;
            Assert.Equal(42, duckInterface.PrivateValueTypeField);
            Assert.Equal(42, duckAbstract.PrivateValueTypeField);
            Assert.Equal(42, duckVirtual.PrivateValueTypeField);

            duckAbstract.PrivateValueTypeField = 50;
            Assert.Equal(50, duckInterface.PrivateValueTypeField);
            Assert.Equal(50, duckAbstract.PrivateValueTypeField);
            Assert.Equal(50, duckVirtual.PrivateValueTypeField);

            duckVirtual.PrivateValueTypeField = 60;
            Assert.Equal(60, duckInterface.PrivateValueTypeField);
            Assert.Equal(60, duckAbstract.PrivateValueTypeField);
            Assert.Equal(60, duckVirtual.PrivateValueTypeField);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void NullableOfKnown(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Null(duckInterface.PublicStaticNullableIntField);
            Assert.Null(duckAbstract.PublicStaticNullableIntField);
            Assert.Null(duckVirtual.PublicStaticNullableIntField);

            duckInterface.PublicStaticNullableIntField = 42;
            Assert.Equal(42, duckInterface.PublicStaticNullableIntField);
            Assert.Equal(42, duckAbstract.PublicStaticNullableIntField);
            Assert.Equal(42, duckVirtual.PublicStaticNullableIntField);

            duckAbstract.PublicStaticNullableIntField = 50;
            Assert.Equal(50, duckInterface.PublicStaticNullableIntField);
            Assert.Equal(50, duckAbstract.PublicStaticNullableIntField);
            Assert.Equal(50, duckVirtual.PublicStaticNullableIntField);

            duckVirtual.PublicStaticNullableIntField = null;
            Assert.Null(duckInterface.PublicStaticNullableIntField);
            Assert.Null(duckAbstract.PublicStaticNullableIntField);
            Assert.Null(duckVirtual.PublicStaticNullableIntField);

            // *

            Assert.Null(duckInterface.PrivateStaticNullableIntField);
            Assert.Null(duckAbstract.PrivateStaticNullableIntField);
            Assert.Null(duckVirtual.PrivateStaticNullableIntField);

            duckInterface.PrivateStaticNullableIntField = 42;
            Assert.Equal(42, duckInterface.PrivateStaticNullableIntField);
            Assert.Equal(42, duckAbstract.PrivateStaticNullableIntField);
            Assert.Equal(42, duckVirtual.PrivateStaticNullableIntField);

            duckAbstract.PrivateStaticNullableIntField = 50;
            Assert.Equal(50, duckInterface.PrivateStaticNullableIntField);
            Assert.Equal(50, duckAbstract.PrivateStaticNullableIntField);
            Assert.Equal(50, duckVirtual.PrivateStaticNullableIntField);

            duckVirtual.PrivateStaticNullableIntField = null;
            Assert.Null(duckInterface.PrivateStaticNullableIntField);
            Assert.Null(duckAbstract.PrivateStaticNullableIntField);
            Assert.Null(duckVirtual.PrivateStaticNullableIntField);

            // *

            Assert.Null(duckInterface.PublicNullableIntField);
            Assert.Null(duckAbstract.PublicNullableIntField);
            Assert.Null(duckVirtual.PublicNullableIntField);

            duckInterface.PublicNullableIntField = 42;
            Assert.Equal(42, duckInterface.PublicNullableIntField);
            Assert.Equal(42, duckAbstract.PublicNullableIntField);
            Assert.Equal(42, duckVirtual.PublicNullableIntField);

            duckAbstract.PublicNullableIntField = 50;
            Assert.Equal(50, duckInterface.PublicNullableIntField);
            Assert.Equal(50, duckAbstract.PublicNullableIntField);
            Assert.Equal(50, duckVirtual.PublicNullableIntField);

            duckVirtual.PublicNullableIntField = null;
            Assert.Null(duckInterface.PublicNullableIntField);
            Assert.Null(duckAbstract.PublicNullableIntField);
            Assert.Null(duckVirtual.PublicNullableIntField);

            // *

            Assert.Null(duckInterface.PrivateNullableIntField);
            Assert.Null(duckAbstract.PrivateNullableIntField);
            Assert.Null(duckVirtual.PrivateNullableIntField);

            duckInterface.PrivateNullableIntField = 42;
            Assert.Equal(42, duckInterface.PrivateNullableIntField);
            Assert.Equal(42, duckAbstract.PrivateNullableIntField);
            Assert.Equal(42, duckVirtual.PrivateNullableIntField);

            duckAbstract.PrivateNullableIntField = 50;
            Assert.Equal(50, duckInterface.PrivateNullableIntField);
            Assert.Equal(50, duckAbstract.PrivateNullableIntField);
            Assert.Equal(50, duckVirtual.PrivateNullableIntField);

            duckVirtual.PrivateNullableIntField = null;
            Assert.Null(duckInterface.PrivateNullableIntField);
            Assert.Null(duckAbstract.PrivateNullableIntField);
            Assert.Null(duckVirtual.PrivateNullableIntField);
        }
    }
}
