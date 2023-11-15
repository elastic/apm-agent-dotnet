// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Metrics.MetricsProvider.CgroupMetricsProvider;
using static Elastic.Apm.Tests.TestHelpers.CgroupFileHelper;

namespace Elastic.Apm.Tests.Metrics
{
	public class CgroupMetricsProviderTests
	{
		[Fact]
		public void TestCgroup1Regex()
		{
			var actual = ApplyCgroupRegex(Cgroup1MountPoint, "39 30 0:36 / /sys/fs/cgroup/memory rw,nosuid,nodev,noexec,relatime shared:10 - cgroup cgroup rw,seclabel,memory");
			actual.Should().Be("/sys/fs/cgroup/memory");
		}

		[Fact]
		public void TestCgroup2Regex()
		{
			var actual = ApplyCgroupRegex(Cgroup2MountPoint, "39 30 0:36 / /sys/fs/cgroup/memory rw,nosuid,nodev,noexec,relatime shared:4 - cgroup2 cgroup rw,seclabel");
			actual.Should().Be("/sys/fs/cgroup/memory");
		}

		[Fact]
		public void TestLimitedCgroup1()
		{
			using var paths = CreateDefaultCgroupFiles(CgroupVersion.CgroupV1);

			var sut = TestableCgroupMetricsProvider(new NoopLogger(), new List<WildcardMatcher>(), paths.RootPath, true);
			var samples = sut.GetSamples().ToList();

			var memLimitSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemLimitBytes);
			memLimitSample.Should().NotBeNull();
			memLimitSample.KeyValue.Value.Should().Be(DefaultMemoryLimitBytes);

