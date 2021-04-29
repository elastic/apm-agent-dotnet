// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Libraries.Newtonsoft.Json.Linq;

namespace Elastic.Apm.ServerInfo
{
	/// <summary>
	/// A "real" <see cref="IApmServerInfo" /> implementation.
	/// </summary>
	internal class ApmServerInfo : IApmServerInfo
	{
		public ElasticVersion Version { get; set; }
	}
}
