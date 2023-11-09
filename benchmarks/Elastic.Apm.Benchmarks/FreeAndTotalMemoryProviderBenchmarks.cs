// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Elastic.Apm.Helpers;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Utilities;
using static Elastic.Apm.Tests.TestHelpers.CgroupFileHelper;

namespace Elastic.Apm.Benchmarks;

[MemoryDiagnoser]
public class FreeAndTotalMemoryProviderBenchmarks
{
    private CgroupPaths _cgroupPaths;
    private FreeAndTotalMemoryProvider _freeAndTotalMemoryProvider;
    private readonly Consumer _consumer = new();

    [GlobalSetup]
    public void Setup()
    {
        _cgroupPaths = CreateDefaultCgroupFiles(CgroupVersion.CgroupV2);
        _freeAndTotalMemoryProvider = FreeAndTotalMemoryProvider
            .TestableFreeAndTotalMemoryProvider(new NoopLogger(), new List<WildcardMatcher>(), _cgroupPaths.RootPath, true);
    }

    [GlobalCleanup]
    public void Cleanup()
    => Directory.Delete(_cgroupPaths.RootPath, true);

    //[Benchmark(Baseline = true)]
    //public void GetSamplesOriginal()
    //{
    //    foreach (var metricSet in _freeAndTotalMemoryProvider.GetSamplesOriginal())
    //    {
    //        metricSet.Samples.Consume(_consumer);
    //    }
    //}

    [Benchmark]
    public void GetSamples()
    {
        foreach (var metricSet in _freeAndTotalMemoryProvider.GetSamples())
        {
            metricSet.Samples.Consume(_consumer);
        }
    }

    // WINDOWS
    //|             Method |     Mean |    Error |   StdDev |    Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
    //|------------------- |---------:|---------:|---------:|---------:|--------:|-------:|----------:|------------:|
    //| GetSamplesOriginal | 600.1 ns | 12.00 ns | 17.22 ns | baseline |         | 0.0277 |     352 B |             |
    //|         GetSamples | 553.8 ns | 10.57 ns | 12.98 ns |      -8% |    3.2% | 0.0162 |     208 B |        -41% |

    // LINUX
    //|             Method |       Mean |    Error |    StdDev |    Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
    //|------------------- |-----------:|---------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
    //| GetSamplesOriginal | 3,693.2 ns | 73.86 ns | 108.27 ns | baseline |         | 0.7324 | 0.0191 |    9216 B |             |
    //|         GetSamples |   438.7 ns |  8.57 ns |   8.41 ns |     -88% |    3.1% | 0.0215 |      - |     272 B |        -97% |
}
