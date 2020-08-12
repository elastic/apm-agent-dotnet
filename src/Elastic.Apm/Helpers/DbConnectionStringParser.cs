// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal class DbConnectionStringParser
	{
		private const string ThisClassName = nameof(DbConnectionStringParser);
		internal const int MaxCacheSize = 100;

		private readonly IApmLogger _logger;
		private readonly ConcurrentDictionary<string, Destination> _cache = new ConcurrentDictionary<string, Destination>();
		// We keep count for _cache because ConcurrentDictionary.Count is very heavy operation
		// (for example see https://github.com/dotnet/corefx/issues/3357)
		private volatile int _cacheCount;

		internal const int MaxNestingDepth = 100;

		internal DbConnectionStringParser(IApmLogger logger) => _logger = logger.Scoped(ThisClassName);

		/// <returns><c>Destination</c> if successful and <c>null</c> otherwise</returns>
		internal Destination ExtractDestination(string dbConnectionString) => ExtractDestination(dbConnectionString, out _);

		/// <summary>
		/// Used only by tests.
		/// </summary>
		internal Destination ExtractDestination(string dbConnectionString, out bool wasFoundInCache)
		{
			if (_cache.TryGetValue(dbConnectionString, out var destination))
			{
				wasFoundInCache = true;
				return destination;
			}

			wasFoundInCache = false;
			destination = ParseConnectionString(dbConnectionString);
			if (_cacheCount < MaxCacheSize && _cache.TryAdd(dbConnectionString, destination)) Interlocked.Increment(ref _cacheCount);
			return destination;
		}

		/// <returns><c>Destination</c> if successful and <c>null</c> otherwise</returns>
		private Destination ParseConnectionString(string dbConnectionString)
		{
			var destination = new Destination();

			try
			{
				new ParserWithState(_logger, destination).ParseFlatKeyValuePairs(dbConnectionString);
			}
			catch (Exception ex)
			{
				_logger.Trace()?.LogException(ex, "Encountered an issue while parsing"
					+ " - considering the whole connection string as invalid and returning null."
					+ " dbConnectionString: {DbConnectionString}."
					, dbConnectionString);
				return null;
			}

			// Check if all the mandatory parts are found
			// ReSharper disable once InvertIf
			if (!destination.AddressHasValue)
			{
				_logger.Trace()?.Log("Parsing did not find address part of destination (which is mandatory)"
					+ " - considering the whole connection string as invalid and returning null."
					+ " dbConnectionString: {DbConnectionString}."
					, dbConnectionString);
				return null;
			}

			return destination;
		}

		private class ParserWithState
		{
			// ReSharper disable once MemberHidesStaticFromOuterClass
			private const string ThisClassName = DbConnectionStringParser.ThisClassName + "." + nameof(ParserWithState);

			private readonly IApmLogger _logger;
			private readonly Destination _destination;
			private readonly Dictionary<string, Action<string>> _keyToPropertySetter;
			private int _currentNestingDepth;

			private const char NestedOpeningDelimiter = '(';
			private const char NestedClosingDelimiter = ')';
			private static readonly char[] NestedDelimiters = { NestedOpeningDelimiter, NestedClosingDelimiter };
			private const char FlatKeyValuePairsSeparator = ';';
			private const char KeyValueSeparator = '=';
			private const char ServerNameDbInstanceSeparator = '\\';
			private const string SqlServerLocalDbPrefix = "(LocalDB)";
			private const string SqlServerExpressUserInstancePrefix = @".\";
			private const string SqlAzurePrefix = @"tcp:";
			private static readonly IEnumerable<string> DiscardablePrefixes = new List<string>
			{
				SqlAzurePrefix
			};

			internal ParserWithState(IApmLogger logger, Destination destination)
			{
				_logger = logger.Scoped(ThisClassName);
				_destination = destination;
				_keyToPropertySetter = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
				{
					{ "Server" , ParseServerValue },
					{ "Data Source" , ParseServerValue },
					{ "Host" , ParseServerValue },
					{ "Hostname" , ParseServerValue },
					{ "Network Address" , ParseServerValue },
					{ "dbq" , ParseServerValue },
					{ "Port" , ParsePortValue },
					{ "Address" , ParseAddressKeyWithNestedStructureValue },
				};
			}

			internal void ParseFlatKeyValuePairs(string flatKeyValuePairs)
			{
				foreach (var keyValue in flatKeyValuePairs.Split(FlatKeyValuePairsSeparator))
					ParseKeyValue(keyValue);
			}

			private void ParseKeyValue(string keyValue)
			{
				if (string.IsNullOrWhiteSpace(keyValue)) return;

				var keyValueSplit = keyValue.Split(new[] { KeyValueSeparator }, 2);
				if (keyValueSplit.Length < 2)
				{
					_logger.Trace()?.Log("Encountered key-value pair without value - skipping it."
						+ " keyValue: `{KeyValueString}'. keyValueSplit: {KeyValueSplit}."
						, keyValue, keyValueSplit);
					return;
				}

				var key = keyValueSplit[0].Trim();
				var value = keyValueSplit[1].Trim();

				// Skip unknown keys
				if (!_keyToPropertySetter.TryGetValue(key, out var valueParser))
				{
					if (!HasNestedStructure(value)) return;

					// Unless value for an unknown key has nested structure
					// then we want to go into the value to see if we find known keys
					ParseNestedStructure(value);
					return;
				}

				try
				{
					valueParser(keyValueSplit[1].Trim());
				}
				catch (FormatException)
				{
					throw;
				}
				catch (Exception ex)
				{
					throw new FormatException(
						"Encountered invalid value for a known key."
						+ $" keyValueSplit: {keyValueSplit}."
						+ $" keyValue: `{keyValue}'."
						, ex);
				}
			}

			private static bool HasNestedStructure(string value) =>
				value.Length >= 2
				&& value[0] == NestedOpeningDelimiter && value[value.Length - 1] == NestedClosingDelimiter;

			private static int FindMatchingClosingDelimiter(string str, int openingDelimiterIndex)
			{
				Assertion.IfEnabled?.That(str[openingDelimiterIndex] == NestedOpeningDelimiter,
					"This method should called only on values with nested structure." +
					$" String: `{str}'." +
					$" openingDelimiterIndex: {openingDelimiterIndex}.");

				var nestingDepth = 1;
				var matchingClosingDelimiterIndex = -1;
				var currentDelimiterIndex = openingDelimiterIndex;
				while (true)
				{
					if (currentDelimiterIndex == str.Length - 1) break;

					currentDelimiterIndex = str.IndexOfAny(NestedDelimiters, currentDelimiterIndex + 1);
					if (currentDelimiterIndex == -1) break;

					if (str[currentDelimiterIndex] == NestedOpeningDelimiter)
					{
						++nestingDepth;
						continue;
					}

					--nestingDepth;
					if (nestingDepth != 0) continue;

					matchingClosingDelimiterIndex = currentDelimiterIndex;
					break;
				}

				if (matchingClosingDelimiterIndex == -1)
				{
					throw new FormatException(
						"Opening and closing delimiters are not balanced."
						+ $" String: `{str}'."
						+ $" openingDelimiterIndex: {openingDelimiterIndex}.");
				}

				return matchingClosingDelimiterIndex;
			}

			private void ParseNestedStructure(string value)
			{
				Assertion.IfEnabled?.That(HasNestedStructure(value),
					$"This method should called only on values with nested structure. Provided value: `{value}'.");

				if (_currentNestingDepth + 1 > MaxNestingDepth) return;
				++_currentNestingDepth;

				var currentSubKeyValueOpenDelimiterIndex = 0;
				while (true)
				{
					var currentSubKeyValueCloseDelimiterIndex = FindMatchingClosingDelimiter(value, currentSubKeyValueOpenDelimiterIndex);
					ParseKeyValue(value.Substring(
						currentSubKeyValueOpenDelimiterIndex + 1,
						currentSubKeyValueCloseDelimiterIndex - (currentSubKeyValueOpenDelimiterIndex + 1)));

					if (currentSubKeyValueCloseDelimiterIndex == value.Length - 1) break;

					currentSubKeyValueOpenDelimiterIndex =
						value.IndexOf(NestedOpeningDelimiter, currentSubKeyValueCloseDelimiterIndex + 1);
					if (currentSubKeyValueOpenDelimiterIndex == -1) break;
				}

				--_currentNestingDepth;
			}

			// @"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=4321)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;"
			//                                                   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
			private void ParseAddressKeyWithNestedStructureValue(string value)
			{
				// If we already found address part of destination we don't need to parse anymore
				if (_destination.AddressHasValue) return;

				if (!HasNestedStructure(value))
				{
					throw new FormatException(
						"Value for `Address' key is expected to have nested structure."
						+ $" valueToParse: `{value}'.");
				}

				ParseNestedStructure(value);
			}

			private void ParseServerValue(string valueArg)
			{
				// Some DB connection string formats allow multiple addresses - we use only the first one
				if (_destination.AddressHasValue) return;

				var value = TrimDiscardable(valueArg);

				if (value.StartsWith(SqlServerLocalDbPrefix, StringComparison.OrdinalIgnoreCase)
					|| value.StartsWith(SqlServerExpressUserInstancePrefix, StringComparison.OrdinalIgnoreCase))
				{
					_destination.Address = "localhost";
					return;
				}

				if (HasNestedStructure(value))
				{
					ParseNestedStructure(value);
					return;
				}

				var dbInstanceSeparatorIndex = value.IndexOf(ServerNameDbInstanceSeparator);
				if (dbInstanceSeparatorIndex != -1) value = value.Substring(0, dbInstanceSeparatorIndex);
				ParseServerWithOptionalPort(value);
			}

			private static string TrimDiscardable(string value)
			{
				var currentResult = value;

				foreach (var discardablePrefix in DiscardablePrefixes)
				{
					if (currentResult.StartsWith(discardablePrefix, StringComparison.OrdinalIgnoreCase))
						currentResult = currentResult.Substring(discardablePrefix.Length);
				}

				currentResult = TrimDiscardableSuffix(currentResult);

				return currentResult;
			}

			private static string TrimDiscardableSuffix(string value)
			{
				// Trim suffix /xyz for connection strings such as:
				//
				// 		Driver=(Oracle in XEClient);dbq=111.21.31.99:4321/XE;Uid=myUsername;Pwd=myPassword;
				//			`/XE' suffix should be removed
				//
				//		DATA SOURCE=192.168.0.151:1521/ORCL;PASSWORD=xxx;PERSIST SECURITY INFO=True;USER ID=xxx
				//			`/ORCL' suffix should be removed
				//
				// To generalize we should trim any /<xyz> suffix if <xyz> is letters only string
				// but not a valid hex number because according to https://en.wikipedia.org/wiki/IPv6_address#Special_addresses
				// IPv6 can contain `/<hex number>' and we don't want to discard a part of the address

				var lastSlashIndex = value.LastIndexOf('/');
				if (lastSlashIndex == -1) return value;

				var foundNonHexDigit = false;
				for (var i = lastSlashIndex + 1 ; i < value.Length ; ++i )
				{
					if (! TextUtils.IsLatinLetter(value[i])) return value;
					if (!foundNonHexDigit && !TextUtils.IsHex(value[i])) foundNonHexDigit = true;
				}

				// If the suffix part after / is not empty and it's a valid hex number - we don't want to trim it
				if (lastSlashIndex < value.Length - 1 && !foundNonHexDigit) return value;

				return value.Substring(0, lastSlashIndex);
			}

			private void ParseServerWithOptionalPort(string valueArg)
			{
				// Possible values:
				// 		- Name/IPv4 with/without port: "Name_or_IPv4_address", "Name_or_IPv4_address:port", "Name_or_IPv4_address,port"
				// 		- IPv6 with/without port: "IPv6_address", "[IPv6_address]", "[IPv6_address]:port", "IPv6_address,port" or even "[IPv6_address],port"

				var value = valueArg.Trim();
				if (value.IsEmpty())
					throw new FormatException($"Server address part is white space only/empty string. valueToParseArg: `{valueArg}'.");

				var commaIndex = value.IndexOf(',');
				if (commaIndex != -1)
				{
					// Server part has comma which means it's "Name_or_IPv4_address,port", "IPv6_address,port" or "[IPv6_address],port"
					_destination.Address = ParseAddress(value.Substring(0, commaIndex));
					ParsePortValue(value.Substring(commaIndex + 1));
					return;
				}

				var firstColumnIndex = value.IndexOf(':');
				if (firstColumnIndex == -1)
				{
					// Server part doesn't have even one column which means it's "Name_or_IPv4_address"
					_destination.Address = value.Trim();
					return;
				}

				var lastColumnIndex = value.LastIndexOf(':');
				if (firstColumnIndex == lastColumnIndex)
				{
					// Server part has just one column which means it's "Name_or_IPv4_address:port"
					_destination.Address = ParseAddress(value.Substring(0, firstColumnIndex));
					ParsePortValue(value.Substring(firstColumnIndex + 1));
					return;
				}

				// Server part has more than one column which means it's "IPv6_address", "[IPv6_address]" or "[IPv6_address]:port"

				if (value[0] != '[')
				{
					// Server part doesn't start with '[' which means it's "IPv6_address"
					_destination.Address = value;
					return;
				}

				// Server part starts with '[' which means it's "[IPv6_address]" or "[IPv6_address]:port"

				if (value[value.Length - 1] == ']')
				{
					// Server part ends with ']' which means it's "[IPv6_address]"
					_destination.Address = ParseAddress(value);
					return;
				}

				// Server part doesn't end with ']' which means it's "[IPv6_address]:port"

				_destination.Address = ParseAddress(value.Substring(0, lastColumnIndex));
				ParsePortValue(value.Substring(lastColumnIndex + 1));
			}

			private static string ParseAddress(string valueArg)
			{
				// Possible values: "Name_or_IPv4_address", "IPv6_address", "[IPv6_address]"

				var value = valueArg.Trim();
				if (value.IsEmpty())
					throw new FormatException($"Server address part is white space only/empty string. valueToParseArg: `{valueArg}'.");

				var startIndex = value[0] == '[' ? 1 : 0;
				var endIndex = value[value.Length - 1] == ']' ? value.Length - 1 : value.Length;

				return value.Substring(startIndex, endIndex - startIndex).Trim();
			}

			private void ParsePortValue(string valueToParse)
			{
				if (string.IsNullOrWhiteSpace(valueToParse))
					throw new FormatException($"Port part of server value is white space only/empty string. valueToParse: `{valueToParse}'.");

				if (! int.TryParse(valueToParse, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
					throw new FormatException($"Failed to parse port part of server value. valueToParse: `{valueToParse}'.");

				if (port < 0)
					throw new FormatException($"Port part of server value is a negative integer. port: {port}. valueToParse: `{valueToParse}'.");

				_destination.Port = port;
			}
		}
	}
}
