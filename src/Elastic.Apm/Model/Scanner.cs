// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Text;

namespace Elastic.Apm.Model
{
	internal class Scanner
	{
		public enum Token
		{
			Other,
			Eof,
			Comment,

			Ident, // includes unhandled keywords
			Number, // 123, 123.45, 123e+45
			String, // 'foo'

			Period, // .
			Lparen, // (
			Rparen, // )

			As,
			Call,
			Delete,
			From,
			Insert,
			Into,
			Or,
			Replace,
			Select,
			Set,
			Table,
			Truncate, // Cassandra/CQL-specific
			Update,
			Merge,
			Using
		}

		private readonly IScannerFilter _scannerFilter;

		public Scanner() => _scannerFilter = NoOp.INSTANCE;

		public Scanner(IScannerFilter scannerFilter) => _scannerFilter = scannerFilter;

		internal static class TokenHelper
		{
			private static readonly Token[] Empty = { };


			private static readonly Token[][] KeywordsByLength =
			{
				new Token[] { }, new Token[] { }, new[] { Token.As, Token.Or }, new[] { Token.Set }, new[] { Token.Call, Token.From, Token.Into },
				new[] { Token.Table }, new[] { Token.Delete, Token.Insert, Token.Select, Token.Update }, new[] { Token.Replace },
				new[] { Token.Truncate }
			};

			public static Token[] GetKeywordsByLength(int length) => length < KeywordsByLength.Length ? KeywordsByLength[length] : Empty;
		}

		private int _end; // text end char offset
		private string _input = "";
		private int _inputLength;
		private int _pos; // read position char offset
		private int _start; // text start char offset

		public void SetQuery(string sql)
		{
			_input = sql;
			_scannerFilter.Reset();
			_inputLength = sql.Length;
			_start = 0;
			_end = 0;
			_pos = 0;
		}

		public Token ScanWhile(Token token)
		{
			for (var t = Scan(); t != Token.Eof; t = Scan())
			{
				if (t != token)
					return t;
			}

			return Token.Eof;
		}

		public bool ScanUntil(Token token)
		{
			for (var t = Scan(); t != Token.Eof; t = Scan())
			{
				if (t == token)
					return true;
			}

			return false;
		}

		public bool ScanToken(Token token)
		{
			for (var t = Scan(); t != Token.Eof; t = Scan())
			{
				if (t == token)
					return true;
				if (t != Token.Comment) return false;
			}
			return false;
		}

		public Token Scan()
		{
			if (!HasNext()) return Token.Eof;

			var c = Next();
			while (char.IsWhiteSpace(c) || _scannerFilter.Skip(this, c))
			{
				if (HasNext())
					c = Next();
				else
					return Token.Eof;
			}
			_start = _pos - 1;
			if (c == '_' || char.IsLetter(c))
				return ScanKeywordOrIdentifier(c != '_');
			if (char.IsDigit(c)) return ScanNumericLiteral();

			switch (c)
			{
				case '\'':
					// Standard string literal
					return ScanStringLiteral();
				case '"':
					// Standard double-quoted identifier.
					//
					// NOTE(axw) MySQL will treat " as a
					// string literal delimiter by default,
					// but we assume standard SQL and treat
					// it as a identifier delimiter.
					return ScanQuotedIdentifier('"');
				case '[':
					// T-SQL bracket-quoted identifier
					return ScanQuotedIdentifier(']');
				case '`':
					// MySQL-style backtick-quoted identifier
					return ScanQuotedIdentifier('`');
				case '(':
					return Token.Lparen;
				case ')':
					return Token.Rparen;
				case '-':
					if (IsNextChar('-'))
					{
						// -- comment
						Next();
						return ScanSimpleComment();
					}
					return Token.Other;
				case '/':
					if (IsNextChar('*'))
					{
						// /* comment */
						Next();
						return ScanBracketedComment();
					}
					else if (IsNextChar('/'))
					{
						// // line comment (ex. Cassandra QL)
						Next();
						return ScanSimpleComment();
					}
					return Token.Other;
				case '.':
					return Token.Period;
				case '$':
					if (!HasNext()) return Token.Other;

					var nextC = Peek();
					if (char.IsDigit(nextC))
					{
						while (HasNext())
						{
							if (!char.IsDigit(Peek()))
								break;

							Next();
						}
						return Token.Other;
					}
					else if (nextC == '$' || nextC == '_' || char.IsLetter(nextC))
					{
						// PostgreSQL supports dollar-quoted string literal syntax, like $foo$...$foo$.
						// The tag (foo in this case) is optional, and if present follows identifier rules.
						while (HasNext())
						{
							c = Next();
							if (c == '$')
							{
								// This marks the end of the initial $foo$.
								var textC = Text();
								var i = _input.IndexOf(textC, _pos, StringComparison.InvariantCultureIgnoreCase);
								if (i >= 0)
								{
									_end = i + textC.Length;
									_pos = i + textC.Length;
									return Token.String;
								}
								return Token.Other;
							}
							if (char.IsLetter(c) || char.IsDigit(c) || c == '_')
							{
								// Identifier char, consume
							}
							else if (char.IsWhiteSpace(c))
							{
								_end--;
								return Token.Other;
							}
						}
						// Unknown token starting with $ until EOF, just ignore it.
						return Token.Other;
					}
					break;
				default:
					return Token.Other;
			}
			return Token.Other;
		}

