// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using System.Web;
using Elastic.Apm.Model;

namespace Elastic.Apm.AspNetFullFramework
{
	/// <summary>
	/// An <see cref="ICurrentExecutionSegmentsContainer"/> that stores the current transaction
	/// and current span in both async local storage and the current <see cref="HttpContext.Items"/>
	/// </summary>
	internal sealed class HttpContextCurrentExecutionSegmentsContainer : ICurrentExecutionSegmentsContainer
	{
		private readonly AsyncLocal<Span> _currentSpan = new AsyncLocal<Span>();
		private readonly AsyncLocal<Transaction> _currentTransaction = new AsyncLocal<Transaction>();

		private const string CurrentSpanKey = "Elastic.Apm.Agent.CurrentSpan";
		private const string CurrentTransactionKey = "Elastic.Apm.Agent.CurrentTransaction";

		public Span CurrentSpan
		{
			get => _currentSpan.Value ?? HttpContext.Current?.Items[CurrentSpanKey] as Span;
			set
			{
				_currentSpan.Value = value;
				var httpContext = HttpContext.Current;
				if (httpContext != null)
					httpContext.Items[CurrentSpanKey] = value;
			}
		}

		public Transaction CurrentTransaction
		{
			get => _currentTransaction.Value ?? HttpContext.Current?.Items[CurrentTransactionKey] as Transaction;
			set
			{
				_currentTransaction.Value = value;
				var httpContext = HttpContext.Current;
				if (httpContext != null)
					httpContext.Items[CurrentTransactionKey] = value;
			}
		}
	}
}
