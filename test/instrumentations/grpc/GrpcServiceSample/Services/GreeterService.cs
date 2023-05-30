using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace GrpcServiceSample
{
	public class GreeterService : Greeter.GreeterBase
	{
		private readonly ILogger<GreeterService> _logger;

		public GreeterService(ILogger<GreeterService> logger) => _logger = logger;

		public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context) =>
			Task.FromResult(new HelloReply { Message = "Hello " + request.Name });

		public override Task<HelloReply> ThrowAnException(HelloRequest request, ServerCallContext context) => throw new Exception("Test Exception");
	}
}
