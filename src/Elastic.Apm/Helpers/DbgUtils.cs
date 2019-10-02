using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Elastic.Apm.Helpers
{
	internal class DbgUtils
	{
		internal static string CurrentMethodName([CallerMemberName] string caller = null) => caller;

		internal static string CurrentThreadDesc =>
			Thread.CurrentThread.Name == null
				? $"managed ID: {Thread.CurrentThread.ManagedThreadId}"
				: $"`{Thread.CurrentThread.Name}' (managed ID: {Thread.CurrentThread.ManagedThreadId})";

		internal static string CurrentDbgContext(string className = null, [CallerMemberName] string caller = null) =>
			className == null ? $"Thread: {CurrentThreadDesc}, {caller}" : $"Thread: {CurrentThreadDesc}, {className}.{caller}";
	}
}
