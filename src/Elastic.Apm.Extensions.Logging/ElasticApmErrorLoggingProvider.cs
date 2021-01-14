// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Extensions.Hosting, PublicKey=" + Signing.PublicKey)]

namespace Elastic.Apm.Extensions.Logging
{
	internal class ElasticApmErrorLoggingProvider : ILoggerProvider
	{
		private readonly IApmAgent _apmAgent;

		public ElasticApmErrorLoggingProvider(IApmAgent apmAgent) => _apmAgent = apmAgent;

		public ILogger CreateLogger(string categoryName)
			=> new ElasticApmErrorLogger(_apmAgent);

		public void Dispose() { }
	}
}
