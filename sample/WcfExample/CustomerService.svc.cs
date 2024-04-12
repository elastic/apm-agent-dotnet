// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace WcfServiceSample
{
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service.svc or Service.svc.cs at the Solution Explorer and start debugging.
    public class CustomerService : ICustomerService
    {
		public string GetCustomer(string value) => string.Format("You entered: {0}", int.Parse(value));
    }
}
