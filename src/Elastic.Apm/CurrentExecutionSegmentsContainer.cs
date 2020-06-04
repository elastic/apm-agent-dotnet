// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using Elastic.Apm.Model;

namespace Elastic.Apm
{
	internal sealed class CurrentExecutionSegmentsContainer : ICurrentExecutionSegmentsContainer
	{
		private readonly AsyncLocal<Span> _currentSpan = new AsyncLocal<Span>();
		private readonly AsyncLocal<Transaction> _currentTransaction = new AsyncLocal<Transaction>();

		public Span CurrentSpan
		{
			get => _currentSpan.Value;
			set => _currentSpan.Value = value;
		}

		public Transaction CurrentTransaction
		{
			get => _currentTransaction.Value;
			set => _currentTransaction.Value = value;
		}
	}
}
