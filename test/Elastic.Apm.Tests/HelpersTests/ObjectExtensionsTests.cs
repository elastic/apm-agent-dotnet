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
			object obj = null;
			// ReSharper disable once ExpressionIsAlwaysNull
			obj.AsNullableToString().Should().Be(ObjectExtensions.NullAsString);
			obj = new object();
			obj.AsNullableToString().Should().NotBe(ObjectExtensions.NullAsString);

			string str = null;
			// ReSharper disable once ExpressionIsAlwaysNull
			str.AsNullableToString().Should().Be(ObjectExtensions.NullAsString);
			str = "";
			str.AsNullableToString().Should().NotBe(ObjectExtensions.NullAsString);

			int? nullableInt = null;
			// ReSharper disable once ExpressionIsAlwaysNull
			nullableInt.AsNullableToString().Should().Be(ObjectExtensions.NullAsString);
			nullableInt = 1;
			nullableInt.AsNullableToString().Should().NotBe(ObjectExtensions.NullAsString);

			const int i = 1;
			i.AsNullableToString().Should().NotBe(ObjectExtensions.NullAsString);
		}
	}
}
