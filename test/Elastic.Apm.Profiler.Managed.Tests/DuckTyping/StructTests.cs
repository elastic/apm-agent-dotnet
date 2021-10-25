// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="StructTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Elastic.Apm.Profiler.Managed.DuckTyping;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping
{
    public class StructTests
    {
        [Fact]
        public void NonPublicStructCopyTest()
        {
            PrivateStruct instance = default;
            CopyStruct copy = instance.DuckCast<CopyStruct>();
            Assert.Equal(instance.Value, copy.Value);
        }

        [Fact]
        public void NonPublicStructInterfaceProxyTest()
        {
            PrivateStruct instance = default;
            IPrivateStruct proxy = instance.DuckCast<IPrivateStruct>();
            Assert.Equal(instance.Value, proxy.Value);
        }

        [Fact]
        public void NonPublicStructAbstractProxyTest()
        {
            PrivateStruct instance = default;
            AbstractPrivateProxy proxy = instance.DuckCast<AbstractPrivateProxy>();
            Assert.Equal(instance.Value, proxy.Value);
        }

        [Fact]
        public void NonPublicStructVirtualProxyTest()
        {
            PrivateStruct instance = default;
            VirtualPrivateProxy proxy = instance.DuckCast<VirtualPrivateProxy>();
            Assert.Equal(instance.Value, proxy.Value);
        }

        [DuckCopy]
        public struct CopyStruct
        {
            public string Value;
        }

        public interface IPrivateStruct
        {
            string Value { get; }
        }

        public abstract class AbstractPrivateProxy
        {
            public abstract string Value { get; }
        }

        public class VirtualPrivateProxy
        {
            public virtual string Value { get; }
        }

        private readonly struct PrivateStruct
        {
            public readonly string Value => "Hello World";
        }

        [Fact]
        public void DuckChainingStructInterfaceProxyTest()
        {
            PrivateDuckChainingTarget instance = new PrivateDuckChainingTarget();
            IPrivateDuckChainingTarget proxy = instance.DuckCast<IPrivateDuckChainingTarget>();
            Assert.Equal(instance.ChainingTestField.Name, proxy.ChainingTestField.Name);
            Assert.Equal(instance.ChainingTest.Name, proxy.ChainingTest.Name);
            Assert.Equal(instance.ChainingTestMethod().Name, proxy.ChainingTestMethod().Name);

            PublicDuckChainingTarget instance2 = new PublicDuckChainingTarget();
            IPrivateDuckChainingTarget proxy2 = instance2.DuckCast<IPrivateDuckChainingTarget>();
            Assert.Equal(instance2.ChainingTestField.Name, proxy2.ChainingTestField.Name);
            Assert.Equal(instance2.ChainingTest.Name, proxy2.ChainingTest.Name);
            Assert.Equal(instance2.ChainingTestMethod().Name, proxy2.ChainingTestMethod().Name);
        }

        public interface IPrivateDuckChainingTarget
        {
            [Duck(Kind = DuckKind.Field)]
            IPrivateTarget ChainingTestField { get; }

            IPrivateTarget ChainingTest { get; }

            IPrivateTarget ChainingTestMethod();
        }

        public interface IPrivateTarget
        {
            [Duck(Kind = DuckKind.Field)]
            public string Name { get; }
        }

        private class PrivateDuckChainingTarget
        {
#pragma warning disable SA1401 // Fields must be private
            public PrivateTarget ChainingTestField = new PrivateTarget { Name = "Hello World 1" };
#pragma warning restore SA1401 // Fields must be private

            public PrivateTarget ChainingTest => new PrivateTarget { Name = "Hello World 2" };

            public PrivateTarget ChainingTestMethod() => new PrivateTarget { Name = "Hello World 3" };
        }

        private struct PrivateTarget
        {
            public string Name;
        }

        public class PublicDuckChainingTarget
        {
#pragma warning disable SA1401 // Fields must be private
            public PublicTarget ChainingTestField = new PublicTarget { Name = "Hello World 1" };
#pragma warning restore SA1401 // Fields must be private

            public PublicTarget ChainingTest => new PublicTarget { Name = "Hello World 2" };

            public PublicTarget ChainingTestMethod() => new PublicTarget { Name = "Hello World 3" };
        }

        public struct PublicTarget
        {
            public string Name;
        }
    }
}
