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
using static Elastic.Apm.Metrics.MetricsProvider.CgroupMetricsProvider;
using static Elastic.Apm.Tests.TestHelpers.CgroupFileHelper;

namespace Elastic.Apm.Benchmarks;

[MemoryDiagnoser]
public class CgroupMetricsProviderBenchmarks
{
	private CgroupPaths _cgroupPaths;
	private CgroupMetricsProvider _cgroupMetricsProvider;
	private readonly Consumer _consumer = new();

	[GlobalSetup]
	public void Setup()
	{
		_cgroupPaths = CreateDefaultCgroupFiles(CgroupVersion.CgroupV2);
		_cgroupMetricsProvider = TestableCgroupMetricsProvider(new NoopLogger(), new List<WildcardMatcher>(), _cgroupPaths.RootPath, true);
	}

	[GlobalSetup(Target = nameof(GetSamplesUnlimited))]
	public void SetupUnlimited()
	{
		_cgroupPaths = CreateDefaultCgroupFiles(CgroupVersion.CgroupV2);
		UnlimitedMaxMemoryFiles(_cgroupPaths);
		_cgroupMetricsProvider = TestableCgroupMetricsProvider(new NoopLogger(), new List<WildcardMatcher>(), _cgroupPaths.RootPath, true);
	}

	private static void UnlimitedMaxMemoryFiles(CgroupPaths paths)
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

	[GlobalCleanup]
	public void Cleanup()
		=> Directory.Delete(_cgroupPaths.RootPath, true);

	[Benchmark]
	public void GetSamples()
	{
		foreach (var metricSet in _cgroupMetricsProvider.GetSamples())
		{
			metricSet.Samples.Consume(_consumer);
		}
	}

	// WINDOWS:
	//|              Method |     Mean |   Error |   StdDev |    Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
	//|-------------------- |---------:|--------:|---------:|---------:|--------:|-------:|----------:|------------:|
	//|  GetSamplesOriginal | 433.0 us | 8.39 us | 11.20 us | baseline |         | 1.9531 |   29.4 KB |             |
	//| GetSamplesOptimised | 409.9 us | 5.11 us |  4.53 us |      -5% |    3.0% |      - |   1.47 KB |        -95% |

	// WINDOWS: After yield return
	//|              Method |     Mean |   Error |   StdDev |    Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
	//|-------------------- |---------:|--------:|---------:|---------:|--------:|-------:|----------:|------------:|
	//|  GetSamplesOriginal | 429.1 us | 8.08 us | 13.93 us | baseline |         | 1.9531 |  29.44 KB |             |
	//| GetSamplesOptimised | 419.7 us | 6.83 us |  6.39 us |      -5% |    3.3% |      - |   1.46 KB |        -95% |

	// WINDOWS: Remove metric
	//|              Method |     Mean |   Error |  StdDev | Allocated | Alloc Ratio |
	//|-------------------- |---------:|--------:|--------:|----------:|------------:|
	//|          GetSamples | 296.9 us | 3.46 us | 3.23 us |   1.05 KB |        -96% |

	// LINUX: After yield return
	//|              Method |     Mean |     Error |    StdDev |    Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
	//|-------------------- |---------:|----------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
	//|  GetSamplesOriginal | 9.643 us | 0.1600 us | 0.1418 us | baseline |         | 2.3346 | 0.0610 |   29328 B |             |
	//| GetSamplesOptimised | 5.525 us | 0.0580 us | 0.0569 us |     -43% |    1.6% | 0.0534 |      - |     680 B |        -98% |

	// LINUX: Remove metric
	//|              Method |     Mean |     Error |    Ratio |    StdDev |   Gen0 | Allocated | Alloc Ratio |
	//|-------------------- |---------:|----------:|---------:|----------:|-------:|----------:|             |
	//|          GetSamples | 4.363 us | 0.0458 us |     -55% | 0.0382 us | 0.0381 |     496 B |        -98% |

	[Benchmark]
	public void GetSamplesUnlimited()
	{
		foreach (var metricSet in _cgroupMetricsProvider.GetSamples())
		{
			metricSet.Samples.Consume(_consumer);
		}
	}

	// WINDOWS:
	//|              Method |     Mean |   Error |  StdDev |   Gen0 | Allocated |
	//|-------------------- |---------:|--------:|--------:|-------:|----------:|
	//| GetSamplesUnlimited | 467.1 us | 9.28 us | 9.11 us | 0.4883 |    9.5 KB |

	// WINDOWS: Optimised
	//|              Method |     Mean |   Error |  StdDev | Allocated |
	//|-------------------- |---------:|--------:|--------:|----------:|
	//| GetSamplesUnlimited | 444.9 us | 8.46 us | 8.69 us |   1.88 KB |

	// WINDOWS: Remove metric
	//|              Method |     Mean |   Error |  StdDev | Allocated | Alloc Ratio |
	//|-------------------- |---------:|--------:|--------:|----------:|             |
	//| GetSamplesUnlimited | 305.4 us | 5.04 us | 4.71 us |   1.43 KB |        -85% |

	// LINUX: Optimised
	//|              Method |     Mean |     Error |    StdDev |   Gen0 | Allocated |
	//|-------------------- |---------:|----------:|----------:|-------:|----------:|
	//| GetSamplesUnlimited | 7.217 us | 0.1375 us | 0.1350 us | 0.0687 |     864 B |
	//** NOTE: This includes some overhead (176 bytes) for building the test path which is not incurred
	//in production. This is there equivient to the limited benchmark when this is taken into account.

	// LINUX: Remove metric
	//|              Method |     Mean |     Error |    StdDev |   Gen0 | Allocated |
	//|-------------------- |---------:|----------:|----------:|-------:|----------:|
	//| GetSamplesUnlimited | 4.908 us | 0.0736 us | 0.0688 us | 0.0534 |     680 B |
}
