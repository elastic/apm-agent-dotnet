// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Elastic.Apm.Helpers
{
	//
	// Copied and adapted from:
	// https://github.com/aspnet/AspNetIdentity/blob/b7826741279450c58b230ece98bd04b4815beabf/src/Microsoft.AspNet.Identity.Core/AsyncHelper.cs
	//
	internal static class AsyncHelper
	{
		private static readonly TaskFactory MyTaskFactory = new(CancellationToken.None,
			TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

		public static TResult RunSync<TResult>(Func<Task<TResult>> func)
		{
			var cultureUi = CultureInfo.CurrentUICulture;
			var culture = CultureInfo.CurrentCulture;
			return MyTaskFactory.StartNew(() =>
			{
				Thread.CurrentThread.CurrentCulture = culture;
				Thread.CurrentThread.CurrentUICulture = cultureUi;
				return func();
			}).Unwrap().GetAwaiter().GetResult();
		}

		public static void RunSync(Func<Task> func)
		{
			var cultureUi = CultureInfo.CurrentUICulture;
			var culture = CultureInfo.CurrentCulture;
			MyTaskFactory.StartNew(() =>
			{
				Thread.CurrentThread.CurrentCulture = culture;
				Thread.CurrentThread.CurrentUICulture = cultureUi;
				return func();
			}).Unwrap().GetAwaiter().GetResult();
		}
	}
}
