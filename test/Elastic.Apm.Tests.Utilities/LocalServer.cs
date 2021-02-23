// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.Utilities
{
	public class LocalServer : IDisposable
	{
		private const int MinPort = 49215;
		private const int MaxPort = 65535;

		private readonly HttpListener _httpListener;
		private readonly Task _task;
		private readonly CancellationTokenSource _tokenSource;
		private long _seenRequests;

		private LocalServer(HttpListener httpListener, Action<HttpListenerContext> testAction = null)
		{
			_httpListener = httpListener;
			Uri = _httpListener.Prefixes.First();
			_tokenSource = new CancellationTokenSource();
			_task = Task.Run(async () =>
			{
				do
				{
					var context = await _httpListener.GetContextAsync();
					using (context.Response)
					{
						Interlocked.Increment(ref _seenRequests);
						if (testAction == null)
							context.Response.StatusCode = 200;
						else
							testAction(context);
					}
				} while (!_tokenSource.IsCancellationRequested);
			}, _tokenSource.Token);
		}

		public static LocalServer Create() => Create(null);

		public static LocalServer Create(Action<HttpListenerContext> testAction)
		{
			for (var i = MinPort; i < MaxPort; i++)
			{
				if (TryCreate($"http://localhost:{i}/", testAction, out var server))
					return server;
			}

			throw new XunitException("could not create LocalServer");
		}

		public static LocalServer Create(string uri, Action<HttpListenerContext> testAction)
		{
			if (TryCreate(uri, testAction, out var server))
				return server;

			throw new XunitException($"could not create LocalServer listening on {uri}");
		}

		public static bool TryCreate(string uri, Action<HttpListenerContext> testAction, out LocalServer server)
		{
			var httpListener = new HttpListener();
			httpListener.Prefixes.Add(uri);
			try
			{
				httpListener.Start();
			}
			catch
			{
				server = null;
				return false;
			}

			server = new LocalServer(httpListener, testAction);
			return true;
		}

		public long SeenRequests => _seenRequests;

		public string Uri { get; }

		public void Dispose()
		{
			_httpListener.Abort();
			((IDisposable)_httpListener).Dispose();
			_tokenSource.Cancel();
			_tokenSource.Dispose();

			if (_task.IsCompleted || _task.IsFaulted || _task.IsCanceled)
				_task.Dispose();
		}
	}
}
