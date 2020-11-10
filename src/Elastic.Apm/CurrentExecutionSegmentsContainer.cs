// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using Elastic.Apm.Api;

namespace Elastic.Apm
{
	internal sealed class CurrentExecutionSegmentsContainer : ICurrentExecutionSegmentsContainer
	{
		private readonly AsyncLocal<ISpan> _currentSpan = new AsyncLocal<ISpan>();
		private readonly AsyncLocal<ITransaction> _currentTransaction = new AsyncLocal<ITransaction>();

		public ISpan CurrentSpan
		{
			get => _currentSpan.Value;
			set => _currentSpan.Value = value;
		}

		public ITransaction CurrentTransaction
		{
			get => _currentTransaction.Value;
			set => _currentTransaction.Value = value;
		}
	}
}
