// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class ObjectExtensionsTests
	{
		[Fact]
		public void Let_simple_test()
		{
			5.Let(x => x.Should().Be(5));
			11.0.Let(x => x.Should().Be(11.0));
			"some test string".Let(x => x.Should().Be("some test string"));
		}

		[Theory]
		[InlineData(null)]
		[InlineData("some test string")]
		public void Let_null_or_string(string possiblyNull)
		{
			var sideEffect = 0;

			possiblyNull?.Let(x =>
			{
				sideEffect = 1;
				x.Should().NotBeNull();
			});

			sideEffect.Should().Be(possiblyNull == null ? 0 : 1);
		}

		[Theory]
		[InlineData(null)]
		[InlineData(false)]
		[InlineData(true)]
		public void Let_nullable_bool(bool? possiblyNull)
		{
			var sideEffect = 0;

			possiblyNull?.Let(x =>
			{
				sideEffect = 1;
				x.Should().Be(possiblyNull.Value);
			});

			sideEffect.Should().Be(possiblyNull == null ? 0 : 1);
		}

		[Fact]
		public void AsNullableToString_test()
		{
			// ReSharper disable ExpressionIsAlwaysNull
			object obj = null;
			obj.AsNullableToString().Should().Be(ObjectExtensions.NullAsString);
			obj = new object();
			obj.AsNullableToString().Should().NotBe(ObjectExtensions.NullAsString);

			string str = null;
			str.AsNullableToString().Should().Be(ObjectExtensions.NullAsString);
			str.AsNullableToString().Should().NotBe(str);
			str = "";
			str.AsNullableToString().Should().Be(str);
			str.AsNullableToString().Should().NotBe(ObjectExtensions.NullAsString);

			const int i = 1;
			i.AsNullableToString().Should().NotBe(ObjectExtensions.NullAsString);
			// ReSharper restore ExpressionIsAlwaysNull
		}

		[Fact]
		public void how_to_chain_AsNullableToString()
		{
			// ReSharper disable ExpressionIsAlwaysNull
			string RightWayToChainAsNullableToString(TimeSpan? nullableTimeSpanArg)
			{
				return (nullableTimeSpanArg?.ToHms()).AsNullableToString();
			}

			string WrongWayToChainAsNullableToString(TimeSpan? nullableTimeSpanArg)
			{
				return nullableTimeSpanArg?.ToHms().AsNullableToString();
			}

			TimeSpan? nullableTimeSpan = TimeSpan.FromHours(1);
			RightWayToChainAsNullableToString(nullableTimeSpan).Should().Be("1h");
			// When nullableTimeSpan is not null the wrong way works
			WrongWayToChainAsNullableToString(nullableTimeSpan).Should().Be("1h");
			nullableTimeSpan = null;
			RightWayToChainAsNullableToString(nullableTimeSpan).Should().Be(ObjectExtensions.NullAsString);
			// But when nullableTimeSpan is null the wrong way does not work
			WrongWayToChainAsNullableToString(nullableTimeSpan).Should().Be(null);
			// ReSharper restore ExpressionIsAlwaysNull
		}
	}
}
