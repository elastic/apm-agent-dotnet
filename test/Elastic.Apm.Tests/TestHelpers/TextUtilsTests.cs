using System;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class TextUtilsTests
	{
		[Fact]
		public void AddIndentationOneLineOfTextTest()
		{
			TextUtils.Indent("One line of text", 1).Should().Be(TextUtils.Indentation + "One line of text");
			TextUtils.Indent("One line of text", 2).Should().Be(TextUtils.Indentation.Repeat(2) + "One line of text");
			TextUtils.Indent("One line of text", 3).Should().Be(TextUtils.Indentation.Repeat(3) + "One line of text");
		}

		[Fact]
		public void AddIndentationMultipleLinesOfTextTest()
		{
			TextUtils.Indent("1st line of text" + Environment.NewLine + "2nd line of text", 1).Should().Be(
				TextUtils.Indentation + "1st line of text" + Environment.NewLine +
				TextUtils.Indentation + "2nd line of text");

			TextUtils.Indent("1st line of text" + Environment.NewLine + "2nd line of text" + Environment.NewLine, 3).Should().Be(
				TextUtils.Indentation.Repeat(3) + "1st line of text" + Environment.NewLine +
				TextUtils.Indentation.Repeat(3) + "2nd line of text" + Environment.NewLine);

			TextUtils.Indent("" + Environment.NewLine + "2nd line of text", 2).Should().Be(
				TextUtils.Indentation.Repeat(2) + "" + Environment.NewLine +
				TextUtils.Indentation.Repeat(2) + "2nd line of text");

			TextUtils.Indent("" + Environment.NewLine +"2nd line of text" + Environment.NewLine, 3).Should().Be(
				TextUtils.Indentation.Repeat(3) + "" + Environment.NewLine +
				TextUtils.Indentation.Repeat(3) + "2nd line of text" + Environment.NewLine);

			TextUtils.Indent("1st line of text" + Environment.NewLine + "", 1).Should().Be(
				TextUtils.Indentation + "1st line of text" + Environment.NewLine);

			TextUtils.Indent("1st line of text" + Environment.NewLine + "" + Environment.NewLine, 2).Should().Be(
				TextUtils.Indentation.Repeat(2) + "1st line of text" + Environment.NewLine +
				TextUtils.Indentation.Repeat(2) + "" + Environment.NewLine);
		}

		[Fact]
		public void AddIndentationEmptyTextTest()
		{
			var actual = TextUtils.Indent("", 1);
			TextUtils.Indent("", 1).Should().Be(TextUtils.Indentation + "", $"but actual is [{actual.Length}]`{actual}'");
			TextUtils.Indent("", 1).Should().Be(TextUtils.Indentation + "");
			TextUtils.Indent("", 2).Should().Be(TextUtils.Indentation.Repeat(2) + "");
			TextUtils.Indent("", 3).Should().Be(TextUtils.Indentation.Repeat(3) + "");
		}

		[Fact]
		public void AddIndentationNullInputThrowsTest()
		{
			((Action)(() => TextUtils.Indent(null, 1))).Should().ThrowExactly<ArgumentNullException>();
		}

		[Fact]
		public void IndentationIsCharTimesLengthTest()
		{
			TextUtils.Indentation.Should().Be(new string(TextUtils.IndentationChar, TextUtils.IndentationLength));
		}
	}
}
