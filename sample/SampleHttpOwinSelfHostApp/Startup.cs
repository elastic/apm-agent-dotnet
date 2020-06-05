using System.Web.Http;
using Elastic.Apm.AspNet.WebApi.SelfHost;
using Owin;

namespace SampleHttpOwinSelfHostApp
{
	public class Startup
	{
		// This code configures Web API. The Startup class is specified as a type
		// parameter in the WebApp.Start method.
		public void Configuration(IAppBuilder appBuilder)
		{
			// Configure Web API for self-host.
			var config = new HttpConfiguration();
			config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional }
			);

			config.AddElasticApm();

			appBuilder.UseWebApi(config);
		}
	}
}
