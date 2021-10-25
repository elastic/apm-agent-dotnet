// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="DuckIgnoreTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Elastic.Apm.Profiler.Managed.DuckTyping;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping
{
    public class DuckIgnoreTests
    {
        [Fact]
        public void NonPublicStructCopyTest()
        {
            PrivateStruct instance = default;
            CopyStruct copy = instance.DuckCast<CopyStruct>();
            Assert.Equal((int)instance.Value, (int)copy.Value);
            Assert.Equal(ValuesDuckType.Third.ToString(), copy.GetValue());
            Assert.Equal(ValuesDuckType.Third.ToString(), ((IGetValue)copy).GetValueProp);
        }

#if NETCOREAPP3_0_OR_GREATER
        [Fact]
        public void NonPublicStructInterfaceProxyTest()
        {
            PrivateStruct instance = default;
            IPrivateStruct proxy = instance.DuckCast<IPrivateStruct>();
            Assert.Equal((int)instance.Value, (int)proxy.Value);
            Assert.Equal(ValuesDuckType.Third.ToString(), proxy.GetValue());
            Assert.Equal(ValuesDuckType.Third.ToString(), proxy.GetValueProp);
        }
#endif

        [Fact]
        public void NonPublicStructAbstractProxyTest()
        {
            PrivateStruct instance = default;
            AbstractPrivateProxy proxy = instance.DuckCast<AbstractPrivateProxy>();
            Assert.Equal((int)instance.Value, (int)proxy.Value);
            Assert.Equal(ValuesDuckType.Third.ToString(), proxy.GetValue());
            Assert.Equal(ValuesDuckType.Third.ToString(), ((IGetValue)proxy).GetValueProp);
            Assert.Equal(42, proxy.GetAnswerToMeaningOfLife());
        }

        [Fact]
        public void NonPublicStructVirtualProxyTest()
        {
            PrivateStruct instance = default;
            VirtualPrivateProxy proxy = instance.DuckCast<VirtualPrivateProxy>();
            Assert.Equal((int)instance.Value, (int)proxy.Value);
            Assert.Equal(ValuesDuckType.Third.ToString(), proxy.GetValue());
            Assert.Equal(ValuesDuckType.Third.ToString(), ((IGetValue)proxy).GetValueProp);
            Assert.Equal(42, proxy.GetAnswerToMeaningOfLife());
        }

        [DuckCopy]
        public struct CopyStruct : IGetValue
        {
            public ValuesDuckType Value;

            string IGetValue.GetValueProp => Value.ToString();

            public string GetValue() => Value.ToString();
        }

#if NETCOREAPP3_0_OR_GREATER
        // Interface with a default implementation
        public interface IPrivateStruct
        {
            ValuesDuckType Value { get; }

            [DuckIgnore]
            public string GetValueProp => Value.ToString();

            [DuckIgnore]
            public string GetValue() => Value.ToString();
        }
#endif

        public abstract class AbstractPrivateProxy : IGetValue
        {
            public abstract ValuesDuckType Value { get; }

            [DuckIgnore]
            public string GetValueProp => Value.ToString();

            [DuckIgnore]
            public string GetValue() => Value.ToString();

            public abstract int GetAnswerToMeaningOfLife();
        }

        public class VirtualPrivateProxy : IGetValue
        {
            public virtual ValuesDuckType Value { get; }

            [DuckIgnore]
            public string GetValueProp => Value.ToString();

            [DuckIgnore]
            public string GetValue() => Value.ToString();

            public virtual int GetAnswerToMeaningOfLife() => default;
        }

        private readonly struct PrivateStruct
        {
            public readonly Values Value => Values.Third;

            public int GetAnswerToMeaningOfLife() => 42;
        }

        public interface IGetValue
        {
            string GetValueProp { get; }

            string GetValue();
        }

        public enum ValuesDuckType
        {
            /// <summary>
            /// First
            /// </summary>
            First,

            /// <summary>
            /// Second
            /// </summary>
            Second,

            /// <summary>
            /// Third
            /// </summary>
            Third
        }

        public enum Values
        {
            /// <summary>
            /// First
            /// </summary>
            First,

            /// <summary>
            /// Second
            /// </summary>
            Second,

            /// <summary>
            /// Third
            /// </summary>
            Third
        }
    }
}
