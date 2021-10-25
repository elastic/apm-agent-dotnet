// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ReferenceTypeFieldTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using Elastic.Apm.Profiler.Managed.DuckTyping;
using Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Fields.ReferenceType.ProxiesDefinitions;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Fields.ReferenceType
{
    public class ReferenceTypeFieldTests
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
            Assert.Equal("10", duckInterface.PublicStaticReadonlyReferenceTypeField);
            Assert.Equal("10", duckAbstract.PublicStaticReadonlyReferenceTypeField);
            Assert.Equal("10", duckVirtual.PublicStaticReadonlyReferenceTypeField);

            // *
            Assert.Equal("11", duckInterface.InternalStaticReadonlyReferenceTypeField);
            Assert.Equal("11", duckAbstract.InternalStaticReadonlyReferenceTypeField);
            Assert.Equal("11", duckVirtual.InternalStaticReadonlyReferenceTypeField);

            // *
            Assert.Equal("12", duckInterface.ProtectedStaticReadonlyReferenceTypeField);
            Assert.Equal("12", duckAbstract.ProtectedStaticReadonlyReferenceTypeField);
            Assert.Equal("12", duckVirtual.ProtectedStaticReadonlyReferenceTypeField);

            // *
            Assert.Equal("13", duckInterface.PrivateStaticReadonlyReferenceTypeField);
            Assert.Equal("13", duckAbstract.PrivateStaticReadonlyReferenceTypeField);
            Assert.Equal("13", duckVirtual.PrivateStaticReadonlyReferenceTypeField);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StaticFields(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Equal("20", duckInterface.PublicStaticReferenceTypeField);
            Assert.Equal("20", duckAbstract.PublicStaticReferenceTypeField);
            Assert.Equal("20", duckVirtual.PublicStaticReferenceTypeField);

            duckInterface.PublicStaticReferenceTypeField = "42";
            Assert.Equal("42", duckInterface.PublicStaticReferenceTypeField);
            Assert.Equal("42", duckAbstract.PublicStaticReferenceTypeField);
            Assert.Equal("42", duckVirtual.PublicStaticReferenceTypeField);

            duckAbstract.PublicStaticReferenceTypeField = "50";
            Assert.Equal("50", duckInterface.PublicStaticReferenceTypeField);
            Assert.Equal("50", duckAbstract.PublicStaticReferenceTypeField);
            Assert.Equal("50", duckVirtual.PublicStaticReferenceTypeField);

            duckVirtual.PublicStaticReferenceTypeField = "60";
            Assert.Equal("60", duckInterface.PublicStaticReferenceTypeField);
            Assert.Equal("60", duckAbstract.PublicStaticReferenceTypeField);
            Assert.Equal("60", duckVirtual.PublicStaticReferenceTypeField);

            // *

            Assert.Equal("21", duckInterface.InternalStaticReferenceTypeField);
            Assert.Equal("21", duckAbstract.InternalStaticReferenceTypeField);
            Assert.Equal("21", duckVirtual.InternalStaticReferenceTypeField);

            duckInterface.InternalStaticReferenceTypeField = "42";
            Assert.Equal("42", duckInterface.InternalStaticReferenceTypeField);
            Assert.Equal("42", duckAbstract.InternalStaticReferenceTypeField);
            Assert.Equal("42", duckVirtual.InternalStaticReferenceTypeField);

            duckAbstract.InternalStaticReferenceTypeField = "50";
            Assert.Equal("50", duckInterface.InternalStaticReferenceTypeField);
            Assert.Equal("50", duckAbstract.InternalStaticReferenceTypeField);
            Assert.Equal("50", duckVirtual.InternalStaticReferenceTypeField);

            duckVirtual.InternalStaticReferenceTypeField = "60";
            Assert.Equal("60", duckInterface.InternalStaticReferenceTypeField);
            Assert.Equal("60", duckAbstract.InternalStaticReferenceTypeField);
            Assert.Equal("60", duckVirtual.InternalStaticReferenceTypeField);

            // *

            Assert.Equal("22", duckInterface.ProtectedStaticReferenceTypeField);
            Assert.Equal("22", duckAbstract.ProtectedStaticReferenceTypeField);
            Assert.Equal("22", duckVirtual.ProtectedStaticReferenceTypeField);

            duckInterface.ProtectedStaticReferenceTypeField = "42";
            Assert.Equal("42", duckInterface.ProtectedStaticReferenceTypeField);
            Assert.Equal("42", duckAbstract.ProtectedStaticReferenceTypeField);
            Assert.Equal("42", duckVirtual.ProtectedStaticReferenceTypeField);

            duckAbstract.ProtectedStaticReferenceTypeField = "50";
            Assert.Equal("50", duckInterface.ProtectedStaticReferenceTypeField);
            Assert.Equal("50", duckAbstract.ProtectedStaticReferenceTypeField);
            Assert.Equal("50", duckVirtual.ProtectedStaticReferenceTypeField);

            duckVirtual.ProtectedStaticReferenceTypeField = "60";
            Assert.Equal("60", duckInterface.ProtectedStaticReferenceTypeField);
            Assert.Equal("60", duckAbstract.ProtectedStaticReferenceTypeField);
            Assert.Equal("60", duckVirtual.ProtectedStaticReferenceTypeField);

            // *

            Assert.Equal("23", duckInterface.PrivateStaticReferenceTypeField);
            Assert.Equal("23", duckAbstract.PrivateStaticReferenceTypeField);
            Assert.Equal("23", duckVirtual.PrivateStaticReferenceTypeField);

            duckInterface.PrivateStaticReferenceTypeField = "42";
            Assert.Equal("42", duckInterface.PrivateStaticReferenceTypeField);
            Assert.Equal("42", duckAbstract.PrivateStaticReferenceTypeField);
            Assert.Equal("42", duckVirtual.PrivateStaticReferenceTypeField);

            duckAbstract.PrivateStaticReferenceTypeField = "50";
            Assert.Equal("50", duckInterface.PrivateStaticReferenceTypeField);
            Assert.Equal("50", duckAbstract.PrivateStaticReferenceTypeField);
            Assert.Equal("50", duckVirtual.PrivateStaticReferenceTypeField);

            duckVirtual.PrivateStaticReferenceTypeField = "60";
            Assert.Equal("60", duckInterface.PrivateStaticReferenceTypeField);
            Assert.Equal("60", duckAbstract.PrivateStaticReferenceTypeField);
            Assert.Equal("60", duckVirtual.PrivateStaticReferenceTypeField);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void ReadonlyFields(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // *
            Assert.Equal("30", duckInterface.PublicReadonlyReferenceTypeField);
            Assert.Equal("30", duckAbstract.PublicReadonlyReferenceTypeField);
            Assert.Equal("30", duckVirtual.PublicReadonlyReferenceTypeField);

            // *
            Assert.Equal("31", duckInterface.InternalReadonlyReferenceTypeField);
            Assert.Equal("31", duckAbstract.InternalReadonlyReferenceTypeField);
            Assert.Equal("31", duckVirtual.InternalReadonlyReferenceTypeField);

            // *
            Assert.Equal("32", duckInterface.ProtectedReadonlyReferenceTypeField);
            Assert.Equal("32", duckAbstract.ProtectedReadonlyReferenceTypeField);
            Assert.Equal("32", duckVirtual.ProtectedReadonlyReferenceTypeField);

            // *
            Assert.Equal("33", duckInterface.PrivateReadonlyReferenceTypeField);
            Assert.Equal("33", duckAbstract.PrivateReadonlyReferenceTypeField);
            Assert.Equal("33", duckVirtual.PrivateReadonlyReferenceTypeField);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Fields(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Equal("40", duckInterface.PublicReferenceTypeField);
            Assert.Equal("40", duckAbstract.PublicReferenceTypeField);
            Assert.Equal("40", duckVirtual.PublicReferenceTypeField);

            duckInterface.PublicReferenceTypeField = "42";
            Assert.Equal("42", duckInterface.PublicReferenceTypeField);
            Assert.Equal("42", duckAbstract.PublicReferenceTypeField);
            Assert.Equal("42", duckVirtual.PublicReferenceTypeField);

            duckAbstract.PublicReferenceTypeField = "50";
            Assert.Equal("50", duckInterface.PublicReferenceTypeField);
            Assert.Equal("50", duckAbstract.PublicReferenceTypeField);
            Assert.Equal("50", duckVirtual.PublicReferenceTypeField);

            duckVirtual.PublicReferenceTypeField = "60";
            Assert.Equal("60", duckInterface.PublicReferenceTypeField);
            Assert.Equal("60", duckAbstract.PublicReferenceTypeField);
            Assert.Equal("60", duckVirtual.PublicReferenceTypeField);

            // *

            Assert.Equal("41", duckInterface.InternalReferenceTypeField);
            Assert.Equal("41", duckAbstract.InternalReferenceTypeField);
            Assert.Equal("41", duckVirtual.InternalReferenceTypeField);

            duckInterface.InternalReferenceTypeField = "42";
            Assert.Equal("42", duckInterface.InternalReferenceTypeField);
            Assert.Equal("42", duckAbstract.InternalReferenceTypeField);
            Assert.Equal("42", duckVirtual.InternalReferenceTypeField);

            duckAbstract.InternalReferenceTypeField = "50";
            Assert.Equal("50", duckInterface.InternalReferenceTypeField);
            Assert.Equal("50", duckAbstract.InternalReferenceTypeField);
            Assert.Equal("50", duckVirtual.InternalReferenceTypeField);

            duckVirtual.InternalReferenceTypeField = "60";
            Assert.Equal("60", duckInterface.InternalReferenceTypeField);
            Assert.Equal("60", duckAbstract.InternalReferenceTypeField);
            Assert.Equal("60", duckVirtual.InternalReferenceTypeField);

            // *

            Assert.Equal("42", duckInterface.ProtectedReferenceTypeField);
            Assert.Equal("42", duckAbstract.ProtectedReferenceTypeField);
            Assert.Equal("42", duckVirtual.ProtectedReferenceTypeField);

            duckInterface.ProtectedReferenceTypeField = "45";
            Assert.Equal("45", duckInterface.ProtectedReferenceTypeField);
            Assert.Equal("45", duckAbstract.ProtectedReferenceTypeField);
            Assert.Equal("45", duckVirtual.ProtectedReferenceTypeField);

            duckAbstract.ProtectedReferenceTypeField = "50";
            Assert.Equal("50", duckInterface.ProtectedReferenceTypeField);
            Assert.Equal("50", duckAbstract.ProtectedReferenceTypeField);
            Assert.Equal("50", duckVirtual.ProtectedReferenceTypeField);

            duckVirtual.ProtectedReferenceTypeField = "60";
            Assert.Equal("60", duckInterface.ProtectedReferenceTypeField);
            Assert.Equal("60", duckAbstract.ProtectedReferenceTypeField);
            Assert.Equal("60", duckVirtual.ProtectedReferenceTypeField);

            // *

            Assert.Equal("43", duckInterface.PrivateReferenceTypeField);
            Assert.Equal("43", duckAbstract.PrivateReferenceTypeField);
            Assert.Equal("43", duckVirtual.PrivateReferenceTypeField);

            duckInterface.PrivateReferenceTypeField = "42";
            Assert.Equal("42", duckInterface.PrivateReferenceTypeField);
            Assert.Equal("42", duckAbstract.PrivateReferenceTypeField);
            Assert.Equal("42", duckVirtual.PrivateReferenceTypeField);

            duckAbstract.PrivateReferenceTypeField = "50";
            Assert.Equal("50", duckInterface.PrivateReferenceTypeField);
            Assert.Equal("50", duckAbstract.PrivateReferenceTypeField);
            Assert.Equal("50", duckVirtual.PrivateReferenceTypeField);

            duckVirtual.PrivateReferenceTypeField = "60";
            Assert.Equal("60", duckInterface.PrivateReferenceTypeField);
            Assert.Equal("60", duckAbstract.PrivateReferenceTypeField);
            Assert.Equal("60", duckVirtual.PrivateReferenceTypeField);
        }
    }
}
