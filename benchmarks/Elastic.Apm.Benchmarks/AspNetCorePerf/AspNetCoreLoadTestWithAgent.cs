// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http;
using BenchmarkDotNet.Attributes;

namespace Elastic.Apm.Benchmarks.AspNetCorePerf
{
	/// <summary>
	/// A Test which triggers a simple ASP.NET Core endpoint and measures the response time while the agent is active in the
	/// process.
	/// </summary>
	public class AspNetCoreLoadTestWithAgent
	{
		[GlobalSetup]
		public void Setup()
		{
			var aspNetCoreTest = new AspNetCoreSampleRunner();
			aspNetCoreTest.StartSampleAppWithAgent(true, "http://localhost:5901");

			var httpClient = new HttpClient();
			httpClient.GetAsync("http://localhost:5901").Wait();
		}

		[Benchmark]
		public void SimpleEmptyWebRequest()
		{
			var httpClient = new HttpClient();
			httpClient.GetAsync("http://localhost:5901/Home/EmptyWebRequest").Wait();
		}

		[Benchmark]
		public void WebRequestWithDbCallsAndCustomSpan()
		{
			var httpClient = new HttpClient();
			httpClient.GetAsync("http://localhost:5901/Home/TransactionWithDbCallAndCustomSpan").Wait();
		}
	}
}
