using System;
using System.Diagnostics;
using System.Threading;
using Elastic.Apm;
using Elastic.Apm.Api;

namespace ApiSamples
{
	/// <summary>
	/// This class exercices the Public Agent API.
	/// </summary>
	internal class Program
	{
		private static void Main(string[] args)
		{
			if (args.Length == 2) //in case it's started with 2 arguments we try to parse those 2 args as a TraceContext
			{
				Console.WriteLine($"Continue trace, traceId: {args[0]}, parentId: {args[1]}");
				var transaction2 = Agent.Tracer.StartTransaction("Transaction2", "TestTransaction", (args[0], args[1]));

				try
				{
					transaction2.CaptureSpan("TestSpan", "TestSpanType", () => Thread.Sleep(200));
				}
				finally
				{
					transaction2.End();
				}

				Thread.Sleep(1000);
				Console.WriteLine("Continue trace finished");
			}
			else
			{
				Console.WriteLine("Start");
				PassTraceContext();

				//WIP: if the process terminates the agent
				//potentially does not have time to send the transaction to the server.
				Thread.Sleep(1000);

				Console.WriteLine("Done");
				Console.ReadKey();
			}
		}

		public static void SampleSpamWithCustomContext()
		{
			Agent.Tracer.CaptureTransaction("SampleTransaction", "SampleTransactionType", transaction =>
			{
				transaction.CaptureSpan("SampleSpan", "SampleSpanType", span =>
				{
					span.Context.Db = new Database
					{
						Statement = "GET /_all/_search?q=tag:wow",
						Type = Database.TypeElasticsearch,
					};
				});
			});
		}

		public static void SampleSpamWithCustomContextFillAll()
		{
			Agent.Tracer.CaptureTransaction("SampleTransaction", "SampleTransactionType", transaction =>
			{
				transaction.CaptureSpan("SampleSpan1", "SampleSpanType", span =>
				{
					span.Context.Http = new Http
					{
						Url = "http://mysite.com",
						Method = "GET",
						StatusCode = 200,
					};
				});

				transaction.CaptureSpan("SampleSpan2", "SampleSpanType", span =>
				{
					span.Context.Db = new Database
					{
						Statement = "GET /_all/_search?q=tag:wow",
						Type = Database.TypeElasticsearch,
						Instance = "MyInstance"
					};
				});
			});
		}

		public static void SampleCustomTransaction()
		{
			Console.WriteLine($"{nameof(SampleCustomTransaction)} started");
			var transaction = Agent.Tracer.StartTransaction("SampleTransaction", ApiConstants.TypeRequest);

			Thread.Sleep(500); //simulate work...

			transaction.End();
			Console.WriteLine($"{nameof(SampleCustomTransaction)} finished");
		}

		public static void SampleCustomTransactionWithSpan()
		{
			Console.WriteLine($"{nameof(SampleCustomTransactionWithSpan)} started");
			var transaction = Agent.Tracer.StartTransaction("SampleTransactionWithSpan", ApiConstants.TypeRequest);

			Thread.Sleep(500);

			var span = transaction.StartSpan("SampleSpan", ApiConstants.TypeExternal);
			Thread.Sleep(200);
			span.End();

			transaction.End();
			Console.WriteLine($"{nameof(SampleCustomTransactionWithSpan)} finished");
		}

		public static void SampleError()
		{
			Console.WriteLine($"{nameof(SampleError)} started");
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

			Console.WriteLine($"{nameof(SampleError)} finished");
		}

		public static void SampleCustomTransactionWithConvenientApi() => Agent.Tracer.CaptureTransaction("TestTransaction", "TestType",
			t =>
			{
				t.Context.Response = new Response() { Finished = true, StatusCode = 200 };
				t.Context.Request = new Request("GET", new Url { Protocol = "HTTP" });

				t.Tags["fooTransaction1"] = "barTransaction1";
				t.Tags["fooTransaction2"] = "barTransaction2";

				Thread.Sleep(10);
				t.CaptureSpan("TestSpan", "TestSpanName", s =>
				{
					Thread.Sleep(20);
					s.Tags["fooSpan"] = "barSpan";
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
					t.CaptureSpan("TestSpan", "TestSpanName", s =>
					{
						Thread.Sleep(20);
						//this span is also started on the transaction:
						t.CaptureSpan("TestSpan2", "TestSpanName", s2 => { Thread.Sleep(20); });
					});
				});

			//1 transaction then 1 span on that transaction and then 1 span on that previous span
			Agent.Tracer.CaptureTransaction("TestTransaction2", "TestType2",
				t =>
				{
					t.CaptureSpan("TestSpan", "TestSpanName", s =>
					{
						Thread.Sleep(20);
						//this span is a subspan of the `s` span:
						s.CaptureSpan("TestSpan2", "TestSpanName", () => Thread.Sleep(20));
					});
				});
		}

		public static void PassTraceContext()
		{
			var transaction = Agent.Tracer.StartTransaction("Transaction1", "TestTransaction");

			try
			{
				Thread.Sleep(300);

				//We start the sample app again with a new service name and we pass TraceContext to it
				//In the main method we check for this and continue the trace.
				var p = new Process();
				p.StartInfo.Environment["ELASTIC_APM_SERVICE_NAME"] = "Service2";
				p.StartInfo.Arguments = $"run {transaction.TraceId} {transaction.Id}";
				p.StartInfo.FileName = "dotnet";
				p.Start();

				Thread.Sleep(1100);
			}
			finally
			{
				transaction.End();
			}
		}
	}
}
