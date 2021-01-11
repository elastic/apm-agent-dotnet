// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elastic.Apm.DiagnosticSource;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Grpc.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

namespace Elastic.Apm.GrpcClient
{
	public class GrpcClientDiagnosticSubscriber : IDiagnosticsSubscriber
	{
		internal GrpcClientDiagnosticListener GrpcClientDiagnosticListener { get; private set; }

		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();

			if (!agent.ConfigurationReader.Enabled)
				return retVal;

			GrpcClientDiagnosticListener = new GrpcClientDiagnosticListener(agent as ApmAgent);
			var subscriber = new DiagnosticInitializer(agent.Logger, new[] { GrpcClientDiagnosticListener });
			retVal.Add(subscriber);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return retVal;
		}
	}
}
