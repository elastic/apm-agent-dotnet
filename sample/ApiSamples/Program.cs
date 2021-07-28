// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Elastic.Apm;
using Elastic.Apm.Api;

#if NET5_0
using OpenTelemetry.Trace;
#endif

namespace ApiSamples
{
	/// <summary>
	/// This class exercises the Public Agent API.
	/// </summary>
	internal class Program
	{
		private static void Main(string[] args)
		{
			Agent.Setup(new AgentComponents());
#if NET5_0
			ActivityMixedWithElasticApi();
			//OpenTelemetryShim();
			Console.ReadKey();
#endif

		}

		public static void ActivityOnlySample()
		{

			var foo = new Activity("foo").Start();
			var bar = new Activity("bar").Start();

			Activity.DefaultIdFormat = ActivityIdFormat.W3C;


			bar.Stop();
			foo.Stop();
		}


#if NET5_0
		public static void OpenTelemetryShim()
		{
			var tracer = TracerProvider.Default.GetTracer("MyTracer", "1");
			var fooSpan = tracer.StartActiveSpan("fooSpanWithShim");

			var barSpan = tracer.StartActiveSpan("barSpanWithShim");
			fooSpan.End();
			barSpan.End();
		}
#endif

		public static void ActivityMixedWithElasticApi()
		{
			var t = Agent.Tracer.StartTransaction("ElasticTransaction", "test");

			var fooActivity = new Activity("fooActivity").Start();

			var elasticSpan = Agent.Tracer.CurrentSpan.StartSpan("ElasticSpan", "test");
			var barActivity = new Activity("barActivity").Start();

			barActivity.Stop();
			elasticSpan.End();
			fooActivity.Stop();
			t.End();
		}

		public static void SampleCustomTransaction()
		{
			WriteLineToConsole($"{nameof(SampleCustomTransaction)} started");
			var transaction = Agent.Tracer.StartTransaction("SampleTransaction", ApiConstants.TypeRequest);

			Thread.Sleep(500); //simulate work...

			transaction.End();
			WriteLineToConsole($"{nameof(SampleCustomTransaction)} finished");
		}

		public static void SampleCustomTransactionWithSpan()
		{
			WriteLineToConsole($"{nameof(SampleCustomTransactionWithSpan)} started");
			var transaction = Agent.Tracer.StartTransaction("SampleTransactionWithSpan", ApiConstants.TypeRequest);

			Thread.Sleep(500);

			var span = transaction.StartSpan("SampleSpan", ApiConstants.TypeExternal);
			Thread.Sleep(200);
			span.End();

			transaction.End();
			WriteLineToConsole($"{nameof(SampleCustomTransactionWithSpan)} finished");
		}

		public static void SampleError()
		{
			WriteLineToConsole($"{nameof(SampleError)} started");
			var transaction = Agent.Tracer.StartTransaction("SampleError", ApiConstants.TypeRequest);

			Thread.Sleep(500); //simulate work...
			var span = transaction.StartSpan("SampleSpan", ApiConstants.TypeExternal);
			try
			{
				throw new Exception("bamm");
			}
			catch (Exception e)
			{
				span.CaptureException(e);
			}
			finally
			{
				span.End();
			}

			transaction.End();

			WriteLineToConsole($"{nameof(SampleError)} finished");
		}

		public static void SampleCustomTransactionWithConvenientApi() => Agent.Tracer.CaptureTransaction("TestTransaction", "TestType",
			t =>
			{
				t.Context.Response = new Response { Finished = true, StatusCode = 200 };
				t.Context.Request = new Request("GET", new Url { Protocol = "HTTP" });

				t.SetLabel("fooTransaction1", "barTransaction1");
				t.SetLabel("fooTransaction2", "barTransaction2");

				Thread.Sleep(10);
				t.CaptureSpan("TestSpan", "TestSpanType", s =>
				{
					Thread.Sleep(20);
					s.SetLabel("fooSpan", "barSpan");
				});
			});


		//1 transaction with 2 spans
		//1 transaction with 1 span that has a sub span
		public static void TwoTransactionWith2Spans()
		{
			//1 transaction 2 spans (both have the transaction as parent)
			Agent.Tracer.CaptureTransaction("TestTransaction1", "TestType1",
				t =>
				{
					t.CaptureSpan("TestSpan", "TestSpanType", s =>
					{
						Thread.Sleep(20);
						//this span is also started on the transaction:
						t.CaptureSpan("TestSpan2", "TestSpanType", s2 => { Thread.Sleep(20); });
					});
				});

			//1 transaction then 1 span on that transaction and then 1 span on that previous span
			Agent.Tracer.CaptureTransaction("TestTransaction2", "TestType2",
				t =>
				{
					t.CaptureSpan("TestSpan", "TestSpanType", s =>
					{
						Thread.Sleep(20);
						//this span is a subspan of the `s` span:
						s.CaptureSpan("TestSpan2", "TestSpanType", () => Thread.Sleep(20));
					});
				});
		}

