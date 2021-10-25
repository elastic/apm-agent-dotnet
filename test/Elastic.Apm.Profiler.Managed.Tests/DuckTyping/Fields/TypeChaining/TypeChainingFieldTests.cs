// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="TypeChainingFieldTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using Elastic.Apm.Profiler.Managed.DuckTyping;
using Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Fields.TypeChaining.ProxiesDefinitions;
using Xunit;

#pragma warning disable SA1201 // Elements must appear in the correct order

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Fields.TypeChaining
{
    public class TypeChainingFieldTests
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

            Assert.Equal(42, duckInterface.PublicStaticReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckAbstract.PublicStaticReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckVirtual.PublicStaticReadonlySelfTypeField.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.PublicStaticReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.PublicStaticReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.PublicStaticReadonlySelfTypeField).Instance);

            // *

            Assert.Equal(42, duckInterface.InternalStaticReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckAbstract.InternalStaticReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckVirtual.InternalStaticReadonlySelfTypeField.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.InternalStaticReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.InternalStaticReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.InternalStaticReadonlySelfTypeField).Instance);

            // *

            Assert.Equal(42, duckInterface.ProtectedStaticReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckAbstract.ProtectedStaticReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckVirtual.ProtectedStaticReadonlySelfTypeField.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.ProtectedStaticReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.ProtectedStaticReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.ProtectedStaticReadonlySelfTypeField).Instance);

            // *

            Assert.Equal(42, duckInterface.PrivateStaticReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckAbstract.PrivateStaticReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckVirtual.PrivateStaticReadonlySelfTypeField.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.PrivateStaticReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.PrivateStaticReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.PrivateStaticReadonlySelfTypeField).Instance);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StaticFields(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            IDummyFieldObject newDummy = null;

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 42 }).DuckCast<IDummyFieldObject>();
            duckInterface.PublicStaticSelfTypeField = newDummy;

            Assert.Equal(42, duckInterface.PublicStaticSelfTypeField.MagicNumber);
            Assert.Equal(42, duckAbstract.PublicStaticSelfTypeField.MagicNumber);
            Assert.Equal(42, duckVirtual.PublicStaticSelfTypeField.MagicNumber);

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 52 }).DuckCast<IDummyFieldObject>();
            duckInterface.InternalStaticSelfTypeField = newDummy;

            Assert.Equal(52, duckInterface.InternalStaticSelfTypeField.MagicNumber);
            Assert.Equal(52, duckAbstract.InternalStaticSelfTypeField.MagicNumber);
            Assert.Equal(52, duckVirtual.InternalStaticSelfTypeField.MagicNumber);

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 62 }).DuckCast<IDummyFieldObject>();
            duckAbstract.ProtectedStaticSelfTypeField = newDummy;

            Assert.Equal(62, duckInterface.ProtectedStaticSelfTypeField.MagicNumber);
            Assert.Equal(62, duckAbstract.ProtectedStaticSelfTypeField.MagicNumber);
            Assert.Equal(62, duckVirtual.ProtectedStaticSelfTypeField.MagicNumber);

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 72 }).DuckCast<IDummyFieldObject>();
            duckAbstract.PrivateStaticSelfTypeField = newDummy;

            Assert.Equal(72, duckInterface.PrivateStaticSelfTypeField.MagicNumber);
            Assert.Equal(72, duckAbstract.PrivateStaticSelfTypeField.MagicNumber);
            Assert.Equal(72, duckVirtual.PrivateStaticSelfTypeField.MagicNumber);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void ReadonlyFields(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // *

            Assert.Equal(42, duckInterface.PublicReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckAbstract.PublicReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckVirtual.PublicReadonlySelfTypeField.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.PublicReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.PublicReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.PublicReadonlySelfTypeField).Instance);

            // *

            Assert.Equal(42, duckInterface.InternalReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckAbstract.InternalReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckVirtual.InternalReadonlySelfTypeField.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.InternalReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.InternalReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.InternalReadonlySelfTypeField).Instance);

            // *

            Assert.Equal(42, duckInterface.ProtectedReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckAbstract.ProtectedReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckVirtual.ProtectedReadonlySelfTypeField.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.ProtectedReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.ProtectedReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.ProtectedReadonlySelfTypeField).Instance);

            // *

            Assert.Equal(42, duckInterface.PrivateReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckAbstract.PrivateReadonlySelfTypeField.MagicNumber);
            Assert.Equal(42, duckVirtual.PrivateReadonlySelfTypeField.MagicNumber);

            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckInterface.PrivateReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckAbstract.PrivateReadonlySelfTypeField).Instance);
            Assert.Equal(ObscureObject.DummyFieldObject.Default, ((IDuckType)duckVirtual.PrivateReadonlySelfTypeField).Instance);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Fields(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            IDummyFieldObject newDummy = null;

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 42 }).DuckCast<IDummyFieldObject>();
            duckInterface.PublicSelfTypeField = newDummy;

            Assert.Equal(42, duckInterface.PublicSelfTypeField.MagicNumber);
            Assert.Equal(42, duckAbstract.PublicSelfTypeField.MagicNumber);
            Assert.Equal(42, duckVirtual.PublicSelfTypeField.MagicNumber);

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 52 }).DuckCast<IDummyFieldObject>();
            duckInterface.InternalSelfTypeField = newDummy;

            Assert.Equal(52, duckInterface.InternalSelfTypeField.MagicNumber);
            Assert.Equal(52, duckAbstract.InternalSelfTypeField.MagicNumber);
            Assert.Equal(52, duckVirtual.InternalSelfTypeField.MagicNumber);

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 62 }).DuckCast<IDummyFieldObject>();
            duckInterface.ProtectedSelfTypeField = newDummy;

            Assert.Equal(62, duckInterface.ProtectedSelfTypeField.MagicNumber);
            Assert.Equal(62, duckAbstract.ProtectedSelfTypeField.MagicNumber);
            Assert.Equal(62, duckVirtual.ProtectedSelfTypeField.MagicNumber);

            // *
            newDummy = (new ObscureObject.DummyFieldObject { MagicNumber = 72 }).DuckCast<IDummyFieldObject>();
            duckInterface.PrivateSelfTypeField = newDummy;

            Assert.Equal(72, duckInterface.PrivateSelfTypeField.MagicNumber);
            Assert.Equal(72, duckAbstract.PrivateSelfTypeField.MagicNumber);
            Assert.Equal(72, duckVirtual.PrivateSelfTypeField.MagicNumber);
        }
    }
}
