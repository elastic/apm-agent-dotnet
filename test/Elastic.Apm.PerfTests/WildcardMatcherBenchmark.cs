// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.PerfTests
{
	[MemoryDiagnoser]
	public class WildcardMatcherBenchmark
	{
		private static string _str;

		[GlobalSetup]
		public void Setup()
		{
			_str = string.Empty;
			var random = new Random();
			for (var i = 0; i < 1000; i++) _str += random.Next(10).ToString();
		}

		[Benchmark]
		public void MatcherVerbatimCaseSensitive()
		{
			var matcher = new WildcardMatcher.VerbatimMatcher(_str, true);
			var res = matcher.Matches(_str);
			Debug.WriteLine(res);
		}

		[Benchmark]
		public void MatcherVerbatimCaseInsensitive()
		{
			var matcher = new WildcardMatcher.VerbatimMatcher(_str, false);
			var res = matcher.Matches(_str);
			Debug.WriteLine(res);
		}

		[Benchmark]
		public void MatcherSimpleCaseSensitive()
		{
			var matcher = new WildcardMatcher.SimpleWildcardMatcher(_str, false, false, false);
			var res = matcher.Matches(_str);
			Debug.WriteLine(res);
		}

		[Benchmark]
		public void MatcherSimpleCaseInsensitive()
		{
			var matcher = new WildcardMatcher.SimpleWildcardMatcher(_str, false, false, true);
			var res = matcher.Matches(_str);
			Debug.WriteLine(res);
		}
	}
}
