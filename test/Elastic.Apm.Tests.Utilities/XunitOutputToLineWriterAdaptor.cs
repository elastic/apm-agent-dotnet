// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.Utilities
{
	public class XunitOutputToLineWriterAdaptor : ILineWriter
	{
		private readonly string _prefix;
		private readonly ITestOutputHelper _xUnitOutputHelper;

		public XunitOutputToLineWriterAdaptor(ITestOutputHelper xUnitOutputHelper, string prefix = "")
		{
			_xUnitOutputHelper = xUnitOutputHelper;
			_prefix = prefix;
		}

		public void WriteLine(string text) => _xUnitOutputHelper.WriteLine(TextUtils.PrefixEveryLine(text, _prefix));
	}
}
