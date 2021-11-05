// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Runtime.InteropServices;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class PlatformDetectionTests
	{
		/// <summary>
		/// Makes sure that Service.Runtime.Name is set to the correct runtime name.
		/// </summary>
		[Fact]
		public void RuntimeName()
		{
			var mockPayloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender)))
				agent.Tracer.CaptureTransaction("TestTransaction", "Test", () => { });

			switch (RuntimeInformation.FrameworkDescription)
			{
				case { } str when str.StartsWith(Runtime.MonoName, StringComparison.OrdinalIgnoreCase):
					mockPayloadSender.FirstTransaction.Service.Runtime.Name.Should().Be(Runtime.MonoName);
					break;
				case { } str when str.StartsWith(Runtime.DotNetFullFrameworkName, StringComparison.OrdinalIgnoreCase):
					mockPayloadSender.FirstTransaction.Service.Runtime.Name.Should().Be(Runtime.DotNetFullFrameworkName);
					break;
				case { } str when str.StartsWith(Runtime.DotNetCoreName, StringComparison.OrdinalIgnoreCase):
					mockPayloadSender.FirstTransaction.Service.Runtime.Name.Should().Be(Runtime.DotNetCoreName);
					break;
				case { } str when str.StartsWith(Runtime.DotNetName, StringComparison.OrdinalIgnoreCase)
					&& !str.StartsWith(Runtime.DotNetFullFrameworkName, StringComparison.OrdinalIgnoreCase):
					mockPayloadSender.FirstTransaction.Service.Runtime.Name.Should().Be(Runtime.DotNetName + $" {RuntimeInformation.FrameworkDescription.Substring(5).Split('.')[0]}");
					break;
			}
		}
	}
}
