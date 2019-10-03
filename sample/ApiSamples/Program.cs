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
			if (args.Length == 1) //in case it's started with an argument we try to parse the argument as a DistributedTracingData
			{
				WriteLineToConsole($"Callee process started - continuing trace with distributed tracing data: {args[0]}");
				var transaction2 = Agent.Tracer.StartTransaction("Transaction2", "TestTransaction",
					DistributedTracingData.TryDeserializeFromString(args[0]));

				try
				{
					transaction2.CaptureSpan("TestSpan", "TestSpanType", () => Thread.Sleep(200));
				}
				finally
				{
					transaction2.End();
				}

				Thread.Sleep(1000);
				WriteLineToConsole("About to exit");
			}
			else
			{
				WriteLineToConsole("Started");
				PassDistributedTracingData();

				//WIP: if the process terminates the agent
				//potentially does not have time to send the transaction to the server.
				Thread.Sleep(1000);

				WriteLineToConsole("About to exit - press any key...");
				Console.ReadKey();
			}
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

				t.Labels["fooTransaction1"] = "barTransaction1";
				t.Labels["fooTransaction2"] = "barTransaction2";

				Thread.Sleep(10);
				t.CaptureSpan("TestSpan", "TestSpanType", s =>
				{
					Thread.Sleep(20);
					s.Labels["fooSpan"] = "barSpan";
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
				var startInfo = new ProcessStartInfo();
				startInfo.Environment["ELASTIC_APM_SERVICE_NAME"] = "Service2";
				var outgoingDistributedTracingData = transaction.OutgoingDistributedTracingData.SerializeToString();
				startInfo.FileName = "dotnet";
				startInfo.Arguments = $"run {outgoingDistributedTracingData}";
				WriteLineToConsole(
					$"Spawning callee process and passing outgoing distributed tracing data: {outgoingDistributedTracingData} to it...");
				var calleeProcess = Process.Start(startInfo);
				WriteLineToConsole("Spawned callee process");

				Thread.Sleep(1100);

				WriteLineToConsole("Waiting for callee process to exit...");
				calleeProcess.WaitForExit();
				WriteLineToConsole("Callee process exited");
			}
			finally
			{
				transaction.End();
			}
		}

		private static void WriteLineToConsole(string line) =>
			Console.WriteLine($"[{Process.GetCurrentProcess().Id}] {line}");

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
	}
}
