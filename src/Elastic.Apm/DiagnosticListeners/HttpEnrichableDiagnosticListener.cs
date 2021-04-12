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
		private readonly List<IHttpSpanCreator> _creators = new List<IHttpSpanCreator>();
		private volatile bool _createSpan;

		public bool CreateSpan
		{
			get => _createSpan;
			set => _createSpan = value;
		}

		/// <summary>
		/// Gets the span creators, entering a read lock that is exited when the
		/// <see cref="Creators"/> is disposed.
		/// </summary>
		/// <returns>a new instance of <see cref="Creators"/></returns>
		protected Creators GetCreators()
		{
			_lock.EnterReadLock();
			return new Creators(_creators, _lock);
		}

		/// <summary>
		/// Adds a http span creator to the collection, within a write lock
		/// </summary>
		/// <param name="creator"></param>
		public void AddCreator(IHttpSpanCreator creator)
		{
			_lock.EnterWriteLock();
			try
			{
				_creators.Add(creator);
			}
			finally
			{
				_lock.ExitWriteLock();
			}
		}

		protected HttpEnrichableDiagnosticListener(IApmAgent apmAgent, bool createSpan) : base(apmAgent) =>
			_createSpan = createSpan;

		/// <summary>
		/// A collection of creators to enumerate
		/// </summary>
		protected class Creators : IDisposable, IEnumerable<IHttpSpanCreator>
		{
			private readonly List<IHttpSpanCreator> _creators;
			private readonly ReaderWriterLockSlim _readerWriterLockSlim;

			public Creators(List<IHttpSpanCreator> creators, ReaderWriterLockSlim readerWriterLockSlim)
			{
				_creators = creators;
				_readerWriterLockSlim = readerWriterLockSlim;
			}

			public void Dispose() => _readerWriterLockSlim.ExitReadLock();

			public IEnumerator<IHttpSpanCreator> GetEnumerator() => _creators.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}
