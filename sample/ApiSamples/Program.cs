using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
			Console.WriteLine("Start");
			TwoTransactionWith2Spans();

			//WIP: if the process terminates the agent
			//potentially does not have time to send the transaction to the server.
			Thread.Sleep(1000);

			Console.WriteLine("Done");
			Console.ReadKey();
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


		//1. transaction with 2 subspan
		//1. transaction with 1 span that has a subspan
		public static void TwoTransactionWith2Spans()
		{

			Agent.Tracer.CaptureTransaction("TestTransaction1", "TestType1",
				t =>
				{

					t.CaptureSpan("TestSpan", "TestSpanName", s =>
					{
						Thread.Sleep(20);
						t.CaptureSpan("TestSpan2", "TestSpanName", s2 => { Thread.Sleep(20); });
					});

				});

			Agent.Tracer.CaptureTransaction("TestTransaction2", "TestType2",
				t =>
				{


					t.CaptureSpan("TestSpan", "TestSpanName", s =>
					{
						Thread.Sleep(20);
						var subSpan = s.StartSpan("TestSpan2", "TestSpanName");
						Thread.Sleep(20);
						subSpan.End();
					});
				});
		}
	}
}
