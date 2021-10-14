// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Elastic.Apm.Profiler.Managed.CallTarget.Handlers.Continuations
{
	internal struct NoThrowAwaiter : ICriticalNotifyCompletion
	{
		private readonly Task _task;

		public NoThrowAwaiter(Task task) => _task = task;

		public bool IsCompleted => _task.IsCompleted;

		public NoThrowAwaiter GetAwaiter() => this;

		public void GetResult()
		{
		}

		public void OnCompleted(Action continuation) => _task.GetAwaiter().OnCompleted(continuation);

		public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
	}
}
