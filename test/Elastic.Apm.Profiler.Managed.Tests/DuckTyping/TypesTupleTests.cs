// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="TypesTupleTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Elastic.Apm.Profiler.Managed.DuckTyping;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping
{
    public class TypesTupleTests
    {
        [Fact]
        public void EqualsTupleTest()
        {
            TypesTuple tuple1 = new TypesTuple(typeof(string), typeof(int));
            TypesTuple tuple2 = new TypesTuple(typeof(string), typeof(int));

            Assert.True(tuple1.Equals(tuple2));
            Assert.True(tuple1.Equals((object)tuple2));
            Assert.False(tuple1.Equals("Hello World"));
        }
    }
}
