// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Http;

namespace Elastic.Apm.BackendComm.CentralConfig
{
	internal interface ICentralConfigResponseParser
	{
		(CentralConfigReader, CentralConfigFetcher.WaitInfoS) ParseHttpResponse(HttpResponseMessage httpResponse, string httpResponseBody);
	}
}
