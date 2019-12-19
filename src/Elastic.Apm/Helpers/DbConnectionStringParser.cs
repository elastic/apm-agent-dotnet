using System;
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
		private readonly ThreadLocal<Dictionary<string, Destination>> _cache =
			new ThreadLocal<Dictionary<string, Destination>>(() => new Dictionary<string, Destination>());

		internal DbConnectionStringParser(IApmLogger logger)
		{
			_logger = logger.Scoped(ThisClassName);
		}

		/// <returns><c>Destination</c> if successful and <c>null</c> otherwise</returns>
		internal Destination TryExtractDestination(string dbConnectionString) => TryExtractDestination(dbConnectionString, out _);

		/// <summary>
		/// Used only by tests.
		/// </summary>
		internal Destination TryExtractDestination(string dbConnectionString, out bool wasFoundInCache)
		{
			var cache = _cache.Value;
			if (cache.TryGetValue(dbConnectionString, out var destination))
			{
				wasFoundInCache = true;
				return destination;
			}

			wasFoundInCache = false;
			destination = ParseConnectionString(dbConnectionString);
			if (cache.Count < MaxCacheSize) cache.Add(dbConnectionString, destination);
			return destination;
		}

		private static readonly Dictionary<string, Action<string, Destination>> KeyToPropertySetter =
			new Dictionary<string, Action<string, Destination>>(StringComparer.OrdinalIgnoreCase)
			{
				{ "Server" , ParseServerValue },
				{ "Data Source" , ParseServerValue },
				{ "Host" , ParseServerValue },
				{ "Hostname" , ParseServerValue },
				{ "Network Address" , ParseServerValue },
				{ "dbq" , ParseServerValue },
				{ "Port" , ParsePortValue }
			};

		private const char KeyValuePairsSeparator = ';';
		private const char KeyValueSeparator = '=';
		private const char ServerNameDbInstanceSeparator = '\\';
		private const string SqlServerLocalDbPrefix = "(LocalDB)";
		private const string SqlServerExpressUserInstancePrefix = @".\";
		private const string SqlAzurePrefix = @"tcp:";
		private const string OracleXeClientSuffix = @"/XE";
		private static readonly IEnumerable<string> DiscardablePrefixes = new List<string>
		{
			SqlAzurePrefix
		};
		private static readonly IEnumerable<string> DiscardableSuffixes = new List<string>
		{
			OracleXeClientSuffix
		};

		/// <returns><c>Destination</c> if successful and <c>null</c> otherwise</returns>
		private Destination ParseConnectionString(string dbConnectionString)
		{
			Destination result = null;
			foreach (var keyValueString in dbConnectionString.Split(KeyValuePairsSeparator))
			{
				if (string.IsNullOrWhiteSpace(keyValueString)) continue;

				var keyValueArray = keyValueString.Split(KeyValueSeparator);
				if (keyValueArray.Length != 2)
				{
					_logger.Trace()?.Log("Encountered invalid key-value pair - skipping it."
						+ " keyValueString: `{KeyValueString}'. keyValueArray: {KeyValueArray}. dbConnectionString: {DbConnectionString}."
						, keyValueString, keyValueArray, dbConnectionString);
					continue;
				}

				if (! KeyToPropertySetter.TryGetValue(keyValueArray[0].Trim(), out var propSetter)) continue;

				if (result == null) result = new Destination();

				try
				{
					propSetter(keyValueArray[1].Trim(), result);
				}
				catch (Exception ex)
				{
					_logger.Trace()?.LogException(ex, "Encountered invalid value for a known key"
						+ " - considering the whole connection string as invalid and returning null."
						+ " keyValueString: `{KeyValueString}'. keyValueArray: {KeyValueArray}. dbConnectionString: {DbConnectionString}."
						, keyValueString, keyValueArray, dbConnectionString);
					return null;
				}
			}

			return result;
		}

		private static void ParseServerValue(string valueToParseArg, Destination destination)
		{
			var valueToParse = TrimDiscardable(valueToParseArg);

			if (valueToParse.StartsWith(SqlServerLocalDbPrefix, StringComparison.OrdinalIgnoreCase)
				|| valueToParse.StartsWith(SqlServerExpressUserInstancePrefix, StringComparison.OrdinalIgnoreCase))
			{
				destination.Address = "localhost";
				return;
			}

			var dbInstanceSeparatorIndex = valueToParse.IndexOf(ServerNameDbInstanceSeparator);
			if (dbInstanceSeparatorIndex != -1) valueToParse = valueToParse.Substring(0, dbInstanceSeparatorIndex);
			ParseServerWithOptionalPort(valueToParse, destination);
		}

		private static string TrimDiscardable(string valueToParse)
		{
			var currentResult = valueToParse;

			foreach (var discardablePrefix in DiscardablePrefixes)
			{
				if (currentResult.StartsWith(discardablePrefix, StringComparison.OrdinalIgnoreCase))
					currentResult = currentResult.Substring(discardablePrefix.Length);
			}

			foreach (var discardableSuffix in DiscardableSuffixes)
			{
				if (currentResult.EndsWith(discardableSuffix, StringComparison.OrdinalIgnoreCase))
					currentResult = currentResult.Substring(0, currentResult.Length - discardableSuffix.Length);
			}

			return currentResult;
		}

		private static void ParseServerWithOptionalPort(string valueToParseArg, Destination destination)
		{
			// Possible values:
			// 		- Name/IPv4 with/without port: "Name_or_IPv4_address", "Name_or_IPv4_address:port", "Name_or_IPv4_address,port"
			// 		- IPv6 with/without port: "IPv6_address", "[IPv6_address]", "[IPv6_address]:port", "IPv6_address,port" or even "[IPv6_address],port"

			var valueToParse = valueToParseArg.Trim();
			if (valueToParse.IsEmpty())
				throw new FormatException($"Server address part is white space only/empty string. valueToParseArg: `{valueToParseArg}'.");

			var commaIndex = valueToParse.IndexOf(',');
			if (commaIndex != -1)
			{
				// Server part has comma which means it's "Name_or_IPv4_address,port", "IPv6_address,port" or "[IPv6_address],port"
				destination.Address = ParseAddress(valueToParse.Substring(0, commaIndex));
				ParsePortValue(valueToParse.Substring(commaIndex + 1), destination);
				return;
			}

			var firstColumnIndex = valueToParse.IndexOf(':');
			if (firstColumnIndex == -1)
			{
				// Server part doesn't have even one column which means it's "Name_or_IPv4_address"
				destination.Address = valueToParse.Trim();
				return;
			}

			var lastColumnIndex = valueToParse.LastIndexOf(':');
			if (firstColumnIndex == lastColumnIndex)
			{
				// Server part has just one column which means it's "Name_or_IPv4_address:port"
				destination.Address = ParseAddress(valueToParse.Substring(0, firstColumnIndex));
				ParsePortValue(valueToParse.Substring(firstColumnIndex + 1), destination);
				return;
			}

			// Server part has more than one column which means it's "IPv6_address", "[IPv6_address]" or "[IPv6_address]:port"

			if (valueToParse[0] != '[')
			{
				// Server part doesn't start with '[' which means it's "IPv6_address"
				destination.Address = valueToParse;
				return;
			}

			// Server part starts with '[' which means it's "[IPv6_address]" or "[IPv6_address]:port"

			if (valueToParse[valueToParse.Length - 1] == ']')
			{
				// Server part ends with ']' which means it's "[IPv6_address]"
				destination.Address = ParseAddress(valueToParse);
				return;
			}

			// Server part doesn't end with ']' which means it's "[IPv6_address]:port"

			destination.Address = ParseAddress(valueToParse.Substring(0, lastColumnIndex));
			ParsePortValue(valueToParse.Substring(lastColumnIndex + 1), destination);
			return;

		}

		private static string ParseAddress(string valueToParseArg)
		{
			// Possible values: "Name_or_IPv4_address", "IPv6_address", "[IPv6_address]"

			var valueToParse = valueToParseArg.Trim();
			if (valueToParse.IsEmpty())
				throw new FormatException($"Server address part is white space only/empty string. valueToParseArg: `{valueToParseArg}'.");

			var startIndex = valueToParse[0] == '[' ? 1 : 0;
			var endIndex = valueToParse[valueToParse.Length - 1] == ']' ? valueToParse.Length - 1 : valueToParse.Length;

			return valueToParse.Substring(startIndex, endIndex - startIndex).Trim();
		}

		private static void ParsePortValue(string valueToParse, Destination destination)
		{
			if (string.IsNullOrWhiteSpace(valueToParse))
				throw new FormatException($"Port part of server value is white space only/empty string. valueToParse: `{valueToParse}'.");

			if (! int.TryParse(valueToParse, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
				throw new FormatException($"Failed to parse port part of server value. valueToParse: `{valueToParse}'.");

			if (port < 0)
				throw new FormatException($"Port part of server value is a negative integer. port: {port}. valueToParse: `{valueToParse}'.");

			destination.Port = port;
		}
	}
}
