using System;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.DiagnosticSource;
using Grpc.Net.Client;
using GrpcServiceSample;

namespace GrpcClientSample
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			Agent.Subscribe(new HttpDiagnosticsSubscriber());

			await Agent.Tracer.CaptureTransaction("SampleCall", "test", async () =>
			  {
				  var channel = GrpcChannel.ForAddress("https://localhost:5001");
				  var client = new Greeter.GreeterClient(channel);

				  var response = await client.SayHelloAsync(
					  new HelloRequest { Name = "World" });

				  var response2 = await client.SayHelloAsync(
					  new HelloRequest { Name = "World2" });

				  Console.WriteLine(response.Message);
				  Console.WriteLine(response2.Message);
				  Console.WriteLine("Hello World!");
			  });

			Console.ReadKey();
		}
	}
}
