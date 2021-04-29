// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Elastic.Apm.DiagnosticListeners
{
	internal class HttpTraceConfiguration
	{
		private readonly ReaderWriterLockSlim _tracersLock = new ReaderWriterLockSlim();
		private readonly List<IHttpSpanTracer> _tracers = new List<IHttpSpanTracer>();
		private volatile bool _captureSpan;
		private volatile bool _subscribed;

		/// <summary>
		/// Whether a Http diagnostic listener has subscribed to HTTP requests.
		/// </summary>
		public bool Subscribed
		{
			get => _subscribed;
			set => _subscribed = value;
		}

		/// <summary>
		/// Whether a HTTP span should be captured.
		/// A HTTP diagnostic listener is used to listen for all HTTP requests, but it may be started
		/// to capture spans for HTTP requests only to specific known services. In the event a HTTP request is not
		/// to a known service, <see cref="CaptureSpan"/> determines whether a HTTP span should be captured.
		/// </summary>
		public bool CaptureSpan
		{
			get => _captureSpan;
			set => _captureSpan = value;
		}

		/// <summary>
		/// Gets the HTTP span tracers, entering a read lock that is exited when the
		/// <see cref="HttpTracers"/> is disposed.
		/// </summary>
		/// <returns>a new instance of <see cref="HttpTracers"/></returns>
		public HttpTracers GetTracers()
		{
			_tracersLock.EnterReadLock();
			return new HttpTracers(_tracers, _tracersLock);
		}

		/// <summary>
		/// Adds a http span tracer to the collection, within a write lock
		/// </summary>
		/// <param name="tracer">The tracer to add</param>
		public void AddTracer(IHttpSpanTracer tracer)
		{
			_tracersLock.EnterWriteLock();
			try
			{
				_tracers.Add(tracer);
			}
			finally
			{
				_tracersLock.ExitWriteLock();
			}
		}

		/// <summary>
		/// A collection of http tracers to enumerate
		/// </summary>
		internal class HttpTracers : IDisposable, IEnumerable<IHttpSpanTracer>
		{
			private readonly List<IHttpSpanTracer> _tracers;
			private readonly ReaderWriterLockSlim _readerWriterLockSlim;

			public HttpTracers(List<IHttpSpanTracer> tracers, ReaderWriterLockSlim readerWriterLockSlim)
			{
				_tracers = tracers;
				_readerWriterLockSlim = readerWriterLockSlim;
			}

			public void Dispose() => _readerWriterLockSlim.ExitReadLock();

			public IEnumerator<IHttpSpanTracer> GetEnumerator() => _tracers.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}
