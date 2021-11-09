// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="DuckIncludeTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#pragma warning disable SA1201 // Elements must appear in the correct order

using Elastic.Apm.Profiler.Managed.DuckTyping;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping
{
    public class DuckIncludeTests
    {
        [Fact]
        public void ShouldOverrideToString()
        {
            var instance = new SomeClassWithDuckInclude();

            var proxy = instance.DuckCast<IInterface>();

            proxy.ToString().Should().Be(instance.ToString());
        }

        [Fact]
        public void ShouldNotOverrideToString()
        {
            var instance = new SomeClassWithoutDuckInclude();

            var proxy = instance.DuckCast<IInterface>();

            proxy.ToString().Should().NotBe(instance.ToString());
        }

        public class SomeClassWithDuckInclude
        {
            [DuckInclude]
            public override string ToString()
            {
                return "OK";
            }
        }

        public class SomeClassWithoutDuckInclude
        {
            public override string ToString()
            {
                return "OK";
            }
        }

        public interface IInterface
        {
        }
    }
}
