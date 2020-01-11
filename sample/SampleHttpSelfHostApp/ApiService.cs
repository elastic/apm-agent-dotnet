using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Web.Http.SelfHost;
using Elastic.Apm.AspNet.WebApi.SelfHost;
using SampleHttpSelfHostApp.Controllers;

namespace SampleHttpSelfHostApp
{
	public class ApiService
	{
		private HttpSelfHostServer _server;

		public void Start()
		{
			var config = new HttpSelfHostConfiguration("http://localhost:12345");

			config.Routes.MapHttpRoute("values", "api/values", new { controller = "Values", action = nameof(ValuesController.GetValues) },
				new { httpMethod = new HttpMethodConstraint(HttpMethod.Get) });

			config.MessageHandlers.AddElasticApmMessageHandler();

			_server = new HttpSelfHostServer(config);

			_server.OpenAsync()
				.ContinueWith(t => { Console.WriteLine("Api Service started with listening `12345` port"); }, TaskContinuationOptions.NotOnFaulted);
		}

		public void Stop() => _server?.Dispose();
	}
}
