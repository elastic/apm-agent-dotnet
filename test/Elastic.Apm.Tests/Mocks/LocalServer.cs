using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Elastic.Apm.Tests.Mocks
{
	public class LocalServer : IDisposable
	{
		private readonly HttpListener _httpListener = new HttpListener();
		private readonly Task _task;
		private readonly CancellationTokenSource _tokenSource;
		private long _seenRequests;
		public long SeenRequests => _seenRequests;

		public LocalServer(Action<HttpListenerContext> testAction = null)
		{
			_httpListener.Prefixes.Add(Uri);
			_httpListener.Start();
			_tokenSource = new CancellationTokenSource();
			_task = Task.Run(() =>
			{
				do
				{
					var context = _httpListener.GetContext();
					Interlocked.Increment(ref _seenRequests);
					if (testAction == null)
						context.Response.StatusCode = 200;
					else
						testAction(context);

					context.Response.OutputStream.Close();
					context.Response.Close();
				} while (!_tokenSource.IsCancellationRequested);
			},_tokenSource.Token);
		}

		public string Uri => "http://localhost:8082/";

		public void Dispose()
		{
			_httpListener.Abort();
			((IDisposable)_httpListener).Dispose();
			_tokenSource.Cancel();
			_tokenSource.Dispose();
		}
	}
}
