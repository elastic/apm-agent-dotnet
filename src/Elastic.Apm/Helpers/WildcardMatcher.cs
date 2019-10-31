using System;
using System.Collections.Generic;
using System.Linq;

namespace Elastic.Apm.Helpers
{
	/// <summary>
	/// Heavily inspired by the Java Elastic APM Agent.
	/// Wildcard matcher to e.g. sanitize strings.
	/// Examples: `/foo/*/bar/*/baz*`, `*foo*`.
	/// Matching is case insensitive by default.
	/// Prepending an element with `(?-i)` makes the matching case sensitive.
	/// </summary>
	public abstract class WildcardMatcher
	{
		private const string CaseInsensitivePrefix = "(?i)";

		private const string CaseSensitivePrefix = "(?-i)";

		private const string Wildcard = "*";

		public abstract string GetMatcher();

		/// <summary>
		/// Checks if the given string matches the wildcard pattern.
		/// </summary>
		/// <param name="s">The string to match</param>
		/// <returns>Whether the string matches the given pattern</returns>
		public abstract bool Matches(string s);

		/// <summary>
		/// This is a different version of <see cref="Matches(string)" /> which has the same semantics as calling
		/// <see cref="Matches(string)" param=" firstPart + secondPart" />.
		/// The difference is that this method does not allocate memory.
		/// </summary>
		/// <param name="firstPart">The first part of the string to match against.</param>
		/// <param name="secondPart">The second part of the string to match against.</param>
		/// <returns><code>true</code> when the wildcard pattern matches the partitioned string, <code>false</code> otherwise. </returns>
		internal abstract bool Matches(string firstPart, string secondPart);

		/// <summary>
		/// Constructs a new <see cref="WildcardMatcher" /> via a wildcard string.
		/// By default, matches are a case insensitive.
		/// It supports the <code>*</code> wildcard which matches zero or more characters.
		/// Prepend <code>(?-i)</code> to your pattern to make it case sensitive.
		/// Example: <code>(?-i)foo*</code> matches the string <code>foobar</code> but does not match <code>FOOBAR</code>.
		/// </summary>
		/// <param name="wildcardString"></param>
		/// <returns></returns>
		public static WildcardMatcher ValueOf(string wildcardString)
		{
			var matcher = wildcardString;
			var ignoreCase = true;
			if (matcher.StartsWith(CaseSensitivePrefix))
			{
				ignoreCase = false;
				matcher = matcher.Substring(CaseSensitivePrefix.Length);
			}
			else if (matcher.StartsWith(CaseInsensitivePrefix)) matcher = matcher.Substring(CaseInsensitivePrefix.Length);

			var split = matcher.Split('*');

			if (split.Length == 1)
			{
				if (!matcher.StartsWith(Wildcard) && !matcher.EndsWith(Wildcard))
					return new VerbatimMatcher(split[0], ignoreCase);
				return new SimpleWildcardMatcher(split[0], matcher.StartsWith(Wildcard), matcher.EndsWith(Wildcard), ignoreCase);
			}

			var matchers = new List<SimpleWildcardMatcher>(split.Length);
			for (var i = 0; i < split.Length; i++)
			{
				var isFirst = i == 0;
				var isLast = i == split.Length - 1;
				matchers.Add(new SimpleWildcardMatcher(split[i],
					!isFirst || matcher.StartsWith(Wildcard),
					!isLast || matcher.EndsWith(Wildcard),
					ignoreCase));
			}
			return new CompoundWildcardMatcher(matcher, matchers);
		}


		/// <summary>
		/// Returns <code>true</code>, if any of the matchers match the provided string.
		/// </summary>
		/// <param name="matchers">The matchers which should be used to match the provided string</param>
		/// <param name="s">The string to match against</param>
		/// <returns><code>true</code>, if any of the matchers match the provided string</returns>
		public static bool IsAnyMatch(IReadOnlyList<WildcardMatcher> matchers, string s) => AnyMatch(matchers, s) != null;

		/// <summary>
		/// Returns the first <see cref="WildcardMatcher" /> that matches the provided string.
		/// </summary>
		/// <param name="matchers"> The matchers which should be used to match the provided string</param>
		/// <param name="s">The string to match against</param>
		/// <returns>The first matching <see cref="WildcardMatcher" />, or <code>null</code> if none match.</returns>
		internal static WildcardMatcher AnyMatch(IReadOnlyList<WildcardMatcher> matchers, string s) => s == null ? null : AnyMatch(matchers, s, null);

		/// <summary>
		/// Returns the first <see cref="WildcardMatcher" /> that matches the provided partitioned string.
		/// </summary>
		/// <param name="matchers"> The matchers which should be used to match the provided string</param>
		/// <param name="firstPart"> The first part of the string to match against.</param>
		/// <param name="secondPart"> The second part of the string to match against.</param>
		/// <returns>The first matching <see cref="WildcardMatcher" />, or <code>null</code> if none match.</returns>
		internal static WildcardMatcher AnyMatch(IReadOnlyCollection<WildcardMatcher> matchers, string firstPart, string secondPart)
		{
			for (var i = 0; i < matchers.Count; i++)
			{
				if (matchers.ElementAt(i).Matches(firstPart, secondPart))
					return matchers.ElementAt(i);
			}

			return null;
		}

