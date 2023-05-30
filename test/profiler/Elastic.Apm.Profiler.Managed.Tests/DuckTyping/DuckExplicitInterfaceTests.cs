// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// <copyright file="DuckExplicitInterfaceTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Elastic.Apm.Profiler.Managed.DuckTyping;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.DuckTyping
{
	public class DuckExplicitInterfaceTests
	{
		[Fact]
		public void NormalTest()
		{
			var targetObject = new TargetObject();
			var proxy = targetObject.DuckCast<IProxyDefinition>();

			proxy.SayHi().Should().Be("Hello World");
			proxy.SayHiWithWildcard().Should().Be("Hello World (*)");
		}

		public class TargetObject : ITarget
		{
			string ITarget.SayHi() => "Hello World";

			string ITarget.SayHiWithWildcard() => "Hello World (*)";
		}

		public interface ITarget
		{
			string SayHi();

			string SayHiWithWildcard();
		}

		public interface IProxyDefinition
		{
			[Duck(ExplicitInterfaceTypeName = "Elastic.Apm.Profiler.Managed.Tests.DuckTyping.DuckExplicitInterfaceTests+ITarget")]
			string SayHi();

			[Duck(ExplicitInterfaceTypeName = "*")]
			string SayHiWithWildcard();
		}
	}
}
