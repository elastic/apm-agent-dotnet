// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="TypeChainingPropertyTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using Elastic.Apm.Profiler.Managed.DuckTyping;
using Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.TypeChaining.ProxiesDefinitions;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.TypeChaining
{
    public class TypeChainingPropertyTests
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

            Assert.Equal(42, duckInterface.PublicStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckAbstract.PublicStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckVirtual.PublicStaticGetSelfType.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.PublicStaticGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.PublicStaticGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.PublicStaticGetSelfType).Instance);

            // *

            Assert.Equal(42, duckInterface.InternalStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckAbstract.InternalStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckVirtual.InternalStaticGetSelfType.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.InternalStaticGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.InternalStaticGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.InternalStaticGetSelfType).Instance);

            // *

            Assert.Equal(42, duckInterface.ProtectedStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckAbstract.ProtectedStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckVirtual.ProtectedStaticGetSelfType.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.ProtectedStaticGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.ProtectedStaticGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.ProtectedStaticGetSelfType).Instance);

            // *

            Assert.Equal(42, duckInterface.PrivateStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckAbstract.PrivateStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckVirtual.PrivateStaticGetSelfType.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.PrivateStaticGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.PrivateStaticGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.PrivateStaticGetSelfType).Instance);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StaticProperties(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            IDummyFieldObject newDummy = null;

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 42 }).DuckCast<IDummyFieldObject>();
            duckInterface.PublicStaticGetSetSelfType = newDummy;

            Assert.Equal(42, duckInterface.PublicStaticGetSetSelfType.MagicNumber);
            Assert.Equal(42, duckAbstract.PublicStaticGetSetSelfType.MagicNumber);
            Assert.Equal(42, duckVirtual.PublicStaticGetSetSelfType.MagicNumber);

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 52 }).DuckCast<IDummyFieldObject>();
            duckInterface.InternalStaticGetSetSelfType = newDummy;

            Assert.Equal(52, duckInterface.InternalStaticGetSetSelfType.MagicNumber);
            Assert.Equal(52, duckAbstract.InternalStaticGetSetSelfType.MagicNumber);
            Assert.Equal(52, duckVirtual.InternalStaticGetSetSelfType.MagicNumber);

            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 42 }).DuckCast<IDummyFieldObject>();
            duckInterface.InternalStaticGetSetSelfType = newDummy;

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 62 }).DuckCast<IDummyFieldObject>();
            duckAbstract.ProtectedStaticGetSetSelfType = newDummy;

            Assert.Equal(62, duckInterface.ProtectedStaticGetSetSelfType.MagicNumber);
            Assert.Equal(62, duckAbstract.ProtectedStaticGetSetSelfType.MagicNumber);
            Assert.Equal(62, duckVirtual.ProtectedStaticGetSetSelfType.MagicNumber);

            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 42 }).DuckCast<IDummyFieldObject>();
            duckAbstract.ProtectedStaticGetSetSelfType = newDummy;

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 72 }).DuckCast<IDummyFieldObject>();
            duckAbstract.PrivateStaticGetSetSelfType = newDummy;

            Assert.Equal(72, duckInterface.PrivateStaticGetSetSelfType.MagicNumber);
            Assert.Equal(72, duckAbstract.PrivateStaticGetSetSelfType.MagicNumber);
            Assert.Equal(72, duckVirtual.PrivateStaticGetSetSelfType.MagicNumber);

            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 42 }).DuckCast<IDummyFieldObject>();
            duckAbstract.PrivateStaticGetSetSelfType = newDummy;
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void GetOnlyProperties(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // *

            Assert.Equal(42, duckInterface.PublicGetSelfType.MagicNumber);
            Assert.Equal(42, duckAbstract.PublicGetSelfType.MagicNumber);
            Assert.Equal(42, duckVirtual.PublicGetSelfType.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.PublicGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.PublicGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.PublicGetSelfType).Instance);

            // *

            Assert.Equal(42, duckInterface.InternalGetSelfType.MagicNumber);
            Assert.Equal(42, duckAbstract.InternalGetSelfType.MagicNumber);
            Assert.Equal(42, duckVirtual.InternalGetSelfType.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.InternalGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.InternalGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.InternalGetSelfType).Instance);

            // *

            Assert.Equal(42, duckInterface.ProtectedGetSelfType.MagicNumber);
            Assert.Equal(42, duckAbstract.ProtectedGetSelfType.MagicNumber);
            Assert.Equal(42, duckVirtual.ProtectedGetSelfType.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.ProtectedGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.ProtectedGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.ProtectedGetSelfType).Instance);

            // *

            Assert.Equal(42, duckInterface.PrivateGetSelfType.MagicNumber);
            Assert.Equal(42, duckAbstract.PrivateGetSelfType.MagicNumber);
            Assert.Equal(42, duckVirtual.PrivateGetSelfType.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.PrivateGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.PrivateGetSelfType).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.PrivateGetSelfType).Instance);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Properties(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            IDummyFieldObject newDummy = null;

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 42 }).DuckCast<IDummyFieldObject>();
            duckInterface.PublicGetSetSelfType = newDummy;

            Assert.Equal(42, duckInterface.PublicGetSetSelfType.MagicNumber);
            Assert.Equal(42, duckAbstract.PublicGetSetSelfType.MagicNumber);
            Assert.Equal(42, duckVirtual.PublicGetSetSelfType.MagicNumber);

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 52 }).DuckCast<IDummyFieldObject>();
            duckInterface.InternalGetSetSelfType = newDummy;

            Assert.Equal(52, duckInterface.InternalGetSetSelfType.MagicNumber);
            Assert.Equal(52, duckAbstract.InternalGetSetSelfType.MagicNumber);
            Assert.Equal(52, duckVirtual.InternalGetSetSelfType.MagicNumber);

            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 42 }).DuckCast<IDummyFieldObject>();
            duckInterface.InternalGetSetSelfType = newDummy;

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 62 }).DuckCast<IDummyFieldObject>();
            duckInterface.ProtectedGetSetSelfType = newDummy;

            Assert.Equal(62, duckInterface.ProtectedGetSetSelfType.MagicNumber);
            Assert.Equal(62, duckAbstract.ProtectedGetSetSelfType.MagicNumber);
            Assert.Equal(62, duckVirtual.ProtectedGetSetSelfType.MagicNumber);

            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 42 }).DuckCast<IDummyFieldObject>();
            duckInterface.ProtectedGetSetSelfType = newDummy;

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 72 }).DuckCast<IDummyFieldObject>();
            duckInterface.PrivateGetSetSelfType = newDummy;

            Assert.Equal(72, duckInterface.PrivateGetSetSelfType.MagicNumber);
            Assert.Equal(72, duckAbstract.PrivateGetSetSelfType.MagicNumber);
            Assert.Equal(72, duckVirtual.PrivateGetSetSelfType.MagicNumber);

            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 42 }).DuckCast<IDummyFieldObject>();
            duckInterface.PrivateGetSetSelfType = newDummy;
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StructCopy(object obscureObject)
        {
            var duckStructCopy = obscureObject.DuckCast<ObscureDuckTypeStruct>();

            Assert.Equal(42, duckStructCopy.PublicStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.InternalStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.ProtectedStaticGetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.PrivateStaticGetSelfType.MagicNumber);

            Assert.Equal(42, duckStructCopy.PublicStaticGetSetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.InternalStaticGetSetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.ProtectedStaticGetSetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.PrivateStaticGetSetSelfType.MagicNumber);

            Assert.Equal(42, duckStructCopy.PublicGetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.InternalGetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.ProtectedGetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.PrivateGetSelfType.MagicNumber);

            Assert.Equal(42, duckStructCopy.PublicGetSetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.InternalGetSetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.ProtectedGetSetSelfType.MagicNumber);
            Assert.Equal(42, duckStructCopy.PrivateGetSetSelfType.MagicNumber);
        }
    }
}
