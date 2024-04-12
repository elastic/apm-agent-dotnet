// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ServiceModel;
using System.ServiceModel.Web;

namespace WcfServiceSample
{
	[ServiceContract]
	public interface ICustomerService
	{
		[OperationContract]
		[WebGet(
			UriTemplate = "/Customer/{value}/Info",
			ResponseFormat = WebMessageFormat.Json,
			BodyStyle = WebMessageBodyStyle.Wrapped
		)]
		string GetCustomer(string value);
	}
}