		private Token ScanKeywordOrIdentifier(bool maybeKeyword)
		{
			while (HasNext())
			{
				var c = Peek();
				if (char.IsDigit(c) || c == '_' || c == '$')
					maybeKeyword = false;
				else if (!char.IsLetter(c)) break;

				Next();
			}
			if (!maybeKeyword) return Token.Ident;

			foreach (var token in TokenHelper.GetKeywordsByLength(TextLength()))
			{
				if (IsTextEqualIgnoreCase(token.ToString()))
					return token;
			}

			return Token.Ident;
		}

		private Token ScanNumericLiteral()
		{
			var hasPeriod = false;
			var hasExponent = false;
			while (HasNext())
			{
				var c = Peek();
				if (char.IsDigit(c))
				{
					Next();
					continue;
				}
				switch (c)
				{
					case '.':
						if (hasPeriod) return Token.Number;

						Next();
						hasPeriod = true;
						break;
					case 'e':
					case 'E':
						if (hasExponent) return Token.Number;

						Next();
						hasExponent = true;
						if (IsNextChar('+') || IsNextChar('-')) Next();
						break;
					default:
						return Token.Number;
				}
			}
			return Token.Number;
		}

		private Token ScanStringLiteral()
		{
			while (HasNext())
			{
				var c = Next();
				if (c == '\\' && HasNext())
				{
					// skip escaped character
					// example: 'what\'s up?'
					Next();
				}
				else if (c == '\'')
				{
					if (IsNextChar('\''))
					{
						// skip escaped single quote
						// example: 'what''s up?'
						Next();
					}
					else
					{
						// end of string
						return Token.String;
					}
				}
			}
			return Token.Eof;
		}

		private Token ScanQuotedIdentifier(char delimiter)
		{
			while (HasNext())
			{
				var c = Next();
				if (c == delimiter)
				{
					if (delimiter == '"' && IsNextChar('"'))
					{
						// skip escaped double quote
						// example: "He said ""great"""
						Next();
						continue;
					}
					// remove quotes from identifier
					_start++;
					_end--;
					return Token.Ident;
				}
			}
			return Token.Eof;
		}

		private Token ScanSimpleComment()
		{
			while (HasNext())
			{
				if (Next() == '\n')
					return Token.Comment;
			}

			return Token.Comment;
		}

		private Token ScanBracketedComment()
		{
			var nesting = 1;
			while (HasNext())
			{
				var c = Next();
				switch (c)
				{
					case '/':
						if (IsNextChar('*'))
						{
							Next();
							nesting++;
						}
						break;
					case '*':
						if (IsNextChar('/'))
						{
							Next();
							nesting--;
							if (nesting == 0) return Token.Comment;
						}
						break;
				}
			}
			return Token.Eof;
		}

		private char Peek() => _input.ElementAt(_pos);

		public char Next()
		{
			var c = Peek();
			_pos++;
			_end = _pos;
			return c;
		}

		private bool HasNext() => _pos < _inputLength;

		private bool IsTextEqualIgnoreCase(string name)
			=> _input.IndexOf(name, _start, name.Length, StringComparison.CurrentCultureIgnoreCase) == _start; //TODO

		/// <summary>
		/// Returns the portion of the SQL that relates to the most recently scanned token.
		/// Note: this method allocates memory and thus should only be used in tests.
		/// </summary>
		/// <returns> the portion of the SQL that relates to the most recently scanned token </returns>
		private string Text()
		{
			var sb = new StringBuilder();
			AppendCurrentTokenText(sb);
			return sb.ToString();
		}

		/// <summary>
		/// Appends the portion of the SQL that relates to the most recently scanned token to the provided {@link StringBuilder}.
		/// </summary>
		/// <param name="sb"> the <see cref="StringBuilder"/> which will be used to append the SQL </param>
		public void AppendCurrentTokenText(StringBuilder sb) => sb.Append(_input, _start, _end - _start);

		public int TextLength() => _end - _start;

		private bool IsNextChar(char c) => HasNext() && Peek() == c;

		public bool IsNextCharIgnoreCase(char c) => HasNext() && char.ToLower(Peek()) == char.ToLower(c);
	}

	internal interface IScannerFilter
	{
		void Reset();

		bool Skip(Scanner s, char c);
	}

	internal class NoOp : IScannerFilter
	{
		public static NoOp INSTANCE;

		static NoOp() => INSTANCE = new NoOp();

		public bool Skip(Scanner s, char c) => false;

		public void Reset() { }
	}
}