		public static void PassDistributedTracingData()
		{
			var transaction = Agent.Tracer.StartTransaction("Transaction1", "TestTransaction");

			try
			{
				Thread.Sleep(300);

				//We start the sample app again with a new service name and we pass DistributedTracingData to it
				//In the main method we check for this and continue the trace.

				var distributedTracingData = Agent.Tracer.CurrentSpan?.OutgoingDistributedTracingData
					?? Agent.Tracer.CurrentTransaction?.OutgoingDistributedTracingData;

				var traceParent = distributedTracingData?.SerializeToString();
				var assembly = Assembly.GetExecutingAssembly().Location;

				WriteLineToConsole(
					$"Spawning callee process and passing outgoing distributed tracing data: {traceParent} to it...");
				var startInfo = new ProcessStartInfo { FileName = "dotnet", Arguments = $"{assembly} {traceParent}" };

				startInfo.Environment["ELASTIC_APM_SERVICE_NAME"] = "Service2";
				var calleeProcess = Process.Start(startInfo);
				WriteLineToConsole("Spawned callee process");

				Thread.Sleep(1100);

				WriteLineToConsole("Waiting for callee process to exit...");
				calleeProcess?.WaitForExit();
				WriteLineToConsole("Callee process exited");
			}
			finally
			{
				transaction.End();
			}
		}

		private static void WriteLineToConsole(string line) =>
			Console.WriteLine($"[{Process.GetCurrentProcess().Id}] {line}");

		/// <summary>
		/// Registers some sample filters.
		/// </summary>
		// ReSharper disable once UnusedMember.Local
		private static void FilterSample()
		{
			Agent.AddFilter((ITransaction transaction) =>
			{
				transaction.Name = "NewTransactionName";
				return transaction;
			});

			Agent.AddFilter((ITransaction transaction) =>
			{
				transaction.Type = "NewSpanName";
				return transaction;
			});

			Agent.AddFilter((ISpan span) =>
			{
				span.Name = "NewSpanName";
				return span;
			});

			Agent.AddFilter(span =>
			{
				if (span.StackTrace.Count > 10)
					span.StackTrace.RemoveRange(10, span.StackTrace.Count - 10);

				return span;
			});

			Agent.AddFilter(error =>
			{
				if (error.Culprit == "SecretComponent")
					return null;

				if (error.Exception.Type == "SecretType")
					error.Exception.Message = "[HIDDEN]";

				Console.WriteLine(
					$"Error printed in a filter - culprit: {error.Culprit}, id: {error.Id}, parentId: {error.ParentId}, traceId: {error.TraceId}, transactionId: {error.TransactionId}");
				return error;
			});
		}

		// ReSharper disable ArrangeMethodOrOperatorBody
		public static void SampleSpanWithCustomContext()
		{
			Agent.Tracer.CaptureTransaction("SampleTransaction", "SampleTransactionType",
				transaction =>
				{
					transaction.CaptureSpan("SampleSpan", "SampleSpanType",
						span => { span.Context.Db = new Database { Statement = "GET /_all/_search?q=tag:wow", Type = Database.TypeElasticsearch }; });
				});
		}

		public static void SampleSpanWithCustomContextFillAll()
		{
			Agent.Tracer.CaptureTransaction("SampleTransaction", "SampleTransactionType", transaction =>
			{
				transaction.CaptureSpan("SampleSpan1", "SampleSpanType", span =>
				{
					// ReSharper disable once UseObjectOrCollectionInitializer
					span.Context.Http = new Http { Url = "http://mysite.com", Method = "GET" };
					// send request, get response with status code
					span.Context.Http.StatusCode = 200;
				});

				transaction.CaptureSpan("SampleSpan2", "SampleSpanType",
					span =>
					{
						span.Context.Db = new Database
						{
							Statement = "GET /_all/_search?q=tag:wow", Type = Database.TypeElasticsearch, Instance = "MyInstance"
						};
					});
			});
		}
		// ReSharper restore ArrangeMethodOrOperatorBody

#if NETCOREAPP3_0
		/// <summary>
		/// Test for https://github.com/elastic/apm-agent-dotnet/issues/884
		/// </summary>
		private IAsyncEnumerable<int> TestCompilation() => throw new Exception();
#endif
	}
}
