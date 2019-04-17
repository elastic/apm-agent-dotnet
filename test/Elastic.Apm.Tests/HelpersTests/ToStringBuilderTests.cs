using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class ToStringBuilderTests
	{
		/// <summary>
		///  Tests use case for a class without any properties
		/// </summary>
		[Fact(DisplayName = "No properties")]
		public void NoProperties() =>
			new ClassWithoutAnyProperties().ToString().Should().Be(nameof(ClassWithoutAnyProperties) + "{}");

		/// <summary>
		///  Tests use case for a class with one property
		/// </summary>
		[Fact]
		public void OneProperty() => new ClassWithOneProperty(321).ToString()
			.Should()
			.Be(nameof(ClassWithOneProperty) + "{prop: 321}");

		/// <summary>
		///  Tests use case for a class with a few properties
		/// </summary>
		[Fact]
		public void AFewProperties() => new ClassWithAFewProperties(123, "456", 789).ToString()
			.Should()
			.Be(nameof(ClassWithAFewProperties) +
				"{" +
				"A: 123, " +
				"Ax2: 246, " +
				"B: 456, " +
				"C: " + nameof(ClassWithoutAnyProperties) + "{}, " +
				"D: " + nameof(ClassWithOneProperty) + "{prop: 789}" +
				"}");

		/// <summary>
		///  Tests that null value is printed as "null"
		/// </summary>
		[Fact]
		public void NullValue() => new ClassWithAFewProperties(123, null, 789).ToString()
			.Should()
			.Be(nameof(ClassWithAFewProperties) +
				"{" +
				"A: 123, " +
				"Ax2: 246, " +
				"B: null, " +
				"C: " + nameof(ClassWithoutAnyProperties) + "{}, " +
				"D: " + nameof(ClassWithOneProperty) + "{prop: 789}" +
				"}");
		private class ClassWithoutAnyProperties
		{
			public override string ToString() => new ToStringBuilder(nameof(ClassWithoutAnyProperties)).ToString();
		}

		private class ClassWithOneProperty
		{
			private readonly int _intPropertyValue;

			public ClassWithOneProperty(int intPropertyValue) => _intPropertyValue = intPropertyValue;

			public override string ToString() => new ToStringBuilder(nameof(ClassWithOneProperty))
			{
				{ "prop", _intPropertyValue }
			}.ToString();
		}

		private class ClassWithAFewProperties
		{
			private readonly int _propertyA;
			private readonly string _propertyB;
			private readonly ClassWithoutAnyProperties _propertyC = new ClassWithoutAnyProperties();
			private readonly ClassWithOneProperty _propertyD;

			public ClassWithAFewProperties(int propertyA, string propertyB, int propertyD)
			{
				_propertyA = propertyA;
				_propertyB = propertyB;
				_propertyD = new ClassWithOneProperty(propertyD);
			}

			public override string ToString() => new ToStringBuilder(nameof(ClassWithAFewProperties))
			{
				{ "A", _propertyA },
				{ "Ax2", _propertyA * 2 },
				{ "B", _propertyB },
				{ "C", _propertyC },
				{ "D", _propertyD }
			}.ToString();
		}
	}
}
