using System.Net.Http;

namespace Elastic.Apm.BackendComm.CentralConfig
{
	internal interface ICentralConfigResponseParser
	{
		(CentralConfigReader, CentralConfigFetcher.WaitInfoS) ParseHttpResponse(HttpResponseMessage httpResponse, string httpResponseBody);
	}
}
