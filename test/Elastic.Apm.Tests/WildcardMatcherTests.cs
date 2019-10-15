using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class WildcardMatcherTests
	{
		[Fact]
		public void TestMatchesStartsWith()
		{
			var matcher = WildcardMatcher.ValueOf("foo*");

			matcher.Matches("foo").Should().BeTrue();
			matcher.Matches("foobar").Should().BeTrue();

			matcher.Matches("bar").Should().BeFalse();
			matcher.Matches("barfoo").Should().BeFalse();
			matcher.Matches("rfoo").Should().BeFalse();
		}

		[Fact]
		public void TestWildcardInTheMiddle()
		{
			var matcher = WildcardMatcher.ValueOf("/foo/*/baz");

			matcher.Matches("/foo/*/baz").Should().BeTrue();
			matcher.Matches("/foo/bar/baz").Should().BeTrue();
			matcher.Matches("/foo/bar", "/baz").Should().BeTrue();
			matcher.Matches("/foo/bar/b", "az").Should().BeTrue();

			matcher.Matches("/foo/bar", "/boaz").Should().BeFalse();
			matcher.Matches("/foo/bar").Should().BeFalse();
		}

		[Fact]
		public void TestCompoundWildcardMatcher()
		{
			var matcher = WildcardMatcher.ValueOf("*foo*foo*");

			matcher.Matches("foofoo").Should().BeTrue();
			matcher.Matches("foo/bar/foo").Should().BeTrue();
			matcher.Matches("/foo/bar/foo/bar").Should().BeTrue();

			matcher.Matches("foo").Should().BeFalse();
		}

		[Fact]
		public void TestCompoundWildcardMatcher3()
		{
			var matcher = WildcardMatcher.ValueOf("*foo*oo*");

			matcher.Matches("foooo").Should().BeTrue();
			matcher.Matches("foofoo").Should().BeTrue();
			matcher.Matches("foo/bar/foo").Should().BeTrue();
			matcher.Matches("/foo/bar/foo/bar").Should().BeTrue();

			matcher.Matches("foo").Should().BeFalse();
			matcher.Matches("fooo").Should().BeFalse();
		}

		[Fact]
		public void TestCompoundWildcardMatcher2()
		{
			var matcher = WildcardMatcher.ValueOf("*foo*bar*");

			matcher.Matches("foobar").Should().BeTrue();
			matcher.Matches("foo/bar/foo/baz").Should().BeTrue();
			matcher.Matches("/foo/bar/baz").Should().BeTrue();

			matcher.Matches("bar/foo").Should().BeFalse();
			matcher.Matches("barfoo").Should().BeFalse();
		}

		[Fact]
		public void TestCompoundWildcardMatcher4()
		{
			var matcher = WildcardMatcher.ValueOf("*foo*far*");

			matcher.Matches("foofar").Should().BeTrue();
			matcher.Matches("foo/far/foo/baz").Should().BeTrue();
			matcher.Matches("/foo/far/baz").Should().BeTrue();

			matcher.Matches("/far/foo").Should().BeFalse();
			matcher.Matches("farfoo").Should().BeFalse();
		}

		[Fact]
		public void TestMatchBetween()
		{
			var matcher = WildcardMatcher.ValueOf("*foo*foo*");

			matcher.Matches("foofoo").Should().BeTrue();
			matcher.Matches("foofo", "o").Should().BeTrue();
			matcher.Matches("foof", "oo").Should().BeTrue();
			matcher.Matches("foo", "foo").Should().BeTrue();
			matcher.Matches("fo", "ofoo").Should().BeTrue();
			matcher.Matches("f", "oofoo").Should().BeTrue();
			matcher.Matches("foo/foo/foo/baz").Should().BeTrue();
			matcher.Matches("/foo/foo/baz").Should().BeTrue();
			matcher.Matches("/foo/foo").Should().BeTrue();

			matcher.Matches("foobar").Should().BeFalse();
		}

		[Fact]
		[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
		[SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
		public void TestCharAt()
		{
			WildcardMatcher.SimpleWildcardMatcher.CharAt(0, "foo", "bar", "foo".Length).Should().Be('f');
			WildcardMatcher.SimpleWildcardMatcher.CharAt(1, "foo", "bar", "foo".Length).Should().Be('o');
			WildcardMatcher.SimpleWildcardMatcher.CharAt(2, "foo", "bar", "foo".Length).Should().Be('o');
			WildcardMatcher.SimpleWildcardMatcher.CharAt(3, "foo", "bar", "foo".Length).Should().Be('b');
			WildcardMatcher.SimpleWildcardMatcher.CharAt(4, "foo", "bar", "foo".Length).Should().Be('a');
			WildcardMatcher.SimpleWildcardMatcher.CharAt(5, "foo", "bar", "foo".Length).Should().Be('r');
		}

		[Fact]
		public void TestIndexOfIgnoreCase()
		{
			WildcardMatcher.IndexOfIgnoreCase("foo", "foo", "foo", false, 0, 6).Should().Be(0);
			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "foo", false, 0, 6).Should().Be(0);
			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "oob", false, 0, 6).Should().Be(1);
			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "oba", false, 0, 6).Should().Be(2);
			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "bar", false, 0, 6).Should().Be(3);
			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "o", false, 0, 6).Should().Be(1);
			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "ob", false, 0, 6).Should().Be(2);
			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "ooba", false, 0, 6).Should().Be(1);
			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "oobar", false, 0, 6).Should().Be(1);
			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "fooba", false, 0, 6).Should().Be(0);
			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "foobar", false, 0, 6).Should().Be(0);
			WildcardMatcher.IndexOfIgnoreCase("afoo", "bar", "oba", false, 0, 7).Should().Be(3);
			WildcardMatcher.IndexOfIgnoreCase("afoo", "bara", "oba", false, 0, 8).Should().Be(3);
			WildcardMatcher.IndexOfIgnoreCase("aafoo", "baraa", "oba", false, 0, 10).Should().Be(4);

			WildcardMatcher.IndexOfIgnoreCase("foo", "bar", "ara", false, 0, 6).Should().Be(-1);
		}

		[Fact]
		public void TestComplexExpressions()
		{
			WildcardMatcher.ValueOf("/foo/*/baz*").Matches("/foo/a/bar/b/baz").Should().BeTrue();
			WildcardMatcher.ValueOf("/foo/*/bar/*/baz").Matches("/foo/a/bar/b/baz").Should().BeTrue();
		}


		[Fact]
		public void TestInfixEmptyMatcher()
		{
			var matcher = WildcardMatcher.ValueOf("**");

			matcher.Matches("").Should().BeTrue();
			matcher.Matches("foo").Should().BeTrue();
		}

		[Fact]
		public void TestMatchesPartitionedStringStartsWith()
		{
			var matcher = WildcardMatcher.ValueOf("/foo/bar*");

			matcher.Matches("/foo/bar/baz", "").Should().BeTrue();
			matcher.Matches("", "/foo/bar/baz").Should().BeTrue();
			matcher.Matches("/foo/bar", "/baz").Should().BeTrue();
			matcher.Matches("/foo", "/bar/baz").Should().BeTrue();
			matcher.Matches("/foo", "/bar/baz").Should().BeTrue();
			matcher.Matches("/bar", "/bar/baz").Should().BeFalse();
			matcher.Matches("/foo", "/foo/baz").Should().BeFalse();
			matcher.Matches("/foo/foo", "/baz").Should().BeFalse();
		}

		[Fact]
		public void TestMatchesEndsWith()
		{
			var matcher = WildcardMatcher.ValueOf("*foo");

			matcher.Matches("foo").Should().BeTrue();
			matcher.Matches("foobar").Should().BeFalse();
			matcher.Matches("bar").Should().BeFalse();
			matcher.Matches("barfoo").Should().BeTrue();
			matcher.Matches("foor").Should().BeFalse();
		}

		[Fact]
		public void TestMatchesPartitionedStringEndsWith()
		{
			var matcher = WildcardMatcher.ValueOf("*/bar/baz");
			matcher.Matches("/foo/bar/baz", "").Should().BeTrue();
			matcher.Matches("", "/foo/bar/baz").Should().BeTrue();
			matcher.Matches("/foo/bar", "/baz").Should().BeTrue();
			matcher.Matches("/foo", "/bar/baz").Should().BeTrue();
			matcher.Matches("/foo", "/bar/baz").Should().BeTrue();
			matcher.Matches("/bar", "/foo/baz").Should().BeFalse();
			matcher.Matches("/foo", "/foo/baz").Should().BeFalse();
		}


		[Fact]
		public void TestMatchesEquals()
		{
			var matcher = WildcardMatcher.ValueOf("foo");

			matcher.Matches("foo").Should().BeTrue();
			matcher.Matches("foobar").Should().BeFalse();
			matcher.Matches("bar").Should().BeFalse();
			matcher.Matches("barfoo").Should().BeFalse();
		}


		[Fact]
		public void TestMatchesInfix()
		{
			var matcher = WildcardMatcher.ValueOf("*foo*");


			matcher.Matches("foo").Should().BeTrue();
			matcher.Matches("foobar").Should().BeTrue();
			matcher.Matches("bar").Should().BeFalse();
			matcher.Matches("barfoo").Should().BeTrue();
			matcher.Matches("barfoobaz").Should().BeTrue();
		}

		[Fact]
		public void TestMatchesInfixPartitionedString_allocationFree()
		{
			var matcher = WildcardMatcher.ValueOf("*foo*");

			// no allocations necessary
			matcher.Matches("foo", "bar").Should().BeTrue();
			matcher.Matches("bar", "foo").Should().BeTrue();
			matcher.Matches("barfoo", "baz").Should().BeTrue();
			matcher.Matches("ba", "rfoo").Should().BeTrue();
		}

		[Fact]
		public void TestMatchesInfixPartitionedString_notAllocationFree()
		{
			var matcher = WildcardMatcher.ValueOf("*foo*");

			// requires concatenating the string
			matcher.Matches("fo", "o").Should().BeTrue();
			matcher.Matches("fo", null).Should().BeFalse();
			matcher.Matches("barfo", "obaz").Should().BeTrue();
			matcher.Matches("bar", "baz").Should().BeFalse();
		}

		[Fact]
		public void TestMatchesNoWildcard()
		{
			var matcher = WildcardMatcher.ValueOf("foo");

			// requires concatenating the string
			matcher.Matches("fo", "o").Should().BeTrue();
			matcher.Matches("foo").Should().BeTrue();
			matcher.Matches("foo", "bar").Should().BeFalse();
			matcher.Matches("foobar").Should().BeFalse();
		}

		[Fact]
		public void TestMatchAnyStartsWith()
		{
			var matcher1 = WildcardMatcher.ValueOf("foo*");
			var matcher2 = WildcardMatcher.ValueOf("bar*");

			WildcardMatcher.AnyMatch(new List<WildcardMatcher> { matcher1, matcher2 }, "foo").Should().Be(matcher1);
			WildcardMatcher.AnyMatch(new List<WildcardMatcher> { matcher1, matcher2 }, "bar").Should().Be(matcher2);
			WildcardMatcher.AnyMatch(new List<WildcardMatcher> { matcher1, matcher2 }, "baz").Should().BeNull();
			WildcardMatcher.AnyMatch(new List<WildcardMatcher> { matcher1, matcher2 }, "fo", "o").Should().Be(matcher1);
			WildcardMatcher.AnyMatch(new List<WildcardMatcher> { matcher1, matcher2 }, "ba", "r").Should().Be(matcher2);
			WildcardMatcher.AnyMatch(new List<WildcardMatcher> { matcher1, matcher2 }, "ba", "z").Should().BeNull();
		}

		[Fact]
		public void TestMatchesStartsWith_ignoreCase()
		{
			var matcher = WildcardMatcher.ValueOf("foo*");

			matcher.Matches("foo").Should().BeTrue();
			matcher.Matches("foobar").Should().BeTrue();
			matcher.Matches("bar").Should().BeFalse();
			matcher.Matches("barfoo").Should().BeFalse();
		}

		[Fact]
		public void TestInfixEmptyMatcher_ignoreCase()
		{
			var matcher = WildcardMatcher.ValueOf("**");

			matcher.Matches("").Should().BeTrue();
			matcher.Matches("foo").Should().BeTrue();
		}

		[Fact]
		public void TestMatchesPartitionedStringStartsWith_ignoreCase()
		{
			var matcher = WildcardMatcher.ValueOf("/foo/bar*");

			matcher.Matches("/foo/bAR/Baz", "").Should().BeTrue();
			matcher.Matches("", "/foo/bAR/baz").Should().BeTrue();
			matcher.Matches("/FOO/BAR", "/baz").Should().BeTrue();
			matcher.Matches("/foo", "/BAR/BAZ").Should().BeTrue();
			matcher.Matches("/FOO", "/bar/baz").Should().BeTrue();
			matcher.Matches("/BAR", "/BAR/BAZ").Should().BeFalse();
			matcher.Matches("/FOO", "/foo/baz").Should().BeFalse();
			matcher.Matches("/foo/FOO", "/BAZ").Should().BeFalse();
		}

		[Fact]
		public void TestMatchesEndsWith_ignoreCase()
		{
			var matcher = WildcardMatcher.ValueOf("*foo");

			matcher.Matches("fOo").Should().BeTrue();
			matcher.Matches("foobar").Should().BeFalse();
			matcher.Matches("bar").Should().BeFalse();
			matcher.Matches("baRFoo").Should().BeTrue();
		}

		[Fact]
		public void TestMatchesPartitionedStringEndsWith_ignoreCase()
		{
			var matcher = WildcardMatcher.ValueOf("*/bar/baz");

			matcher.Matches("/foO/BAR/Baz", "").Should().BeTrue();
			matcher.Matches("", "/foO/Bar/baz").Should().BeTrue();
			matcher.Matches("/FOo/bar", "/baz").Should().BeTrue();
			matcher.Matches("/foo", "/bar/BAZ").Should().BeTrue();
			matcher.Matches("/fOo", "/bAR/baz").Should().BeTrue();
			matcher.Matches("/bar", "/foO/baz").Should().BeFalse();
			matcher.Matches("/FOo", "/foo/baz").Should().BeFalse();
		}

		[Fact]
		public void TestMatchesEquals_ignoreCase()
		{
			var matcher = WildcardMatcher.ValueOf("foo");

			matcher.Matches("fOo").Should().BeTrue();
			matcher.Matches("foOBar").Should().BeFalse();
			matcher.Matches("BAR").Should().BeFalse();
			matcher.Matches("barfoo").Should().BeFalse();
		}

		[Fact]
		public void TestMatchesInfix_ignoreCase()
		{
			var matcher = WildcardMatcher.ValueOf("*foo*");
			matcher.Matches("FOO").Should().BeTrue();
			matcher.Matches("foOBar").Should().BeTrue();
			matcher.Matches("BAR").Should().BeFalse();
			matcher.Matches("baRFOo").Should().BeTrue();
			matcher.Matches("BARFOOBAZ").Should().BeTrue();
		}

		[Fact]
		public void TestMatchesInfix_caseSensitive()
		{
			var matcher = WildcardMatcher.ValueOf("(?-i)*foo*");

			matcher.Matches("foo").Should().BeTrue();
			matcher.Matches("FOO").Should().BeFalse();
		}

		[Fact]
		public void TestMatchesInfixPartitionedString_ignoreCase()
		{
			var matcher = WildcardMatcher.ValueOf("*foo*");

			// no allocations necessary
			matcher.Matches("foo", "BAR").Should().BeTrue();
			matcher.Matches("BAR", "foo").Should().BeTrue();
			matcher.Matches("baRFoo", "baz").Should().BeTrue();
			matcher.Matches("bA", "Rfoo").Should().BeTrue();
			matcher.Matches("fo", "O").Should().BeTrue();
			matcher.Matches("barFO", "obaz").Should().BeTrue();
			matcher.Matches("bar", "baz").Should().BeFalse();
		}

		[Fact]
		public void TestMatchesNoWildcard_ignoreCase()
		{
			var matcher = WildcardMatcher.ValueOf("foo");
			matcher.Matches("FO", "O").Should().BeTrue();
			matcher.Matches("FOO").Should().BeTrue();
			matcher.Matches("foO", "Bar").Should().BeFalse();
			matcher.Matches("foobar").Should().BeFalse();
		}

		[Fact]
		public void TestNeedleLongerThanHaystack()
		{
			WildcardMatcher.ValueOf("*foo").Matches("baz").Should().BeFalse();
			WildcardMatcher.ValueOf("*foob").Matches("baz").Should().BeFalse();
			WildcardMatcher.ValueOf("*fooba").Matches("baz").Should().BeFalse();
			WildcardMatcher.ValueOf("*foobar").Matches("baz").Should().BeFalse();
			WildcardMatcher.ValueOf("foo*").Matches("baz").Should().BeFalse();
			WildcardMatcher.ValueOf("foob*").Matches("baz").Should().BeFalse();
			WildcardMatcher.ValueOf("fooba*").Matches("baz").Should().BeFalse();
			WildcardMatcher.ValueOf("foobar*").Matches("baz").Should().BeFalse();
			WildcardMatcher.ValueOf("*foobar*").Matches("baz").Should().BeFalse();
		}
	}
}
