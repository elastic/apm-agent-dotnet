// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Elastic.Apm.Logging
{
	/// <summary>
	/// Formatter to convert the named format items like {NamedformatItem} to <see cref="M:string.Format" /> format.
	/// </summary>
	internal class LogValuesFormatter
	{
		private readonly string _scope;
		private const string NullValue = "(null)";
		private static readonly object[] EmptyArray = new object[0];
		private static readonly char[] FormatDelimiters = { ',', ':' };
		private readonly string _format;

		public LogValuesFormatter(string format, string scope = null)
		{
			_scope = scope;
			OriginalFormat = format;

			var sb = new StringBuilder();
			var scanIndex = 0;
			var endIndex = format.Length;

			while (scanIndex < endIndex)
			{
				var openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
				var closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);

				if (closeBraceIndex == endIndex)
				{
					sb.Append(format, scanIndex, endIndex - scanIndex);
					scanIndex = endIndex;
				}
				else
				{
					// Format item syntax : { index[,alignment][ :formatString] }.
					var formatDelimiterIndex = FindIndexOfAny(format, FormatDelimiters, openBraceIndex, closeBraceIndex);

					sb.Append(format, scanIndex, openBraceIndex - scanIndex + 1);
					sb.Append((ValueNames.Count).ToString(CultureInfo.InvariantCulture));
					ValueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
					sb.Append(format, formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);

					scanIndex = closeBraceIndex + 1;
				}
			}

			_format = sb.ToString();
		}

		public string OriginalFormat { get; private set; }
		public List<string> ValueNames { get; } = new List<string>();

		private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
		{
			// Example: {{prefix{{{Argument}}}suffix}}.
			var braceIndex = endIndex;
			var scanIndex = startIndex;
			var braceOccurenceCount = 0;

			while (scanIndex < endIndex)
			{
				if (braceOccurenceCount > 0 && format[scanIndex] != brace)
				{
					if (braceOccurenceCount % 2 == 0)
					{
						// Even number of '{' or '}' found. Proceed search with next occurence of '{' or '}'.
						braceOccurenceCount = 0;
						braceIndex = endIndex;
					}
					else
					{
						// An unescaped '{' or '}' found.
						break;
					}
				}
				else if (format[scanIndex] == brace)
				{
					if (brace == '}')
					{
						if (braceOccurenceCount == 0)
						{
							// For '}' pick the first occurence.
							braceIndex = scanIndex;
						}
					}
					else
					{
						// For '{' pick the last occurence.
						braceIndex = scanIndex;
					}

					braceOccurenceCount++;
				}

				scanIndex++;
			}

			return braceIndex;
		}

		private static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
		{
			var findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
			return findIndex == -1 ? endIndex : findIndex;
		}

		public string Format(object[] values)
		{
			if (_scope != null) return Format(_scope, values);

			values = values ?? EmptyArray;

			for (var i = 0; i < values.Length; i++)
				values[i] = FormatArgument(values[i]);

			return string.Format(CultureInfo.InvariantCulture, _format, values);
		}
		private string Format(string scope, object[] values)
		{
			values = values ?? EmptyArray;
			var args = new object[values.Length + 1];
			args[0] = scope;

			for (var i = 0; i < values.Length; i++)
				args[i+1] = FormatArgument(values[i]);

			return string.Format(CultureInfo.InvariantCulture, _format, args);
		}

		private object FormatArgument(object value)
		{
			if (value == null) return NullValue;

			// since 'string' implements IEnumerable, special case it
			if (value is string) return value;

			// if the value implements IEnumerable, build a comma separated string.
			var enumerable = value as IEnumerable;
			if (enumerable != null) return string.Join(", ", enumerable.Cast<object>().Select(o => o ?? NullValue));

			return value;
		}

		public LogValues GetState(object[] values)
		{
			values = values ?? EmptyArray;
			var offset = (_scope == null ? 1 : 2);
			var args = new KeyValuePair<string, object>[values.Length + offset];
			args[0] = new KeyValuePair<string, object>("{OriginalFormat}", OriginalFormat);
			if (_scope != null)
				args[1] = new KeyValuePair<string, object>("{Scope}", _scope);

			for (int i = 0, j = (_scope != null ? 1 : 0); j < ValueNames.Count; i++, j++)
				args[offset + i] = new KeyValuePair<string, object>(ValueNames[j], values[i]);

			return new LogValues(args);
		}

		public class LogValues : ReadOnlyCollection<KeyValuePair<string, object>>
		{
			public LogValues(IList<KeyValuePair<string, object>> list) : base(list) { }
		}

	}
}
