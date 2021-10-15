// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ReferenceTypePropertyTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using Elastic.Apm.Profiler.Managed.DuckTyping;
using Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.ReferenceType.ProxiesDefinitions;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.ReferenceType
{
    public class ReferenceTypePropertyTests
    {
        public static IEnumerable<object[]> Data()
        {
            return new[]
            {
                new object[] { ObscureObject.GetPropertyPublicObject() },
                new object[] { ObscureObject.GetPropertyInternalObject() },
                new object[] { ObscureObject.GetPropertyPrivateObject() },
            };
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StaticGetOnlyPropertyException(object obscureObject)
        {
            Assert.Throws<DuckTypePropertyCantBeWrittenException>(() =>
            {
                obscureObject.DuckCast<IObscureStaticErrorDuckType>();
            });
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void GetOnlyPropertyException(object obscureObject)
        {
            Assert.Throws<DuckTypePropertyCantBeWrittenException>(() =>
            {
                obscureObject.DuckCast<IObscureErrorDuckType>();
            });
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StaticGetOnlyProperties(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // *
            Assert.Equal("10", duckInterface.PublicStaticGetReferenceType);
            Assert.Equal("10", duckAbstract.PublicStaticGetReferenceType);
            Assert.Equal("10", duckVirtual.PublicStaticGetReferenceType);

            // *
            Assert.Equal("11", duckInterface.InternalStaticGetReferenceType);
            Assert.Equal("11", duckAbstract.InternalStaticGetReferenceType);
            Assert.Equal("11", duckVirtual.InternalStaticGetReferenceType);

            // *
            Assert.Equal("12", duckInterface.ProtectedStaticGetReferenceType);
            Assert.Equal("12", duckAbstract.ProtectedStaticGetReferenceType);
            Assert.Equal("12", duckVirtual.ProtectedStaticGetReferenceType);

            // *
            Assert.Equal("13", duckInterface.PrivateStaticGetReferenceType);
            Assert.Equal("13", duckAbstract.PrivateStaticGetReferenceType);
            Assert.Equal("13", duckVirtual.PrivateStaticGetReferenceType);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StaticProperties(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Equal("20", duckInterface.PublicStaticGetSetReferenceType);
            Assert.Equal("20", duckAbstract.PublicStaticGetSetReferenceType);
            Assert.Equal("20", duckVirtual.PublicStaticGetSetReferenceType);

            duckInterface.PublicStaticGetSetReferenceType = "42";
            Assert.Equal("42", duckInterface.PublicStaticGetSetReferenceType);
            Assert.Equal("42", duckAbstract.PublicStaticGetSetReferenceType);
            Assert.Equal("42", duckVirtual.PublicStaticGetSetReferenceType);

            duckAbstract.PublicStaticGetSetReferenceType = "50";
            Assert.Equal("50", duckInterface.PublicStaticGetSetReferenceType);
            Assert.Equal("50", duckAbstract.PublicStaticGetSetReferenceType);
            Assert.Equal("50", duckVirtual.PublicStaticGetSetReferenceType);

            duckVirtual.PublicStaticGetSetReferenceType = "60";
            Assert.Equal("60", duckInterface.PublicStaticGetSetReferenceType);
            Assert.Equal("60", duckAbstract.PublicStaticGetSetReferenceType);
            Assert.Equal("60", duckVirtual.PublicStaticGetSetReferenceType);

            duckInterface.PublicStaticGetSetReferenceType = "20";

            // *

            Assert.Equal("21", duckInterface.InternalStaticGetSetReferenceType);
            Assert.Equal("21", duckAbstract.InternalStaticGetSetReferenceType);
            Assert.Equal("21", duckVirtual.InternalStaticGetSetReferenceType);

            duckInterface.InternalStaticGetSetReferenceType = "42";
            Assert.Equal("42", duckInterface.InternalStaticGetSetReferenceType);
            Assert.Equal("42", duckAbstract.InternalStaticGetSetReferenceType);
            Assert.Equal("42", duckVirtual.InternalStaticGetSetReferenceType);

            duckAbstract.InternalStaticGetSetReferenceType = "50";
            Assert.Equal("50", duckInterface.InternalStaticGetSetReferenceType);
            Assert.Equal("50", duckAbstract.InternalStaticGetSetReferenceType);
            Assert.Equal("50", duckVirtual.InternalStaticGetSetReferenceType);

            duckVirtual.InternalStaticGetSetReferenceType = "60";
            Assert.Equal("60", duckInterface.InternalStaticGetSetReferenceType);
            Assert.Equal("60", duckAbstract.InternalStaticGetSetReferenceType);
            Assert.Equal("60", duckVirtual.InternalStaticGetSetReferenceType);

            duckInterface.InternalStaticGetSetReferenceType = "21";

            // *

            Assert.Equal("22", duckInterface.ProtectedStaticGetSetReferenceType);
            Assert.Equal("22", duckAbstract.ProtectedStaticGetSetReferenceType);
            Assert.Equal("22", duckVirtual.ProtectedStaticGetSetReferenceType);

            duckInterface.ProtectedStaticGetSetReferenceType = "42";
            Assert.Equal("42", duckInterface.ProtectedStaticGetSetReferenceType);
            Assert.Equal("42", duckAbstract.ProtectedStaticGetSetReferenceType);
            Assert.Equal("42", duckVirtual.ProtectedStaticGetSetReferenceType);

            duckAbstract.ProtectedStaticGetSetReferenceType = "50";
            Assert.Equal("50", duckInterface.ProtectedStaticGetSetReferenceType);
            Assert.Equal("50", duckAbstract.ProtectedStaticGetSetReferenceType);
            Assert.Equal("50", duckVirtual.ProtectedStaticGetSetReferenceType);

            duckVirtual.ProtectedStaticGetSetReferenceType = "60";
            Assert.Equal("60", duckInterface.ProtectedStaticGetSetReferenceType);
            Assert.Equal("60", duckAbstract.ProtectedStaticGetSetReferenceType);
            Assert.Equal("60", duckVirtual.ProtectedStaticGetSetReferenceType);

            duckInterface.ProtectedStaticGetSetReferenceType = "22";

            // *

            Assert.Equal("23", duckInterface.PrivateStaticGetSetReferenceType);
            Assert.Equal("23", duckAbstract.PrivateStaticGetSetReferenceType);
            Assert.Equal("23", duckVirtual.PrivateStaticGetSetReferenceType);

            duckInterface.PrivateStaticGetSetReferenceType = "42";
            Assert.Equal("42", duckInterface.PrivateStaticGetSetReferenceType);
            Assert.Equal("42", duckAbstract.PrivateStaticGetSetReferenceType);
            Assert.Equal("42", duckVirtual.PrivateStaticGetSetReferenceType);

            duckAbstract.PrivateStaticGetSetReferenceType = "50";
            Assert.Equal("50", duckInterface.PrivateStaticGetSetReferenceType);
            Assert.Equal("50", duckAbstract.PrivateStaticGetSetReferenceType);
            Assert.Equal("50", duckVirtual.PrivateStaticGetSetReferenceType);

            duckVirtual.PrivateStaticGetSetReferenceType = "60";
            Assert.Equal("60", duckInterface.PrivateStaticGetSetReferenceType);
            Assert.Equal("60", duckAbstract.PrivateStaticGetSetReferenceType);
            Assert.Equal("60", duckVirtual.PrivateStaticGetSetReferenceType);

            duckInterface.PrivateStaticGetSetReferenceType = "23";
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void GetOnlyProperties(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // *
            Assert.Equal("30", duckInterface.PublicGetReferenceType);
            Assert.Equal("30", duckAbstract.PublicGetReferenceType);
            Assert.Equal("30", duckVirtual.PublicGetReferenceType);

            // *
            Assert.Equal("31", duckInterface.InternalGetReferenceType);
            Assert.Equal("31", duckAbstract.InternalGetReferenceType);
            Assert.Equal("31", duckVirtual.InternalGetReferenceType);

            // *
            Assert.Equal("32", duckInterface.ProtectedGetReferenceType);
            Assert.Equal("32", duckAbstract.ProtectedGetReferenceType);
            Assert.Equal("32", duckVirtual.ProtectedGetReferenceType);

            // *
            Assert.Equal("33", duckInterface.PrivateGetReferenceType);
            Assert.Equal("33", duckAbstract.PrivateGetReferenceType);
            Assert.Equal("33", duckVirtual.PrivateGetReferenceType);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Properties(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Equal("40", duckInterface.PublicGetSetReferenceType);
            Assert.Equal("40", duckAbstract.PublicGetSetReferenceType);
            Assert.Equal("40", duckVirtual.PublicGetSetReferenceType);

            duckInterface.PublicGetSetReferenceType = "42";
            Assert.Equal("42", duckInterface.PublicGetSetReferenceType);
            Assert.Equal("42", duckAbstract.PublicGetSetReferenceType);
            Assert.Equal("42", duckVirtual.PublicGetSetReferenceType);

            duckAbstract.PublicGetSetReferenceType = "50";
            Assert.Equal("50", duckInterface.PublicGetSetReferenceType);
            Assert.Equal("50", duckAbstract.PublicGetSetReferenceType);
            Assert.Equal("50", duckVirtual.PublicGetSetReferenceType);

            duckVirtual.PublicGetSetReferenceType = "60";
            Assert.Equal("60", duckInterface.PublicGetSetReferenceType);
            Assert.Equal("60", duckAbstract.PublicGetSetReferenceType);
            Assert.Equal("60", duckVirtual.PublicGetSetReferenceType);

            duckInterface.PublicGetSetReferenceType = "40";

            // *

            Assert.Equal("41", duckInterface.InternalGetSetReferenceType);
            Assert.Equal("41", duckAbstract.InternalGetSetReferenceType);
            Assert.Equal("41", duckVirtual.InternalGetSetReferenceType);

            duckInterface.InternalGetSetReferenceType = "42";
            Assert.Equal("42", duckInterface.InternalGetSetReferenceType);
            Assert.Equal("42", duckAbstract.InternalGetSetReferenceType);
            Assert.Equal("42", duckVirtual.InternalGetSetReferenceType);

            duckAbstract.InternalGetSetReferenceType = "50";
            Assert.Equal("50", duckInterface.InternalGetSetReferenceType);
            Assert.Equal("50", duckAbstract.InternalGetSetReferenceType);
            Assert.Equal("50", duckVirtual.InternalGetSetReferenceType);

            duckVirtual.InternalGetSetReferenceType = "60";
            Assert.Equal("60", duckInterface.InternalGetSetReferenceType);
            Assert.Equal("60", duckAbstract.InternalGetSetReferenceType);
            Assert.Equal("60", duckVirtual.InternalGetSetReferenceType);

            duckInterface.InternalGetSetReferenceType = "41";

            // *

            Assert.Equal("42", duckInterface.ProtectedGetSetReferenceType);
            Assert.Equal("42", duckAbstract.ProtectedGetSetReferenceType);
            Assert.Equal("42", duckVirtual.ProtectedGetSetReferenceType);

            duckInterface.ProtectedGetSetReferenceType = "45";
            Assert.Equal("45", duckInterface.ProtectedGetSetReferenceType);
            Assert.Equal("45", duckAbstract.ProtectedGetSetReferenceType);
            Assert.Equal("45", duckVirtual.ProtectedGetSetReferenceType);

            duckAbstract.ProtectedGetSetReferenceType = "50";
            Assert.Equal("50", duckInterface.ProtectedGetSetReferenceType);
            Assert.Equal("50", duckAbstract.ProtectedGetSetReferenceType);
            Assert.Equal("50", duckVirtual.ProtectedGetSetReferenceType);

            duckVirtual.ProtectedGetSetReferenceType = "60";
            Assert.Equal("60", duckInterface.ProtectedGetSetReferenceType);
            Assert.Equal("60", duckAbstract.ProtectedGetSetReferenceType);
            Assert.Equal("60", duckVirtual.ProtectedGetSetReferenceType);

            duckInterface.ProtectedGetSetReferenceType = "42";

            // *

            Assert.Equal("43", duckInterface.PrivateGetSetReferenceType);
            Assert.Equal("43", duckAbstract.PrivateGetSetReferenceType);
            Assert.Equal("43", duckVirtual.PrivateGetSetReferenceType);

            duckInterface.PrivateGetSetReferenceType = "42";
            Assert.Equal("42", duckInterface.PrivateGetSetReferenceType);
            Assert.Equal("42", duckAbstract.PrivateGetSetReferenceType);
            Assert.Equal("42", duckVirtual.PrivateGetSetReferenceType);

            duckAbstract.PrivateGetSetReferenceType = "50";
            Assert.Equal("50", duckInterface.PrivateGetSetReferenceType);
            Assert.Equal("50", duckAbstract.PrivateGetSetReferenceType);
            Assert.Equal("50", duckVirtual.PrivateGetSetReferenceType);

            duckVirtual.PrivateGetSetReferenceType = "60";
            Assert.Equal("60", duckInterface.PrivateGetSetReferenceType);
            Assert.Equal("60", duckAbstract.PrivateGetSetReferenceType);
            Assert.Equal("60", duckVirtual.PrivateGetSetReferenceType);

            duckInterface.PrivateGetSetReferenceType = "43";
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Indexer(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            duckInterface["1"] = "100";
            Assert.Equal("100", duckInterface["1"]);
            Assert.Equal("100", duckAbstract["1"]);
            Assert.Equal("100", duckVirtual["1"]);

            duckAbstract["2"] = "200";
            Assert.Equal("200", duckInterface["2"]);
            Assert.Equal("200", duckAbstract["2"]);
            Assert.Equal("200", duckVirtual["2"]);

            duckVirtual["3"] = "300";
            Assert.Equal("300", duckInterface["3"]);
            Assert.Equal("300", duckAbstract["3"]);
            Assert.Equal("300", duckVirtual["3"]);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StructCopy(object obscureObject)
        {
            var duckStructCopy = obscureObject.DuckCast<ObscureDuckTypeStruct>();

            Assert.Equal("10", duckStructCopy.PublicStaticGetReferenceType);
            Assert.Equal("11", duckStructCopy.InternalStaticGetReferenceType);
            Assert.Equal("12", duckStructCopy.ProtectedStaticGetReferenceType);
            Assert.Equal("13", duckStructCopy.PrivateStaticGetReferenceType);

            Assert.Equal("20", duckStructCopy.PublicStaticGetSetReferenceType);
            Assert.Equal("21", duckStructCopy.InternalStaticGetSetReferenceType);
            Assert.Equal("22", duckStructCopy.ProtectedStaticGetSetReferenceType);
            Assert.Equal("23", duckStructCopy.PrivateStaticGetSetReferenceType);

            Assert.Equal("30", duckStructCopy.PublicGetReferenceType);
            Assert.Equal("31", duckStructCopy.InternalGetReferenceType);
            Assert.Equal("32", duckStructCopy.ProtectedGetReferenceType);
            Assert.Equal("33", duckStructCopy.PrivateGetReferenceType);

            Assert.Equal("40", duckStructCopy.PublicGetSetReferenceType);
            Assert.Equal("41", duckStructCopy.InternalGetSetReferenceType);
            Assert.Equal("42", duckStructCopy.ProtectedGetSetReferenceType);
            Assert.Equal("43", duckStructCopy.PrivateGetSetReferenceType);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void UnionTest(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IDuckTypeUnion>();

            Assert.Equal("40", duckInterface.PublicGetSetReferenceType);

            duckInterface.PublicGetSetReferenceType = "42";
            Assert.Equal("42", duckInterface.PublicGetSetReferenceType);

            duckInterface.PublicGetSetReferenceType = "40";

            // *

            Assert.Equal("41", duckInterface.InternalGetSetReferenceType);

            duckInterface.InternalGetSetReferenceType = "42";
            Assert.Equal("42", duckInterface.InternalGetSetReferenceType);

            duckInterface.InternalGetSetReferenceType = "41";

            // *

            Assert.Equal("42", duckInterface.ProtectedGetSetReferenceType);

            duckInterface.ProtectedGetSetReferenceType = "45";
            Assert.Equal("45", duckInterface.ProtectedGetSetReferenceType);

            duckInterface.ProtectedGetSetReferenceType = "42";

            // *

            Assert.Equal("43", duckInterface.PrivateGetSetReferenceType);

            duckInterface.PrivateGetSetReferenceType = "42";
            Assert.Equal("42", duckInterface.PrivateGetSetReferenceType);

            duckInterface.PrivateGetSetReferenceType = "43";
        }
    }
}
