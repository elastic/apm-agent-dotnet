// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using System.Threading;

namespace Elastic.Apm.Helpers
{
	internal class DbgUtils
	{
		internal static string CurrentThreadDesc =>
			Thread.CurrentThread.Name == null
				? $"managed ID: {Thread.CurrentThread.ManagedThreadId}"
				: $"`{Thread.CurrentThread.Name}' (managed ID: {Thread.CurrentThread.ManagedThreadId})";

		internal static string CurrentMethodName([CallerMemberName] string caller = null) => caller;

		internal static string CurrentDbgContext(string className = null, [CallerMemberName] string caller = null) =>
			className == null ? $"Thread: {CurrentThreadDesc}, {caller}" : $"Thread: {CurrentThreadDesc}, {className}.{caller}";
	}
}
