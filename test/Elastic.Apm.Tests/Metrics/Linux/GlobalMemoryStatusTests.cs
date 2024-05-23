// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Text;
using Elastic.Apm.Tests.TestHelpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.Metrics.Linux;

public class GlobalMemoryStatusTests
{
	private readonly ITestOutputHelper _output;

	public GlobalMemoryStatusTests(ITestOutputHelper output) => _output = output;

	[Fact]
	public void GlobalMemoryStatus_ReturnsExpectedValues()
	{
		var rootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var procPath = Path.Combine(rootPath, "proc");

		Directory.CreateDirectory(procPath);

		var sb = new StringBuilder();
		sb.Append($"MemTotal:       {CgroupFileHelper.DefaultMemInfoTotalBytes / 1024} kB").Append("\n");
		sb.Append("MemFree:         4806144 kB").Append("\n");
		sb.Append("Buffers:          211756 kB").Append("\n");
		sb.Append("Cached:          1071092 kB").Append("\n");
		sb.Append("SwapTotal:       4194296 kB").Append("\n");
		sb.Append("SwapFree:        4194296 kB").Append("\n");
		sb.Append($"MemAvailable:    {CgroupFileHelper.DefaultMemInfoAvailableBytes / 1024} kB").Append("\n");

		using (var memInfo = new StreamWriter(File.Create(Path.Combine(procPath, "meminfo"))))
		{
			memInfo.Write(sb.ToString());
			memInfo.Flush();
		}

		var (total, available) = Apm.Metrics.Linux.GlobalMemoryStatus
			.GetTotalAndAvailableSystemMemory(new NoopLogger(), rootPath, ignoreOs: true);

		_output.WriteLine($"Root Path: {rootPath}");
		_output.WriteLine($"Available: {available}");
		_output.WriteLine($"Total: {total}");

		available.Should().Be(CgroupFileHelper.DefaultMemInfoAvailableBytes);
		total.Should().Be(CgroupFileHelper.DefaultMemInfoTotalBytes);

		Directory.Delete(rootPath, true);
	}

	[Fact]
	public void GlobalMemoryStatus_ReturnsExpectedValue_WhenTotalMemoryNotPresent()
	{
		var rootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var procPath = Path.Combine(rootPath, "proc");

		Directory.CreateDirectory(procPath);

		var sb = new StringBuilder();
		sb.Append("MemFree:         4806144 kB").Append("\n");
		sb.Append("Buffers:          211756 kB").Append("\n");
		sb.Append("Cached:          1071092 kB").Append("\n");
		sb.Append("SwapTotal:       4194296 kB").Append("\n");
		sb.Append("SwapFree:        4194296 kB").Append("\n");
		sb.Append($"MemAvailable:    {CgroupFileHelper.DefaultMemInfoAvailableBytes / 1024} kB").Append("\n");

		using (var memInfo = new StreamWriter(File.Create(Path.Combine(procPath, "meminfo"))))
		{
			memInfo.Write(sb.ToString());
			memInfo.Flush();
		}

		var (total, available) = Apm.Metrics.Linux.GlobalMemoryStatus
			.GetTotalAndAvailableSystemMemory(new NoopLogger(), rootPath, ignoreOs: true);

		_output.WriteLine($"Root Path: {rootPath}");
		_output.WriteLine($"Available: {available}");
		_output.WriteLine($"Total: {total}");

		available.Should().Be(CgroupFileHelper.DefaultMemInfoAvailableBytes);
		total.Should().Be(-1);

		Directory.Delete(rootPath, true);
	}

	[Fact]
	public void GlobalMemoryStatus_ReturnsExpectedValue_WhenAvailableMemoryNotPresent()
	{
		var rootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		var procPath = Path.Combine(rootPath, "proc");

		Directory.CreateDirectory(procPath);

		var sb = new StringBuilder();
		sb.Append($"MemTotal:       {CgroupFileHelper.DefaultMemInfoTotalBytes / 1024} kB").Append("\n");
		sb.Append("MemFree:         4806144 kB").Append("\n");
		sb.Append("Buffers:          211756 kB").Append("\n");
		sb.Append("Cached:          1071092 kB").Append("\n");
		sb.Append("SwapTotal:       4194296 kB").Append("\n");
		sb.Append("SwapFree:        4194296 kB").Append("\n");

		using (var memInfo = new StreamWriter(File.Create(Path.Combine(procPath, "meminfo"))))
		{
			memInfo.Write(sb.ToString());
			memInfo.Flush();
		}

		var (total, available) = Apm.Metrics.Linux.GlobalMemoryStatus
			.GetTotalAndAvailableSystemMemory(new NoopLogger(), rootPath, ignoreOs: true);

		_output.WriteLine($"Root Path: {rootPath}");
		_output.WriteLine($"Available: {available}");
		_output.WriteLine($"Total: {total}");

		available.Should().Be(-1);
		total.Should().Be(CgroupFileHelper.DefaultMemInfoTotalBytes);

		Directory.Delete(rootPath, true);
	}
}
