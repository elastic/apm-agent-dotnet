// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Tests.Utilities
{
	public class SystemDiagnosticsTraceLineWriter : ILineWriter
	{
		private readonly string _prefix;

		public SystemDiagnosticsTraceLineWriter(string prefix = "") => _prefix = prefix;

		public void WriteLine(string text)
		{
			Trace.WriteLine(TextUtils.PrefixEveryLine(text, _prefix));
			Trace.Flush();
		}
	}
}
