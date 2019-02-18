using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Newtonsoft.Json.Linq;

namespace HttpListenerSample
{
	internal class Program
	{
		private static readonly Random Random = new Random();

		private static async Task Main()
		{
			if (!HttpListener.IsSupported)
			{
				Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
				return;
			}

			//Enable outgoing HTTP request capturing with the elastic APM agent:
			//small inside: with this the agent subscribes to the corresponding diagnosticsource events
			Agent.Subscribe(new HttpDiagnosticsSubscriber());

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
					// Obtain a response object.
					var response = context.Response;
					// Construct a response.
					var responseString = $"<HTML><BODY> <p> Hello world! Random number: {await GenerateRandomNumber()} </p>"
						+ " <p> Number of stargazers on <a href=\"https://github.com/elastic/apm-agent-dotnet\"> "
						+ $"GitHub for the APM .NET Agent </a>: {await GetNumberOfStars()} </p>"
						+ " </BODY></HTML>";
					var buffer = Encoding.UTF8.GetBytes(responseString);
					// Get a response stream and write the response to it.
					response.ContentLength64 = buffer.Length;
					var output = response.OutputStream;
					output.Write(buffer, 0, buffer.Length);
					// You must close the output stream.
					output.Close();
				});
			}
		}

		private static async Task<int> GenerateRandomNumber() => await Agent.Tracer.CurrentTransaction.CaptureSpan("RandomGenerator", "Random",
			async () =>
			{
				await Task.Delay(5); // Simulate some work
				return Random.Next();
			});

		//This method has no agent code in it, since the outgoing HTTP call is automatically captured by the agent
		private static async Task<int> GetNumberOfStars()
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("User-Agent", "APM-Sample-App");
			var responseMsg = await httpClient.GetAsync("https://api.github.com/repos/elastic/apm-agent-dotnet");
			var responseStr = await responseMsg.Content.ReadAsStringAsync();

			return int.TryParse(JObject.Parse(responseStr)["stargazers_count"].ToString(), out var retVal) ? retVal : 0;
		}
	}
}