			var memUsageSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemUsageBytes);
			memUsageSample.Should().NotBeNull();
			memUsageSample.KeyValue.Value.Should().Be(DefaultMemoryUsageBytes);
		}

		[Fact]
		public void TestLimitedCgroup2()
		{
			using var paths = CreateDefaultCgroupFiles(CgroupVersion.CgroupV2);

			var sut = TestableCgroupMetricsProvider(new NoopLogger(), new List<WildcardMatcher>(), paths.RootPath, true);
			var samples = sut.GetSamples().ToList();

			var memLimitSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemLimitBytes);
			memLimitSample.Should().NotBeNull();
			memLimitSample.KeyValue.Value.Should().Be(DefaultMemoryLimitBytes);

			var memUsageSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemUsageBytes);
			memUsageSample.Should().NotBeNull();
			memUsageSample.KeyValue.Value.Should().Be(DefaultMemoryUsageBytes);
		}

		[Fact]
		public void TestUnlimitedCgroup1()
		{
			using var paths = CreateDefaultCgroupFiles(CgroupVersion.CgroupV1);
			UnlimitedMaxMemoryFiles(paths);

			var sut = TestableCgroupMetricsProvider(new NoopLogger(), new List<WildcardMatcher>(), paths.RootPath, true);
			var samples = sut.GetSamples().ToList();

			var memLimitSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemLimitBytes);
			memLimitSample.Should().NotBeNull();
			memLimitSample.KeyValue.Value.Should().Be(DefaultMemInfoTotalBytes);

			var memUsageSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemUsageBytes);
			memUsageSample.Should().NotBeNull();
			memUsageSample.KeyValue.Value.Should().Be(DefaultMemoryUsageBytes);
		}

		[Fact]
		public void TestUnlimitedCgroup2()
		{
			using var paths = CreateDefaultCgroupFiles(CgroupVersion.CgroupV2);
			UnlimitedMaxMemoryFiles(paths);

			var sut = TestableCgroupMetricsProvider(new NoopLogger(), new List<WildcardMatcher>(), paths.RootPath, true);
			var samples = sut.GetSamples().ToList();

			var memLimitSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemLimitBytes);
			memLimitSample.Should().NotBeNull();
			memLimitSample.KeyValue.Value.Should().Be(DefaultMemInfoTotalBytes);

			var memUsageSample = samples.First().Samples.SingleOrDefault(s => s.KeyValue.Key == SystemProcessCgroupMemoryMemUsageBytes);
			memUsageSample.Should().NotBeNull();
			memUsageSample.KeyValue.Value.Should().Be(DefaultMemoryUsageBytes);
		}

		private void UnlimitedMaxMemoryFiles(CgroupPaths paths)
		{
			if (paths.CgroupVersion == CgroupVersion.CgroupV1)
			{
				using var sr = new StreamWriter(File.Create(Path.Combine(paths.CgroupV1MemoryControllerPath, "memory.limit_in_bytes")));
				sr.WriteAsync($"9223372036854771712\n");
			}

			if (paths.CgroupVersion == CgroupVersion.CgroupV2)
			{
				using var sr = new StreamWriter(File.Create(Path.Combine(paths.CgroupV2SlicePath, "memory.max")));
				sr.WriteAsync($"max\n");
			}
		}

		private void ReplaceMemStatInactiveFile(CgroupPaths paths, double inactiveFileValue, double totalInactiveFileValue, bool inactiveFirst)
		{
			using var memoryStat = File.CreateText(Path.Combine(paths.CgroupV1MemoryControllerPath, "memory.stat"));
			var sb = new StringBuilder();

			sb.Append("cache 10407936").Append("\n");
			sb.Append("rss 778842112").Append("\n");
			sb.Append("rss_huge 0").Append("\n");
			sb.Append("shmem 0").Append("\n");
			sb.Append("mapped_file 0").Append("\n");
			sb.Append("dirty 0").Append("\n");
			sb.Append("writeback 0").Append("\n");
			sb.Append("swap 0").Append("\n");
			sb.Append("pgpgin 234465").Append("\n");
			sb.Append("pgpgout 41732").Append("\n");
			sb.Append("pgfault 233838").Append("\n");
			sb.Append("pgmajfault 0").Append("\n");
			sb.Append("inactive_anon 0").Append("\n");
			sb.Append("active_anon 778702848").Append("\n");

			if (inactiveFirst)
			{
				sb.AppendLine($"inactive_file {inactiveFileValue}");
				sb.AppendLine($"total_inactive_file {totalInactiveFileValue}");
			}
			else
			{
				sb.AppendLine($"total_inactive_file {totalInactiveFileValue}");
				sb.AppendLine($"inactive_file {inactiveFileValue}");
			}

			sb.Append("active_file 0").Append("\n");
			sb.Append("unevictable 0").Append("\n");
			sb.Append("hierarchical_memory_limit 1073741824").Append("\n");
			sb.Append("hierarchical_memsw_limit 2147483648").Append("\n");
			sb.Append("total_cache 10407936").Append("\n");
			sb.Append("total_rss 778842112").Append("\n");
			sb.Append("total_rss_huge 0").Append("\n");
			sb.Append("total_shmem 0").Append("\n");
			sb.Append("total_mapped_file 0").Append("\n");
			sb.Append("total_dirty 0").Append("\n");
			sb.Append("total_writeback 0").Append("\n");
			sb.Append("total_swap 0").Append("\n");
			sb.Append("total_pgpgin 234465").Append("\n");
			sb.Append("total_pgpgout 41732").Append("\n");
			sb.Append("total_pgfault 233838").Append("\n");
			sb.Append("total_pgmajfault 0").Append("\n");
			sb.Append("total_inactive_anon 0").Append("\n");
			sb.Append("total_active_anon 778702848").Append("\n");
			sb.Append("total_active_file 0").Append("\n");
			sb.Append("total_unevictable 0").Append("\n");
			sb.Append("recent_rotated_anon 231947").Append("\n");
			sb.Append("recent_rotated_file 2").Append("\n");
			sb.Append("recent_scanned_anon 231947").Append("\n");
			sb.Append("recent_scanned_file 2622").Append("\n");
			memoryStat.Write(sb.ToString());
			memoryStat.Flush();
		}
	}
}
