using System;
using System.Net;
using System.Threading.Tasks;

namespace Elastic.Apm.Tests.Mocks
{
	public class LocalServer : IDisposable
	{
		private readonly HttpListener httpListener = new HttpListener();

		public LocalServer(Action<HttpListenerContext> testAction = null)
		{
			httpListener.Prefixes.Add(Uri);
			httpListener.Start();

			Task.Run(() =>
			{
				var context = httpListener.GetContext();

				if (testAction == null)
					context.Response.StatusCode = 200;
				else
					testAction(context);

				context.Response.OutputStream.Close();
				context.Response.Close();
			});
		}

		public string Uri => "http://localhost:8082/";

		public void Dispose()
		{
			httpListener.Abort();
			((IDisposable)httpListener).Dispose();
		}
	}
}