		/// <summary>
		/// Based on https://stackoverflow.com/a/29809553/1125055
		/// Thx to Zach Vorhies
		/// </summary>
		/// <param name="haystack1"></param>
		/// <param name="haystack2"></param>
		/// <param name="needle"></param>
		/// <param name="ignoreCase"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		internal static int IndexOfIgnoreCase(string haystack1, string haystack2, string needle, bool ignoreCase, int start, int end)
		{
			if (start < 0) return -1;

			var totalHaystackLength = haystack1.Length + haystack2.Length;
			if (needle.IsEmpty() || totalHaystackLength == 0)
			{
				// Fallback to legacy behavior.
				return haystack1.IndexOf(needle, StringComparison.Ordinal);
			}

			var haystack1Length = haystack1.Length;
			var needleLength = needle.Length;
			for (var i = start; i < end; i++)
			{
				// Early out, if possible.
				if (i + needleLength > totalHaystackLength) return -1;

				// Attempt to match substring starting at position i of haystack.
				var j = 0;
				var ii = i;
				while (ii < totalHaystackLength && j < needleLength)
				{
					var c = ignoreCase
						? char.ToLowerInvariant(CharAt(ii, haystack1, haystack2, haystack1Length))
						: CharAt(ii, haystack1, haystack2, haystack1Length);
					var c2 = ignoreCase ? char.ToLowerInvariant(needle.ElementAt(j)) : needle.ElementAt(j);
					if (c != c2) break;

					j++;
					ii++;
				}
				// Walked all the way to the end of the needle, return the start
				// position that this was found.
				if (j == needleLength) return i;
			}

			return -1;
		}

		internal static char CharAt(int i, string firstPart, string secondPart, int firstPartLength) =>
			i < firstPartLength ? firstPart.ElementAt(i) : secondPart.ElementAt(i - firstPartLength);

		/// <summary>
		/// This <see cref="WildcardMatcher" /> supports wildcards in the middle of the matcher by decomposing the matcher into
		/// several
		/// <see cref="WildcardMatcher.SimpleWildcardMatcher" />.
		/// </summary>
		internal class CompoundWildcardMatcher : WildcardMatcher
		{
			private readonly string _matcher;
			private readonly List<SimpleWildcardMatcher> _wildcardMatchers;

			public CompoundWildcardMatcher(string matcher, List<SimpleWildcardMatcher> wildcardMatchers)
			{
				_matcher = matcher;
				_wildcardMatchers = wildcardMatchers;
			}

			public override bool Matches(string s)
			{
				var offset = 0;
				for (var i = 0; i < _wildcardMatchers.Count(); i++)
				{
					var matcher = _wildcardMatchers.ElementAt(i);
					offset = matcher.IndexOf(s, offset);
					if (offset == -1) return false;

					offset += matcher.Matcher.Length;
				}
				return true;
			}

			internal override bool Matches(string firstPart, string secondPart)
			{
				var offset = 0;
				for (var i = 0; i < _wildcardMatchers.Count; i++)
				{
					var wildcardMatcher = _wildcardMatchers.ElementAt(i);
					offset = wildcardMatcher.IndexOf(firstPart, secondPart, offset);
					if (offset == -1) return false;

					offset += wildcardMatcher.Matcher.Length;
				}
				return true;
			}

			public override string GetMatcher() => _matcher;
		}

		internal class VerbatimMatcher : WildcardMatcher
		{
			private readonly string _matcher;
			private readonly bool _ignoreCase;

			public VerbatimMatcher(string s, bool ignoreCase)
			{
				_matcher = s;
				_ignoreCase = ignoreCase;
			}

			public override string GetMatcher() => _matcher;

			public override bool Matches(string s) => string.Equals(_matcher, s,
				_ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

			internal override bool Matches(string firstPart, string secondPart) => string.Equals(_matcher, firstPart + secondPart,
				_ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}

		/// <summary>
		/// This <see cref="WildcardMatcher" /> does not support wildcards in the middle of a matcher.
		/// </summary>
		internal class SimpleWildcardMatcher : WildcardMatcher
		{
			public readonly string Matcher;

			private readonly bool _ignoreCase;

			private readonly bool _wildcardAtBeginning;

			private readonly bool _wildcardAtEnd;

			public SimpleWildcardMatcher(string matcher, bool wildcardAtBeginning, bool wildcardAtEnd, bool ignoreCase)
			{
				Matcher = matcher;
				_wildcardAtEnd = wildcardAtEnd;
				_wildcardAtBeginning = wildcardAtBeginning;
				_ignoreCase = ignoreCase;
			}

			public override bool Matches(string s) => IndexOf(s, 0) != -1;

			internal override bool Matches(string firstPart, string secondPart) => IndexOf(firstPart, secondPart, 0) != -1;

			public int IndexOf(string s, int offset) => IndexOf(s, "", offset);

			public int IndexOf(string firstPart, string secondPart, int offset)
			{
				if (secondPart == null) secondPart = "";
				var totalLength = firstPart.Length + secondPart.Length;
				if (_wildcardAtEnd && _wildcardAtBeginning)
					return IndexOfIgnoreCase(firstPart, secondPart, Matcher, _ignoreCase, offset, totalLength);

				if (_wildcardAtEnd)
					return IndexOfIgnoreCase(firstPart, secondPart, Matcher, _ignoreCase, 0, 1);

				if (_wildcardAtBeginning)
					return IndexOfIgnoreCase(firstPart, secondPart, Matcher, _ignoreCase, totalLength - Matcher.Length, totalLength);
				if (totalLength == Matcher.Length)
					return IndexOfIgnoreCase(firstPart, secondPart, Matcher, _ignoreCase, 0, totalLength);

				return -1;
			}

			public override string GetMatcher() => Matcher;
		}
	}
}
