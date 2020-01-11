using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace SampleHttpOwinSelfHostApp.Controllers
{
	public class ValuesController : ApiController
	{
		private readonly HttpClient _httpClient;

		public ValuesController() => _httpClient = new HttpClient();

		public async Task<IEnumerable<string>> Get()
		{
			var listeners = DiagnosticListener.AllListeners;
			var lastValue = "call to elastic.co: ";
			try
			{
				var elasticRes = await _httpClient.GetAsync("https://elastic.co");
				lastValue += elasticRes.StatusCode;
			}
			catch
			{
				lastValue += "failed";
			}

			return new[] { "value1", "value2", lastValue };
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) _httpClient?.Dispose();
		}
	}
}
