// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics.MetricsProvider;
using JetBrains.Profiler.Api;
using static Elastic.Apm.Metrics.MetricsProvider.CgroupMetricsProvider;
using static Elastic.Apm.Tests.TestHelpers.CgroupFileHelper;

var paths = CreateDefaultCgroupFiles(CgroupVersion.CgroupV1);
UnlimitedMaxMemoryFiles(paths);

// WARMUP

var sut = TestableCgroupMetricsProvider(new NoopLogger(), new List<WildcardMatcher>(), paths.RootPath, true);
//var sut = new FreeAndTotalMemoryProvider(new NoopLogger(), new List<WildcardMatcher>());
foreach (var metricSet in sut.GetSamples())
	foreach (var _ in metricSet.Samples)
	{
	}

// PROFILING

MemoryProfiler.CollectAllocations(true);

MemoryProfiler.GetSnapshot("Before create");

sut = TestableCgroupMetricsProvider(new NoopLogger(), new List<WildcardMatcher>(), paths.RootPath, true);
//sut = new FreeAndTotalMemoryProvider(new NoopLogger(), new List<WildcardMatcher>());

MemoryProfiler.GetSnapshot("After create");

foreach (var metricSet in sut.GetSamples())
	foreach (var _ in metricSet.Samples)
	{
	}

MemoryProfiler.GetSnapshot("After get samples");

MemoryProfiler.CollectAllocations(false);

static void UnlimitedMaxMemoryFiles(CgroupPaths paths)
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

internal sealed class NoopLogger : IApmLogger
{
	public bool IsEnabled(LogLevel level) => false;
	public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter) { }
}
