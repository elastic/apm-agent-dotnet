// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Reflection;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.Utilities.XUnit
{
	/// <summary>
	/// Captures <see cref="Activity.DefaultIdFormat"/> before the test run and restores it after.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class CaptureRestoreActivityIdFormat : BeforeAfterTestAttribute
	{
		private ActivityIdFormat _originalFormat;

		public override void Before(MethodInfo methodUnderTest) => _originalFormat = Activity.DefaultIdFormat;

		public override void After(MethodInfo methodUnderTest) => Activity.DefaultIdFormat = _originalFormat;
	}
}
