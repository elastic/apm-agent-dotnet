// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcServiceSample;
using Microsoft.Extensions.Logging;

namespace Elastic.Apm.Grpc.Tests.Services
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
