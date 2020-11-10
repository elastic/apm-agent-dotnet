// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;

namespace Elastic.Apm.ServerInfo
{
	internal interface IServerInfo
	{
		public Version Version { get; set; }

		public bool Initialized { get; }

		public Task GetServerInfoAsync();
	}

	public class Version
	{
		public Version(int major, int minor, int path)
		{
			Major = major;
			Minor = minor;
			Path = path;
		}

		public int Major { get; }
		public int Minor { get; }
		public int Path { get; }
	}
}
