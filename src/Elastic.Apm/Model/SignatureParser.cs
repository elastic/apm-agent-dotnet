// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Text;

namespace Elastic.Apm.Model
{
	internal class SignatureParser
	{
		/// <summary>
		/// If the cache reaches this size we assume that the application creates a lot of dynamic queries.
		/// In that case it's inefficient to try to cache these as they are not likely to be repeated.
		/// But we still pay the price of allocating a Map.Entry and a String for the signature.
		/// </summary>
		private const int DisableCacheThreshold = 512;

		/// <summary>
		/// The cache management overhead is probably not worth it for short queries
		/// </summary>
		private const int QueryLengthCacheLowerThreshold = 64;

		/// <summary>
		/// We don't want to keep alive references to huge query strings
		/// </summary>
		private static readonly int QueryLengthCacheUpperThreshold = 10_000;

		private readonly Scanner _scanner;

		/// <summary>
		/// Not using weak keys because ORMs like Hibernate generate equal SQL strings for the same query but don't reuse the same string instance.
		/// When relying on weak keys, we would not leverage any caching benefits if the query string is collected.
		/// That means that we are leaking Strings but as the size of the map is limited that should not be an issue.
		/// </summary>
		private readonly ConcurrentDictionary<string, string[]>
			_signatureCache = new();

		public SignatureParser(Scanner scanner) => _scanner = scanner;

		public void QuerySignature(string query, StringBuilder signature, bool preparedStatement) =>
			QuerySignature(query, signature, null, preparedStatement);

		public void QuerySignature(string query, StringBuilder signature, StringBuilder dbLink, bool preparedStatement)
		{
			var cacheable = preparedStatement // non-prepared statements are likely to be dynamic strings
				&& QueryLengthCacheLowerThreshold < query.Length
				&& query.Length < QueryLengthCacheUpperThreshold;
			if (cacheable)
			{
				if (_signatureCache.TryGetValue(query, out var cachedSignature))
				{
					if (cachedSignature != null)
					{
						signature.Append(cachedSignature[0]);
						if (dbLink != null)
							dbLink.Append(cachedSignature[1]);
						return;
					}
				}
			}
			var scanner = _scanner;
			scanner.SetQuery(query);
			Parse(scanner, query, signature, dbLink);

			if (cacheable && _signatureCache.Count <= DisableCacheThreshold)
			{
				// we don't mind a small overshoot due to race conditions
				_signatureCache[query] = new[] { signature.ToString(), dbLink != null ? dbLink.ToString() : "" };
			}
		}

		private void Parse(Scanner scanner, string query, StringBuilder signature, StringBuilder dbLink)
		{
			var firstToken = scanner.ScanWhile(Scanner.Token.Comment);
			switch (firstToken)
			{
				case Scanner.Token.Call:
					signature.Append("CALL");
					if (scanner.ScanUntil(Scanner.Token.Ident)) AppendIdentifiers(scanner, signature, dbLink);
					return;
				case Scanner.Token.Delete:
					signature.Append("DELETE");
					if (scanner.ScanUntil(Scanner.Token.From) && scanner.ScanUntil(Scanner.Token.Ident))
					{
						signature.Append(" FROM");
						AppendIdentifiers(scanner, signature, dbLink);
					}
					return;
				case Scanner.Token.Insert:
				case Scanner.Token.Replace:
					signature.Append(firstToken.ToString());
					if (scanner.ScanUntil(Scanner.Token.Into) && scanner.ScanUntil(Scanner.Token.Ident))
					{
						signature.Append(" INTO");
						AppendIdentifiers(scanner, signature, dbLink);
					}
					return;
				case Scanner.Token.Select:
					signature.Append("SELECT");
					var level = 0;
					for (var t = scanner.Scan(); t != Scanner.Token.Eof; t = scanner.Scan())
					{
						if (t == Scanner.Token.Lparen)
							level++;
						else if (t == Scanner.Token.Rparen)
							level--;
						else if (t == Scanner.Token.From)
						{
							if (level == 0)
							{
								if (scanner.ScanToken(Scanner.Token.Ident))
								{
									signature.Append(" FROM");
									AppendIdentifiers(scanner, signature, dbLink);
								}
								else
									return;
							}
						}
					}
					return;
				case Scanner.Token.Update:
					signature.Append("UPDATE");
					// Scan for the table name
					var hasPeriod = false;
					var hasFirstPeriod = false;
					var isDbLink = false;
					if (scanner.ScanToken(Scanner.Token.Ident))
					{
						signature.Append(' ');
						scanner.AppendCurrentTokenText(signature);
						for (var t = scanner.Scan(); t != Scanner.Token.Eof; t = scanner.Scan())
						{
							switch (t)
							{
								case Scanner.Token.Ident:
									if (hasPeriod)
									{
										scanner.AppendCurrentTokenText(signature);
										hasPeriod = false;
									}
									if (!hasFirstPeriod)
									{
										// Some dialects allow option keywords before the table name
										// example: UPDATE IGNORE foo.bar
										signature.Length = 0;
										signature.Append("UPDATE ");
										scanner.AppendCurrentTokenText(signature);
									}
									else if (isDbLink)
									{
										if (dbLink != null) scanner.AppendCurrentTokenText(dbLink);
										isDbLink = false;
									}
									// Two adjacent identifiers found after the first period.
									// Ignore the secondary ones, in case they are unknown keywords.
									break;
								case Scanner.Token.Period:
									hasFirstPeriod = true;
									hasPeriod = true;
									signature.Append('.');
									break;
								default:
									if ("@".Equals(scanner.ToString()))
									{
										isDbLink = true;
										break;
									}
									else
										return;
							}
						}
					}
					return;
				case Scanner.Token.Merge:
					signature.Append("MERGE");
					if (scanner.ScanToken(Scanner.Token.Into) && scanner.ScanUntil(Scanner.Token.Ident))
					{
						signature.Append(" INTO");
						AppendIdentifiers(scanner, signature, dbLink);
					}
					return;
				default:
					query = query.Trim();
					var indexOfWhitespace = query.IndexOf(' ');
					signature.Append(query, 0, indexOfWhitespace > 0 ? indexOfWhitespace : query.Length);
					break;
			}
		}

		private void AppendIdentifiers(Scanner scanner, StringBuilder signature, StringBuilder dbLink)
		{
			signature.Append(' ');
			scanner.AppendCurrentTokenText(signature);
			var connectedIdents = false;
			var isDbLink = false;
			for (var t = scanner.Scan(); t != Scanner.Token.Eof; t = scanner.Scan())
			{
				switch (t)
				{
					case Scanner.Token.Ident:
						// do not add tokens which are separated by a space
						if (connectedIdents)
						{
							scanner.AppendCurrentTokenText(signature);
							connectedIdents = false;
						}
						else
						{
							if (isDbLink)
							{
								if (dbLink != null)
									scanner.AppendCurrentTokenText(dbLink);
							}
							return;
						}
						break;
					case Scanner.Token.Period:
						signature.Append('.');
						connectedIdents = true;
						break;
					case Scanner.Token.Using:
						return;
					default:
						if ("@".Equals(scanner.ToString())) isDbLink = true;
						break;
				}
			}
		}
	}
}
