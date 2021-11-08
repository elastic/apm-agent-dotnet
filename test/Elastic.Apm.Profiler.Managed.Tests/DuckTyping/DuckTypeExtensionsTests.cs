// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="DuckTypeExtensionsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Threading.Tasks;
using Elastic.Apm.Profiler.Managed.DuckTyping;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping
{
    public class DuckTypeExtensionsTests
    {
        [Fact]
        public void DuckCastTest()
        {
            var task = (Task)Task.FromResult("Hello World");

            var iTaskString = task.DuckCast<ITaskString>();
            var objTaskString = task.DuckCast(typeof(ITaskString));

			iTaskString.Result.Should().Be("Hello World");
			(iTaskString.GetType() == objTaskString.GetType()).Should().BeTrue();
		}

        [Fact]
        public void NullCheck()
        {
            object obj = null;
            var iTaskString = obj.DuckCast<ITaskString>();

			iTaskString.Should().BeNull();
		}

        [Fact]
        public void TryDuckCastTest()
        {
            var task = (Task)Task.FromResult("Hello World");

            var tskResultBool = task.TryDuckCast<ITaskString>(out var tskResult);
            Assert.True(tskResultBool);
            Assert.Equal("Hello World", tskResult.Result);

            var tskErrorBool = task.TryDuckCast<ITaskError>(out var tskResultError);
            Assert.False(tskErrorBool);
            Assert.Null(tskResultError);
        }

        [Fact]
        public void DuckAsTest()
        {
            var task = (Task)Task.FromResult("Hello World");

            var tskResult = task.DuckAs<ITaskString>();
            var tskResultError = task.DuckAs<ITaskError>();

			tskResult.Result.Should().Be("Hello World");
			tskResultError.Should().BeNull();
		}

        [Fact]
        public void DuckIsTest()
        {
            var task = (Task)Task.FromResult("Hello World");

            var bOk = task.DuckIs<ITaskString>();
            var bError = task.DuckIs<ITaskError>();

			bOk.Should().BeTrue();
			bError.Should().BeFalse();
		}

        public interface ITaskString
        {
            string Result { get; }
        }

        public interface ITaskError
        {
            string ResultWrong { get; }
        }
    }
}
