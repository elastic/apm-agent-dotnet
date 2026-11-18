// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.AspNetFullFramework
{
	internal static class AppPoolNameParser
	{
		/// <summary>
		/// Returns the app pool name passed to the IIS worker process (w3wp) as "-ap &lt;name&gt;",
		/// or <c>null</c> when the argument is not present.
		/// </summary>
		internal static string FromCommandLineArgs(string[] args)
		{
			if (args is null)
				return null;

			for (var i = 0; i < args.Length - 1; i++)
			{
				if (!string.Equals(args[i], "-ap", StringComparison.OrdinalIgnoreCase))
					continue;

				var name = args[i + 1]?.Trim().Trim('"').Trim();
				return string.IsNullOrEmpty(name) ? null : name;
			}

			return null;
		}
	}
}
