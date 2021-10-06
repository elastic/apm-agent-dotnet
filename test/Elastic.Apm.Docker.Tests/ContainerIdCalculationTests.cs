// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.Text;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Docker.Tests
{
	public class ContainerIdCalculationTests
	{
		[Theory]
		[InlineData("13:name=systemd:/docker/ae8d0ef9c8757a42d01e994b0ffe0e93e26ece256f68cd850467836c83202367",
			"ae8d0ef9c8757a42d01e994b0ffe0e93e26ece256f68cd850467836c83202367")]
		[InlineData("12:devices:/docker/051e2ee0bce99116029a13df4a9e943137f19f957f38ac02d6bad96f9b700f76",
			"051e2ee0bce99116029a13df4a9e943137f19f957f38ac02d6bad96f9b700f76")]
		[InlineData("1:name=systemd:/system.slice/docker-cde7c2bab394630a42d73dc610b9c57415dced996106665d427f6d0566594411.scope",
			"cde7c2bab394630a42d73dc610b9c57415dced996106665d427f6d0566594411")]
		[InlineData("1:name=systemd:/kubepods/besteffort/pode9b90526-f47d-11e8-b2a5-080027b9f4fb/15aa6e53-b09a-40c7-8558-c6c31e36c88a",
			"15aa6e53-b09a-40c7-8558-c6c31e36c88a")]
		[InlineData(
			"1:name=systemd:/kubepods.slice/kubepods-burstable.slice/kubepods-burstable-pod90d81341_92de_11e7_8cf2_507b9d4141fa.slice/crio-2227daf62df6694645fee5df53c1f91271546a9560e8600a525690ae252b7f63.scope",
			"2227daf62df6694645fee5df53c1f91271546a9560e8600a525690ae252b7f63")]
		[InlineData(
			"12:pids:/kubepods/kubepods/besteffort/pod0e886e9a-3879-45f9-b44d-86ef9df03224/244a65edefdffe31685c42317c9054e71dc1193048cf9459e2a4dd35cbc1dba4",
			"244a65edefdffe31685c42317c9054e71dc1193048cf9459e2a4dd35cbc1dba4")]
		[InlineData(
			"10:cpuset:/kubepods/pod5eadac96-ab58-11ea-b82b-0242ac110009/7fe41c8a2d1da09420117894f11dd91f6c3a44dfeb7d125dc594bd53468861df",
			"7fe41c8a2d1da09420117894f11dd91f6c3a44dfeb7d125dc594bd53468861df")]
		[InlineData(
			"9:freezer:/kubepods.slice/kubepods-pod22949dce_fd8b_11ea_8ede_98f2b32c645c.slice/docker-b15a5bdedd2e7645c3be271364324321b908314e4c77857bbfd32a041148c07f.scope",
			"b15a5bdedd2e7645c3be271364324321b908314e4c77857bbfd32a041148c07f")]
		public void TestCGroupContent(string cGroupContent, string expectedContainerId)
		{
			if (!File.Exists("/proc/self/cgroup")) return; //only run in Docker - this check can be improved

			var noopLogger = new NoopLogger();
			var systemInfoHelper = new TestSystemInfoHelper(noopLogger, cGroupContent);

			var systemInfo = systemInfoHelper.GetSystemInfo(null);
			systemInfo.Should().NotBeNull();
			systemInfo.Container.Should().NotBeNull();
			systemInfo.Container.Id.Should().Be(expectedContainerId);
		}

		[Fact]
		public void TestCGroupContentWithInvalidData()
		{
			if (!File.Exists("/proc/self/cgroup")) return; //only run in Docker - this check can be improved

			var noopLogger = new NoopLogger();
			var systemInfoHelper = new TestSystemInfoHelper(noopLogger, "asdf:invalid-dockerid:243543");

			var systemInfo = systemInfoHelper.GetSystemInfo(null);

			systemInfo.Container.Should().BeNull();
		}
	}

	internal class TestSystemInfoHelper : SystemInfoHelper
	{
		private readonly string _lineInCroup;

		public TestSystemInfoHelper(IApmLogger logger, string lineInCroup) : base(logger)
			=> _lineInCroup = lineInCroup;

		protected override StreamReader GetCGroupAsStream()
			=> new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(_lineInCroup)));
	}
}
