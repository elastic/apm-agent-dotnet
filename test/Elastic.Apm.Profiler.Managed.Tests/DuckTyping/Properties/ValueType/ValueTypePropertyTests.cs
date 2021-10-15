// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="ValueTypePropertyTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Profiler.Managed.DuckTyping;
using Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.ValueType.ProxiesDefinitions;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping.Properties.ValueType
{
    public partial class ValueTypePropertyTests
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
            Assert.Equal(10, duckInterface.PublicStaticGetValueType);
            Assert.Equal(10, duckAbstract.PublicStaticGetValueType);
            Assert.Equal(10, duckVirtual.PublicStaticGetValueType);

            // *
            Assert.Equal(11, duckInterface.InternalStaticGetValueType);
            Assert.Equal(11, duckAbstract.InternalStaticGetValueType);
            Assert.Equal(11, duckVirtual.InternalStaticGetValueType);

            // *
            Assert.Equal(12, duckInterface.ProtectedStaticGetValueType);
            Assert.Equal(12, duckAbstract.ProtectedStaticGetValueType);
            Assert.Equal(12, duckVirtual.ProtectedStaticGetValueType);

            // *
            Assert.Equal(13, duckInterface.PrivateStaticGetValueType);
            Assert.Equal(13, duckAbstract.PrivateStaticGetValueType);
            Assert.Equal(13, duckVirtual.PrivateStaticGetValueType);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StaticProperties(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Equal(20, duckInterface.PublicStaticGetSetValueType);
            Assert.Equal(20, duckAbstract.PublicStaticGetSetValueType);
            Assert.Equal(20, duckVirtual.PublicStaticGetSetValueType);

            duckInterface.PublicStaticGetSetValueType = 42;
            Assert.Equal(42, duckInterface.PublicStaticGetSetValueType);
            Assert.Equal(42, duckAbstract.PublicStaticGetSetValueType);
            Assert.Equal(42, duckVirtual.PublicStaticGetSetValueType);

            duckAbstract.PublicStaticGetSetValueType = 50;
            Assert.Equal(50, duckInterface.PublicStaticGetSetValueType);
            Assert.Equal(50, duckAbstract.PublicStaticGetSetValueType);
            Assert.Equal(50, duckVirtual.PublicStaticGetSetValueType);

            duckVirtual.PublicStaticGetSetValueType = 60;
            Assert.Equal(60, duckInterface.PublicStaticGetSetValueType);
            Assert.Equal(60, duckAbstract.PublicStaticGetSetValueType);
            Assert.Equal(60, duckVirtual.PublicStaticGetSetValueType);

            duckInterface.PublicStaticGetSetValueType = 20;

            // *

            Assert.Equal(21, duckInterface.InternalStaticGetSetValueType);
            Assert.Equal(21, duckAbstract.InternalStaticGetSetValueType);
            Assert.Equal(21, duckVirtual.InternalStaticGetSetValueType);

            duckInterface.InternalStaticGetSetValueType = 42;
            Assert.Equal(42, duckInterface.InternalStaticGetSetValueType);
            Assert.Equal(42, duckAbstract.InternalStaticGetSetValueType);
            Assert.Equal(42, duckVirtual.InternalStaticGetSetValueType);

            duckAbstract.InternalStaticGetSetValueType = 50;
            Assert.Equal(50, duckInterface.InternalStaticGetSetValueType);
            Assert.Equal(50, duckAbstract.InternalStaticGetSetValueType);
            Assert.Equal(50, duckVirtual.InternalStaticGetSetValueType);

            duckVirtual.InternalStaticGetSetValueType = 60;
            Assert.Equal(60, duckInterface.InternalStaticGetSetValueType);
            Assert.Equal(60, duckAbstract.InternalStaticGetSetValueType);
            Assert.Equal(60, duckVirtual.InternalStaticGetSetValueType);

            duckInterface.InternalStaticGetSetValueType = 21;

            // *

            Assert.Equal(22, duckInterface.ProtectedStaticGetSetValueType);
            Assert.Equal(22, duckAbstract.ProtectedStaticGetSetValueType);
            Assert.Equal(22, duckVirtual.ProtectedStaticGetSetValueType);

            duckInterface.ProtectedStaticGetSetValueType = 42;
            Assert.Equal(42, duckInterface.ProtectedStaticGetSetValueType);
            Assert.Equal(42, duckAbstract.ProtectedStaticGetSetValueType);
            Assert.Equal(42, duckVirtual.ProtectedStaticGetSetValueType);

            duckAbstract.ProtectedStaticGetSetValueType = 50;
            Assert.Equal(50, duckInterface.ProtectedStaticGetSetValueType);
            Assert.Equal(50, duckAbstract.ProtectedStaticGetSetValueType);
            Assert.Equal(50, duckVirtual.ProtectedStaticGetSetValueType);

            duckVirtual.ProtectedStaticGetSetValueType = 60;
            Assert.Equal(60, duckInterface.ProtectedStaticGetSetValueType);
            Assert.Equal(60, duckAbstract.ProtectedStaticGetSetValueType);
            Assert.Equal(60, duckVirtual.ProtectedStaticGetSetValueType);

            duckInterface.ProtectedStaticGetSetValueType = 22;

            // *

            Assert.Equal(23, duckInterface.PrivateStaticGetSetValueType);
            Assert.Equal(23, duckAbstract.PrivateStaticGetSetValueType);
            Assert.Equal(23, duckVirtual.PrivateStaticGetSetValueType);

            duckInterface.PrivateStaticGetSetValueType = 42;
            Assert.Equal(42, duckInterface.PrivateStaticGetSetValueType);
            Assert.Equal(42, duckAbstract.PrivateStaticGetSetValueType);
            Assert.Equal(42, duckVirtual.PrivateStaticGetSetValueType);

            duckAbstract.PrivateStaticGetSetValueType = 50;
            Assert.Equal(50, duckInterface.PrivateStaticGetSetValueType);
            Assert.Equal(50, duckAbstract.PrivateStaticGetSetValueType);
            Assert.Equal(50, duckVirtual.PrivateStaticGetSetValueType);

            duckVirtual.PrivateStaticGetSetValueType = 60;
            Assert.Equal(60, duckInterface.PrivateStaticGetSetValueType);
            Assert.Equal(60, duckAbstract.PrivateStaticGetSetValueType);
            Assert.Equal(60, duckVirtual.PrivateStaticGetSetValueType);

            duckInterface.PrivateStaticGetSetValueType = 23;
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void GetOnlyProperties(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            // *
            Assert.Equal(30, duckInterface.PublicGetValueType);
            Assert.Equal(30, duckAbstract.PublicGetValueType);
            Assert.Equal(30, duckVirtual.PublicGetValueType);

            // *
            Assert.Equal(31, duckInterface.InternalGetValueType);
            Assert.Equal(31, duckAbstract.InternalGetValueType);
            Assert.Equal(31, duckVirtual.InternalGetValueType);

            // *
            Assert.Equal(32, duckInterface.ProtectedGetValueType);
            Assert.Equal(32, duckAbstract.ProtectedGetValueType);
            Assert.Equal(32, duckVirtual.ProtectedGetValueType);

            // *
            Assert.Equal(33, duckInterface.PrivateGetValueType);
            Assert.Equal(33, duckAbstract.PrivateGetValueType);
            Assert.Equal(33, duckVirtual.PrivateGetValueType);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Properties(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Equal(40, duckInterface.PublicGetSetValueType);
            Assert.Equal(40, duckAbstract.PublicGetSetValueType);
            Assert.Equal(40, duckVirtual.PublicGetSetValueType);

            duckInterface.PublicGetSetValueType = 42;
            Assert.Equal(42, duckInterface.PublicGetSetValueType);
            Assert.Equal(42, duckAbstract.PublicGetSetValueType);
            Assert.Equal(42, duckVirtual.PublicGetSetValueType);

            duckAbstract.PublicGetSetValueType = 50;
            Assert.Equal(50, duckInterface.PublicGetSetValueType);
            Assert.Equal(50, duckAbstract.PublicGetSetValueType);
            Assert.Equal(50, duckVirtual.PublicGetSetValueType);

            duckVirtual.PublicGetSetValueType = 60;
            Assert.Equal(60, duckInterface.PublicGetSetValueType);
            Assert.Equal(60, duckAbstract.PublicGetSetValueType);
            Assert.Equal(60, duckVirtual.PublicGetSetValueType);

            duckInterface.PublicGetSetValueType = 40;

            // *

            Assert.Equal(41, duckInterface.InternalGetSetValueType);
            Assert.Equal(41, duckAbstract.InternalGetSetValueType);
            Assert.Equal(41, duckVirtual.InternalGetSetValueType);

            duckInterface.InternalGetSetValueType = 42;
            Assert.Equal(42, duckInterface.InternalGetSetValueType);
            Assert.Equal(42, duckAbstract.InternalGetSetValueType);
            Assert.Equal(42, duckVirtual.InternalGetSetValueType);

            duckAbstract.InternalGetSetValueType = 50;
            Assert.Equal(50, duckInterface.InternalGetSetValueType);
            Assert.Equal(50, duckAbstract.InternalGetSetValueType);
            Assert.Equal(50, duckVirtual.InternalGetSetValueType);

            duckVirtual.InternalGetSetValueType = 60;
            Assert.Equal(60, duckInterface.InternalGetSetValueType);
            Assert.Equal(60, duckAbstract.InternalGetSetValueType);
            Assert.Equal(60, duckVirtual.InternalGetSetValueType);

            duckInterface.InternalGetSetValueType = 41;

            // *

            Assert.Equal(42, duckInterface.ProtectedGetSetValueType);
            Assert.Equal(42, duckAbstract.ProtectedGetSetValueType);
            Assert.Equal(42, duckVirtual.ProtectedGetSetValueType);

            duckInterface.ProtectedGetSetValueType = 45;
            Assert.Equal(45, duckInterface.ProtectedGetSetValueType);
            Assert.Equal(45, duckAbstract.ProtectedGetSetValueType);
            Assert.Equal(45, duckVirtual.ProtectedGetSetValueType);

            duckAbstract.ProtectedGetSetValueType = 50;
            Assert.Equal(50, duckInterface.ProtectedGetSetValueType);
            Assert.Equal(50, duckAbstract.ProtectedGetSetValueType);
            Assert.Equal(50, duckVirtual.ProtectedGetSetValueType);

            duckVirtual.ProtectedGetSetValueType = 60;
            Assert.Equal(60, duckInterface.ProtectedGetSetValueType);
            Assert.Equal(60, duckAbstract.ProtectedGetSetValueType);
            Assert.Equal(60, duckVirtual.ProtectedGetSetValueType);

            duckInterface.ProtectedGetSetValueType = 42;

            // *

            Assert.Equal(43, duckInterface.PrivateGetSetValueType);
            Assert.Equal(43, duckAbstract.PrivateGetSetValueType);
            Assert.Equal(43, duckVirtual.PrivateGetSetValueType);

            duckInterface.PrivateGetSetValueType = 42;
            Assert.Equal(42, duckInterface.PrivateGetSetValueType);
            Assert.Equal(42, duckAbstract.PrivateGetSetValueType);
            Assert.Equal(42, duckVirtual.PrivateGetSetValueType);

            duckAbstract.PrivateGetSetValueType = 50;
            Assert.Equal(50, duckInterface.PrivateGetSetValueType);
            Assert.Equal(50, duckAbstract.PrivateGetSetValueType);
            Assert.Equal(50, duckVirtual.PrivateGetSetValueType);

            duckVirtual.PrivateGetSetValueType = 60;
            Assert.Equal(60, duckInterface.PrivateGetSetValueType);
            Assert.Equal(60, duckAbstract.PrivateGetSetValueType);
            Assert.Equal(60, duckVirtual.PrivateGetSetValueType);

            duckInterface.PrivateGetSetValueType = 43;
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Indexer(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            duckInterface[1] = 100;
            Assert.Equal(100, duckInterface[1]);
            Assert.Equal(100, duckAbstract[1]);
            Assert.Equal(100, duckVirtual[1]);

            duckAbstract[2] = 200;
            Assert.Equal(200, duckInterface[2]);
            Assert.Equal(200, duckAbstract[2]);
            Assert.Equal(200, duckVirtual[2]);

            duckVirtual[3] = 300;
            Assert.Equal(300, duckInterface[3]);
            Assert.Equal(300, duckAbstract[3]);
            Assert.Equal(300, duckVirtual[3]);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void NullableOfKnown(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Null(duckInterface.PublicStaticNullableInt);
            Assert.Null(duckAbstract.PublicStaticNullableInt);
            Assert.Null(duckVirtual.PublicStaticNullableInt);

            duckInterface.PublicStaticNullableInt = 42;
            Assert.Equal(42, duckInterface.PublicStaticNullableInt);
            Assert.Equal(42, duckAbstract.PublicStaticNullableInt);
            Assert.Equal(42, duckVirtual.PublicStaticNullableInt);

            duckAbstract.PublicStaticNullableInt = 50;
            Assert.Equal(50, duckInterface.PublicStaticNullableInt);
            Assert.Equal(50, duckAbstract.PublicStaticNullableInt);
            Assert.Equal(50, duckVirtual.PublicStaticNullableInt);

            duckVirtual.PublicStaticNullableInt = null;
            Assert.Null(duckInterface.PublicStaticNullableInt);
            Assert.Null(duckAbstract.PublicStaticNullableInt);
            Assert.Null(duckVirtual.PublicStaticNullableInt);

            // *

            Assert.Null(duckInterface.PrivateStaticNullableInt);
            Assert.Null(duckAbstract.PrivateStaticNullableInt);
            Assert.Null(duckVirtual.PrivateStaticNullableInt);

            duckInterface.PrivateStaticNullableInt = 42;
            Assert.Equal(42, duckInterface.PrivateStaticNullableInt);
            Assert.Equal(42, duckAbstract.PrivateStaticNullableInt);
            Assert.Equal(42, duckVirtual.PrivateStaticNullableInt);

            duckAbstract.PrivateStaticNullableInt = 50;
            Assert.Equal(50, duckInterface.PrivateStaticNullableInt);
            Assert.Equal(50, duckAbstract.PrivateStaticNullableInt);
            Assert.Equal(50, duckVirtual.PrivateStaticNullableInt);

            duckVirtual.PrivateStaticNullableInt = null;
            Assert.Null(duckInterface.PrivateStaticNullableInt);
            Assert.Null(duckAbstract.PrivateStaticNullableInt);
            Assert.Null(duckVirtual.PrivateStaticNullableInt);

            // *

            Assert.Null(duckInterface.PublicNullableInt);
            Assert.Null(duckAbstract.PublicNullableInt);
            Assert.Null(duckVirtual.PublicNullableInt);

            duckInterface.PublicNullableInt = 42;
            Assert.Equal(42, duckInterface.PublicNullableInt);
            Assert.Equal(42, duckAbstract.PublicNullableInt);
            Assert.Equal(42, duckVirtual.PublicNullableInt);

            duckAbstract.PublicNullableInt = 50;
            Assert.Equal(50, duckInterface.PublicNullableInt);
            Assert.Equal(50, duckAbstract.PublicNullableInt);
            Assert.Equal(50, duckVirtual.PublicNullableInt);

            duckVirtual.PublicNullableInt = null;
            Assert.Null(duckInterface.PublicNullableInt);
            Assert.Null(duckAbstract.PublicNullableInt);
            Assert.Null(duckVirtual.PublicNullableInt);

            // *

            Assert.Null(duckInterface.PrivateNullableInt);
            Assert.Null(duckAbstract.PrivateNullableInt);
            Assert.Null(duckVirtual.PrivateNullableInt);

            duckInterface.PrivateNullableInt = 42;
            Assert.Equal(42, duckInterface.PrivateNullableInt);
            Assert.Equal(42, duckAbstract.PrivateNullableInt);
            Assert.Equal(42, duckVirtual.PrivateNullableInt);

            duckAbstract.PrivateNullableInt = 50;
            Assert.Equal(50, duckInterface.PrivateNullableInt);
            Assert.Equal(50, duckAbstract.PrivateNullableInt);
            Assert.Equal(50, duckVirtual.PrivateNullableInt);

            duckVirtual.PrivateNullableInt = null;
            Assert.Null(duckInterface.PrivateNullableInt);
            Assert.Null(duckAbstract.PrivateNullableInt);
            Assert.Null(duckVirtual.PrivateNullableInt);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void KnownEnum(object obscureObject)
        {
            var duckInterface = obscureObject.DuckCast<IObscureDuckType>();
            var duckAbstract = obscureObject.DuckCast<ObscureDuckTypeAbstractClass>();
            var duckVirtual = obscureObject.DuckCast<ObscureDuckTypeVirtualClass>();

            Assert.Equal(TaskStatus.RanToCompletion, duckInterface.Status);
            Assert.Equal(TaskStatus.RanToCompletion, duckAbstract.Status);
            Assert.Equal(TaskStatus.RanToCompletion, duckVirtual.Status);

            duckInterface.Status = TaskStatus.Running;

            Assert.Equal(TaskStatus.Running, duckInterface.Status);
            Assert.Equal(TaskStatus.Running, duckAbstract.Status);
            Assert.Equal(TaskStatus.Running, duckVirtual.Status);

            duckAbstract.Status = TaskStatus.Faulted;

            Assert.Equal(TaskStatus.Faulted, duckInterface.Status);
            Assert.Equal(TaskStatus.Faulted, duckAbstract.Status);
            Assert.Equal(TaskStatus.Faulted, duckVirtual.Status);

            duckVirtual.Status = TaskStatus.WaitingForActivation;

            Assert.Equal(TaskStatus.WaitingForActivation, duckInterface.Status);
            Assert.Equal(TaskStatus.WaitingForActivation, duckAbstract.Status);
            Assert.Equal(TaskStatus.WaitingForActivation, duckVirtual.Status);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void StructCopy(object obscureObject)
        {
            var duckStructCopy = obscureObject.DuckCast<ObscureDuckTypeStruct>();

            Assert.Equal(10, duckStructCopy.PublicStaticGetValueType);
            Assert.Equal(11, duckStructCopy.InternalStaticGetValueType);
            Assert.Equal(12, duckStructCopy.ProtectedStaticGetValueType);
            Assert.Equal(13, duckStructCopy.PrivateStaticGetValueType);

            Assert.Equal(20, duckStructCopy.PublicStaticGetSetValueType);
            Assert.Equal(21, duckStructCopy.InternalStaticGetSetValueType);
            Assert.Equal(22, duckStructCopy.ProtectedStaticGetSetValueType);
            Assert.Equal(23, duckStructCopy.PrivateStaticGetSetValueType);

            Assert.Equal(30, duckStructCopy.PublicGetValueType);
            Assert.Equal(31, duckStructCopy.InternalGetValueType);
            Assert.Equal(32, duckStructCopy.ProtectedGetValueType);
            Assert.Equal(33, duckStructCopy.PrivateGetValueType);

            Assert.Equal(40, duckStructCopy.PublicGetSetValueType);
            Assert.Equal(41, duckStructCopy.InternalGetSetValueType);
            Assert.Equal(42, duckStructCopy.ProtectedGetSetValueType);
            Assert.Equal(43, duckStructCopy.PrivateGetSetValueType);
        }

        [Fact]
        public void StructDuckType()
        {
            ObscureObject.PublicStruct source = default;
            source.PublicGetSetValueType = 42;

            var dest = source.DuckCast<IStructDuckType>();
            Assert.Equal(source.PublicGetSetValueType, dest.PublicGetSetValueType);
        }
    }
}
