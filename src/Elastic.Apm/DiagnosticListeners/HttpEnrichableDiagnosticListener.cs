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
	internal abstract class HttpEnrichableDiagnosticListener : DiagnosticListenerBase
	{
		private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
		private readonly List<IHttpSpanEnricher> _enrichers = new List<IHttpSpanEnricher>();

		/// <summary>
		/// Gets the enrichers, entering a read lock that is exited when the
		/// <see cref="Enrichers"/> is disposed.
		/// </summary>
		/// <returns>a new instance of <see cref="Enrichers"/></returns>
		protected Enrichers GetEnrichers()
		{
			_lock.EnterReadLock();
			return new Enrichers(_enrichers, _lock);
		}

		/// <summary>
		/// Adds an enricher to the collection, within a write lock
		/// </summary>
		/// <param name="enricher"></param>
		public void AddEnricher(IHttpSpanEnricher enricher)
		{
			_lock.EnterWriteLock();
			try
			{
				_enrichers.Add(enricher);
			}
			finally
			{
				_lock.ExitWriteLock();
			}
		}

		protected HttpEnrichableDiagnosticListener(IApmAgent apmAgent) : base(apmAgent) { }

		/// <summary>
		/// A collection of enrichers to enumerate
		/// </summary>
		protected class Enrichers : IDisposable, IEnumerable<IHttpSpanEnricher>
		{
			private readonly List<IHttpSpanEnricher> _enrichers;
			private readonly ReaderWriterLockSlim _readerWriterLockSlim;

			public Enrichers(List<IHttpSpanEnricher> enrichers, ReaderWriterLockSlim readerWriterLockSlim)
			{
				_enrichers = enrichers;
				_readerWriterLockSlim = readerWriterLockSlim;
			}

			public void Dispose() => _readerWriterLockSlim.ExitReadLock();

			public IEnumerator<IHttpSpanEnricher> GetEnumerator() => _enrichers.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}
