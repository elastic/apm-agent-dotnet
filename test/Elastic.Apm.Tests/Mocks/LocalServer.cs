using System;
using System.Net;
using System.Threading.Tasks;

namespace Elastic.Apm.Tests.Mocks
{
	public class LocalServer : IDisposable
	{
		public LocalServer(Action<HttpListenerContext> testAction = null)
		{
			_httpListener.Prefixes.Add(Uri);
			_httpListener.Start();

			Task.Run(() =>
			{
				var context = _httpListener.GetContext();

				if (testAction == null)
					context.Response.StatusCode = 200;
				else
					testAction(context);

				context.Response.OutputStream.Close();
				context.Response.Close();
			});
		}

		private readonly HttpListener _httpListener = new HttpListener();
		public string Uri => "http://localhost:8082/";

		public void Dispose()
		{
			_httpListener.Abort();
			((IDisposable)_httpListener).Dispose();
		}
	}
}
