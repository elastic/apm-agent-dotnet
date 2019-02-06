using System;
using System.Net;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;

namespace HttpListenerSample
{
	class Program
	{
		static async Task Main(string[] args)
		{
			if (!HttpListener.IsSupported)
			{
				Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
				return;
			}

			// Create a listener.
			var listener = new HttpListener();
			// Add the prefix
			listener.Prefixes.Add("http://localhost:8080/");

			listener.Start();
			Console.WriteLine("Listening...");

			while (true)
			{
				// Note: The GetContext method blocks while waiting for a request.
				var context = listener.GetContext();

				// Capture the incoming request as a transaction with the agent
				await Agent.Tracer.CaptureTransaction("Request", ApiConstants.TypeRequest, async () =>
				{
					var request = context.Request;
					// Obtain a response object.
					var response = context.Response;
					// Construct a response.
					var responseString = $"<HTML><BODY> Hello world! Random number: {await GenerateRandomNumber()}</BODY></HTML>";
					var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
					// Get a response stream and write the response to it.
					response.ContentLength64 = buffer.Length;
					var output = response.OutputStream;
					output.Write(buffer, 0, buffer.Length);
					// You must close the output stream.
					output.Close();
				});
			}

			listener.Stop();
		}

		static readonly Random Random = new Random();

		private static async Task<int> GenerateRandomNumber()
		{
			// Get the current transaction and then capture this method as a span on the current transaction
			return await Agent.Tracer.CurrentTransaction.CaptureSpan("RandomGenerator", "Random", async () =>
			{
				await Task.Delay(5); // Simulate some work
				return Random.Next();
			});
		}
	}
}
