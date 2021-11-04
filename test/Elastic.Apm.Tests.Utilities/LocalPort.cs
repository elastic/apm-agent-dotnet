// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Net.Sockets;

namespace Elastic.Apm.Tests.Utilities
{
	public class LocalPort
	{
		private static readonly IPEndPoint DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, 0);

		public static int GetAvailablePort()
		{
			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				socket.Bind(DefaultLoopbackEndpoint);
				return ((IPEndPoint) socket.LocalEndPoint).Port;
			}
		}
	}
}
