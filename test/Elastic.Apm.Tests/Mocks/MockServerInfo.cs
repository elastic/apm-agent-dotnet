// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Tests.Mocks
{
	public class MockServerInfo : IServerInfo
	{
		public MockServerInfo()
			=> Version = new Version(7, 10);

		public MockServerInfo(Version version) => Version = version;

		public bool ServerVersionQueried => true;

		public Version Version { get; }

		public Task GetServerInfoAsync() => Task.CompletedTask;
	}
}
