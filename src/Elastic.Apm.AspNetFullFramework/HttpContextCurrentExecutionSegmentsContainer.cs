// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using System.Web;
using Elastic.Apm.Api;
using Elastic.Apm.Model;

namespace Elastic.Apm.AspNetFullFramework
{
	/// <summary>
	/// An <see cref="ICurrentExecutionSegmentsContainer"/> that stores the current transaction
	/// and current span in both async local storage and the current <see cref="HttpContext.Items"/>
	/// </summary>
	internal sealed class HttpContextCurrentExecutionSegmentsContainer : ICurrentExecutionSegmentsContainer
	{
		private readonly AsyncLocal<ISpan> _currentSpan = new AsyncLocal<ISpan>();
		private readonly AsyncLocal<ITransaction> _currentTransaction = new AsyncLocal<ITransaction>();

		private const string CurrentSpanKey = "Elastic.Apm.Agent.CurrentSpan";
		private const string CurrentTransactionKey = "Elastic.Apm.Agent.CurrentTransaction";

		public ISpan CurrentSpan
		{
			get => _currentSpan.Value ?? HttpContext.Current?.Items[CurrentSpanKey] as ISpan;
			set
			{
				_currentSpan.Value = value;
				var httpContext = HttpContext.Current;
				if (httpContext != null)
					httpContext.Items[CurrentSpanKey] = value;
			}
		}

		public ITransaction CurrentTransaction
		{
			get => _currentTransaction.Value ?? HttpContext.Current?.Items[CurrentTransactionKey] as ITransaction;
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
