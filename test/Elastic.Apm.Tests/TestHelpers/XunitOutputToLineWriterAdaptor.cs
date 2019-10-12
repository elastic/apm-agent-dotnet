﻿using Elastic.Apm.Helpers;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.TestHelpers
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
