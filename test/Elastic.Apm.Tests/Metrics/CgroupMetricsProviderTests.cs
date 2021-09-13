// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Metrics.MetricsProvider.CgroupMetricsProvider;

namespace Elastic.Apm.Tests.Metrics
{
	public class CgroupMetricsProviderTests
	{
		private readonly string _projectRoot;

		public CgroupMetricsProviderTests()
		{
			var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			_projectRoot = assemblyDirectory;
		}

		[Theory]
		[InlineData(964778496, "/proc/cgroup", "/proc/limited/memory", 7964778496)]
		[InlineData(964778496, "/proc/cgroup2", "/proc/sys_cgroup2", 7964778496)]
		// stat have different values to inactive_file and total_inactive_file
		[InlineData(964778496, "/proc/cgroup2_only_0", "/proc/sys_cgroup2_unlimited", null)]
		// stat have different values to inactive_file and total_inactive_file different order
		[InlineData(964778496, "/proc/cgroup2_only_memory", "/proc/sys_cgroup2_unlimited_stat_different_order", null)]
		public void TestFreeCgroupMemory(double value, string selfCGroup, string sysFsGroup, double? memLimit)
		{
			var mountInfo = GetTestFilePath(sysFsGroup);
			var tempFile = TempFile.CreateWithContents(
				sysFsGroup.StartsWith("/proc/sys_cgroup2")
					? $"30 23 0:26 / {mountInfo} rw,nosuid,nodev,noexec,relatime shared:4 - cgroup2 cgroup rw,seclabel\n"
					: $"39 30 0:35 / {mountInfo} rw,nosuid,nodev,noexec,relatime shared:10 - cgroup cgroup rw,seclabel,memory\n");

			using (tempFile)
			{
				var provider = new CgroupMetricsProvider(GetTestFilePath(selfCGroup), tempFile.Path, new NoopLogger(), new List<WildcardMatcher>());

				var samples = provider.GetSamples().ToList();

				samples.First().Samples.Should().HaveCountGreaterOrEqualTo(2);

				var inactiveFileBytesSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryStatsInactiveFileBytes);
				inactiveFileBytesSample.Should().NotBeNull();

				var memUsageSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemUsageBytes);
				memUsageSample.Should().NotBeNull();
				memUsageSample?.KeyValue.Value.Should().Be(value);

				if (memLimit.HasValue)
				{
					var memLimitSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemLimitBytes);
					memLimitSample.Should().NotBeNull();
					memLimitSample?.KeyValue.Value.Should().Be(memLimit);
				}
			}
		}

		[Theory]
		[InlineData("39 30 0:36 / /sys/fs/cgroup/memory rw,nosuid,nodev,noexec,relatime shared:10 - cgroup cgroup rw,seclabel,memory", "/sys/fs/cgroup/memory")]
		public void TestCgroup1Regex(string testString, string expected)
		{
			var actual = ApplyCgroupRegex(Cgroup1MountPoint, testString);
			actual.Should().Be(expected);
		}

		[Theory]
		[InlineData("39 30 0:36 / /sys/fs/cgroup/memory rw,nosuid,nodev,noexec,relatime shared:4 - cgroup2 cgroup rw,seclabel", "/sys/fs/cgroup/memory")]
		public void TestCgroup2Regex(string testString, string expected)
		{
			var actual = ApplyCgroupRegex(Cgroup2MountPoint, testString);
			actual.Should().Be(expected);
		}

		[Fact]
		public void TestUnlimitedCgroup1()
		{
			var cgroupMetrics = CreateUnlimitedSystemCgroupMetricsProvider("/proc/cgroup","/proc/unlimited/memory", "cgroup cgroup");
			var samples = cgroupMetrics.GetSamples().ToList();

			var memLimitSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemLimitBytes);
			memLimitSample.Should().BeNull();

			var memUsageSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemUsageBytes);
			memUsageSample.Should().NotBeNull();
			memUsageSample?.KeyValue.Value.Should().Be(964778496);
		}

		[Fact]
		public void TestUnlimitedCgroup2()
		{
			var cgroupMetrics = CreateUnlimitedSystemCgroupMetricsProvider("/proc/cgroup2","/proc/sys_cgroup2_unlimited", "cgroup2 cgroup");
			var samples = cgroupMetrics.GetSamples().ToList();

			var memLimitSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemLimitBytes);
			memLimitSample.Should().BeNull();

			var memUsageSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemUsageBytes);
			memUsageSample.Should().NotBeNull();
			memUsageSample?.KeyValue.Value.Should().Be(964778496);
		}

		/// <summary>
		/// Makes sure that CGroup metrics collection exits with a log on non Linux OSs and only collects data on Linux.
		/// </summary>
		[Fact]
		public void OsTest()
		{
			var logger = new InMemoryBlockingLogger(LogLevel.Trace);
			var cgroupMetricsProvider = new CgroupMetricsProvider(logger, new List<WildcardMatcher>());

			var samples = cgroupMetricsProvider.GetSamples();

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				samples.Should().NotBeNullOrEmpty();
				logger.Lines.Where(line => line.Contains("detected a non Linux OS, therefore Cgroup metrics will not be reported")).Should().BeNullOrEmpty();
			}
			else
			{
				samples.Should().BeNullOrEmpty();
				logger.Lines.Where(line => line.Contains("detected a non Linux OS, therefore Cgroup metrics will not be reported")).Should().NotBeNullOrEmpty();
			}

		}

		/// <summary>
		/// Converts a test path into a path to a physical test file on disk
		/// </summary>
		private string GetTestFilePath(string linuxPath) => Path.GetFullPath(Path.Combine(_projectRoot, "TestResources" + linuxPath));

		private CgroupMetricsProvider CreateUnlimitedSystemCgroupMetricsProvider(string cGroupPath, string mountPath, string cgroup)
		{
			var mountInfo = GetTestFilePath(mountPath).Replace("\\", "/");
			var tempFile = TempFile.CreateWithContents(
				$"39 30 0:35 / {mountInfo} rw,nosuid,nodev,noexec,relatime shared:10 - {cgroup} rw,seclabel,memory\n");

			return new CgroupMetricsProvider(GetTestFilePath(cGroupPath), tempFile.Path, new NoopLogger(), new List<WildcardMatcher>());
		}
	}
}
