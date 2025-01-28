// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

#if NET8_0_OR_GREATER
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;
#else
using System.Globalization;
#endif

namespace Elastic.Apm.Metrics.MetricsProvider
{
	/// <summary>
	/// Provides cgroup metrics, if applicable
	/// </summary>
	internal class CgroupMetricsProvider : IMetricsProvider
	{
		private const string Cgroup1MaxMemory = "memory.limit_in_bytes";
		private const string Cgroup1Unlimited = "9223372036854771712";
		private const string Cgroup1UsedMemory = "memory.usage_in_bytes";
		private const string Cgroup2MaxMemory = "memory.max";
		private const string Cgroup2Unlimited = "max";
		private const string Cgroup2UsedMemory = "memory.current";
		private const string CgroupMemoryStat = "memory.stat";
		private const string DefaultSysFsCgroup = "/sys/fs/cgroup";
		private const string ProcSelfCgroup = "/proc/self/cgroup";
		private const string ProcSelfMountinfo = "/proc/self/mountinfo";
		private const string ProcMeminfo = "/proc/meminfo";

		internal const string SystemProcessCgroupMemoryMemLimitBytes = "system.process.cgroup.memory.mem.limit.bytes";
		internal const string SystemProcessCgroupMemoryMemUsageBytes = "system.process.cgroup.memory.mem.usage.bytes";

		internal static readonly Regex Cgroup1MountPoint = new("^\\d+? \\d+? .+? .+? (.*?) .*cgroup.*memory.*");
		internal static readonly Regex Cgroup2MountPoint = new("^\\d+? \\d+? .+? .+? (.*?) .*cgroup2.*cgroup.*");
		internal static readonly Regex MemoryCgroup = new("^\\d+:memory:.*");

#if NET8_0_OR_GREATER
		private static readonly FileStreamOptions Options = new() { BufferSize = 0, Mode = FileMode.Open, Access = FileAccess.Read };
#endif

		private readonly CgroupFiles _cGroupFiles;
		private readonly bool _collectMemLimitBytes;
		private readonly bool _collectMemUsageBytes;
		private readonly bool _collectTotalMemory;

		private readonly string _pathPrefix;
		private readonly bool _ignoreOs;
		private readonly IApmLogger _logger;

		/// <summary>
		/// Initializes a new instance of <see cref="CgroupMetricsProvider" />
		/// </summary>
		/// <param name="logger">the logger</param>
		/// <param name="disabledMetrics">List of disabled metrics</param>
		public CgroupMetricsProvider(IApmLogger logger, IReadOnlyList<WildcardMatcher> disabledMetrics)
			: this(logger, disabledMetrics, null) { }

		/// <summary>
		/// Get a testable <see cref="CgroupMetricsProvider"/> instance.
		/// </summary>
		internal static CgroupMetricsProvider TestableCgroupMetricsProvider(
			IApmLogger logger,
			IReadOnlyList<WildcardMatcher> disabledMetrics,
			string pathPrefix,
			bool ignoreOs = false) =>
			new(logger, disabledMetrics, pathPrefix, ignoreOs);

		private CgroupMetricsProvider(IApmLogger logger, IReadOnlyList<WildcardMatcher> disabledMetrics, string pathPrefix, bool ignoreOs = false)
		{
			_pathPrefix = pathPrefix ?? string.Empty;
			_ignoreOs = ignoreOs;

			_collectMemLimitBytes = IsSystemProcessCgroupMemoryMemLimitBytesEnabled(disabledMetrics);
			_collectMemUsageBytes = IsSystemProcessCgroupMemoryMemUsageBytesEnabled(disabledMetrics);
			_collectTotalMemory = IsTotalMemoryEnabled(disabledMetrics);

			_logger = logger.Scoped(nameof(CgroupMetricsProvider));

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !_ignoreOs)
			{
				_logger.Trace()
					?.Log("{MetricsProviderName} detected a non-Linux OS, therefore"
						+ " Cgroup metrics will not be reported", nameof(CgroupMetricsProvider));

				return;
			}

			_cGroupFiles = FindCGroupFiles();

			IsMetricAlreadyCaptured = true;
		}

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName { get; } = nameof(CgroupMetricsProvider);

		public bool IsMetricAlreadyCaptured { get; }

		private CgroupFiles FindCGroupFiles()
		{
			// PERF: There are some allocations in this method, which we have not optimised
			// as this method is invoked once from the constructor and we only expect a single
			// instance of this type.

			// This code block allocates only during profiling and testing
			var procSelfCGroup = !string.IsNullOrEmpty(_pathPrefix)
				? Path.Combine(_pathPrefix, ProcSelfCgroup.Substring(1))
				: ProcSelfCgroup;

			if (!File.Exists(procSelfCGroup))
			{
				_logger.Debug()?.Log("{File} does not exist. Cgroup metrics will not be reported", procSelfCGroup);
				return null;
			}
			_logger.Trace()?.Log("{File} exists. Cgroup metrics will be reported", procSelfCGroup);

			string cGroupLine = null;

			try
			{
				using var reader = new StreamReader(procSelfCGroup);
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					if (cGroupLine is null && line.StartsWith("0:"))
						cGroupLine = line;

					if (MemoryCgroup.IsMatch(line))
					{
						cGroupLine = line;
						break;
					}
				}

				if (cGroupLine is null)
				{
					_logger.Warning()?.Log("No {File} file line matched the tested patterns. Cgroup metrics will not be reported", procSelfCGroup);
					return null;
				}
			}
			catch (IOException e)
			{
				_logger.Info()?.LogException(e, "Cannot read {File}. Cgroup metrics will not be reported", procSelfCGroup);
				return null;
			}

			CgroupFiles cgroupFiles;

			var mountInfo = !string.IsNullOrEmpty(_pathPrefix)
				? Path.Combine(_pathPrefix, ProcSelfMountinfo.Substring(1))
				: ProcSelfMountinfo;

			if (File.Exists(mountInfo))
			{
				string mountLine = null;
				try
				{
					using var reader = new StreamReader(mountInfo);
					while ((mountLine = reader.ReadLine()) != null)
					{
						// cgroup v2
						var rootCgroupFsPath = ApplyCgroupRegex(Cgroup2MountPoint, mountLine);
						if (rootCgroupFsPath != null)
						{
							cgroupFiles = CreateCgroup2Files(cGroupLine, rootCgroupFsPath);
							if (cgroupFiles != null)
								return cgroupFiles;
						}

						// cgroup v1
						var memoryMountPath = ApplyCgroupRegex(Cgroup1MountPoint, mountLine);
						if (memoryMountPath != null)
						{
							cgroupFiles = CreateCgroup1Files(memoryMountPath);
							if (cgroupFiles != null)
								return cgroupFiles;
						}
					}
				}
				catch (IOException e)
				{
					_logger.Info()?.LogException(e, "Failed to discover memory mount files path based on mountinfo line '{MountLine}'.", mountLine);
				}
			}
			else
			{
				_logger.Info()
					?.Log(
						"{File} file does not exist. Looking for memory files in {DefaultFile}.",
						ProcSelfMountinfo,
						DefaultSysFsCgroup);
			}


			// Failed to auto-discover the cgroup fs path from mountinfo, fall back to /sys/fs/cgroup
			cgroupFiles = CreateCgroup2Files(cGroupLine, DefaultSysFsCgroup);
			if (cgroupFiles != null)
				return cgroupFiles;

			cgroupFiles = CreateCgroup1Files(Path.Combine(DefaultSysFsCgroup, "memory"));

			return cgroupFiles;
		}

		private CgroupFiles CreateCgroup1Files(string memoryMountPath)
		{
			var maxMemoryFile = Path.Combine(memoryMountPath, Cgroup1MaxMemory);

			if (File.Exists(maxMemoryFile))
			{
				maxMemoryFile = GetMaxMemoryFile(maxMemoryFile, Cgroup1Unlimited);
				return new CgroupFiles(
					maxMemoryFile,
					Path.Combine(memoryMountPath, Cgroup1UsedMemory),
					Path.Combine(memoryMountPath, CgroupMemoryStat)
				);
			}

			return null;
		}

		private CgroupFiles CreateCgroup2Files(string cGroupLine, string rootCgroupFsPath)
		{
			var cgroupLineParts = cGroupLine.Split(':');
			var sliceSubDir = cgroupLineParts[cgroupLineParts.Length - 1];
			var maxMemoryFile = Path.Combine(rootCgroupFsPath + sliceSubDir, Cgroup2MaxMemory);

			if (File.Exists(maxMemoryFile))
			{
				maxMemoryFile = GetMaxMemoryFile(maxMemoryFile, Cgroup2Unlimited);
				return new CgroupFiles(
					maxMemoryFile,
					Path.Combine(rootCgroupFsPath + sliceSubDir, Cgroup2UsedMemory),
					Path.Combine(rootCgroupFsPath + sliceSubDir, CgroupMemoryStat)
				);
			}

			return null;
		}

		internal static string ApplyCgroupRegex(Regex regex, string mountLine)
		{
			var match = regex.Match(mountLine);
			return match.Success
				? match.Groups[1].Value
				: null;
		}

		private string GetMaxMemoryFile(string maxMemoryFile, string cgroupUnlimitedConstant)
		{
			try
			{
				using var reader = new StreamReader(maxMemoryFile);
				var memMaxLine = reader.ReadLine();
				if (cgroupUnlimitedConstant.Equals(memMaxLine, StringComparison.OrdinalIgnoreCase))
				{
					// Make sure we don't send the max metric when cgroup is not bound to a memory limit
					maxMemoryFile = null;
				}

				return maxMemoryFile;
			}
			catch (IOException e)
			{
				_logger.Info()?.LogException(e, "Cannot read {File}.", maxMemoryFile);
				return null;
			}
		}

		public IEnumerable<MetricSet> GetSamples()
		{
			if (_cGroupFiles is not null)
				yield return new(TimeUtils.TimestampNow(), GetSamplesCore());
		}

		private IEnumerable<MetricSample> GetSamplesCore()
		{
			if (_collectMemUsageBytes)
			{
				var sample = GetMemoryMemUsageBytes();
				if (sample is not null)
					yield return sample;
			}

			if (_collectMemLimitBytes)
			{
				var sample = GetMemoryMemLimitBytes();
				if (sample is not null)
					yield return sample;
			}
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private MetricSample GetMemoryMemLimitBytes()
		{
			try
			{
				if (_cGroupFiles.MaxMemoryFile is null)
				{
					// When the memory is unlimited, MaxMemoryFile will be null.
					// Per the spec, we fall back to returning max memory instead.
					var (totalMemory, _) = Linux.GlobalMemoryStatus.GetTotalAndAvailableSystemMemory(_logger, _pathPrefix, _ignoreOs);

					if (totalMemory >= 0)
						return new MetricSample(SystemProcessCgroupMemoryMemLimitBytes, totalMemory);
				}

#if NET8_0_OR_GREATER // Optimised code for newer runtimes
				return GetLongValueFromFile(_cGroupFiles.MaxMemoryFile, SystemProcessCgroupMemoryMemLimitBytes);
#else
				using var reader = new StreamReader(_cGroupFiles.MaxMemoryFile);
				var line = reader.ReadLine();
				if (double.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
					return new MetricSample(SystemProcessCgroupMemoryMemLimitBytes, value);
#endif

			}
			catch (IOException e)
			{
				_logger.Info()?.LogException(e, "error collecting {Metric} metric", SystemProcessCgroupMemoryMemLimitBytes);
			}

			return null;
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private MetricSample GetMemoryMemUsageBytes()
		{
			try
			{
#if NET8_0_OR_GREATER // Optimised code for newer runtimes
				return GetLongValueFromFile(_cGroupFiles.UsedMemoryFile, SystemProcessCgroupMemoryMemUsageBytes);
#else
				using var reader = new StreamReader(_cGroupFiles.UsedMemoryFile);
				var line = reader.ReadLine();
				if (double.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
					return new MetricSample(SystemProcessCgroupMemoryMemUsageBytes, value);
#endif
			}
			catch (IOException e)
			{
				_logger.Info()?.LogException(e, "error collecting {metric} metric", SystemProcessCgroupMemoryMemUsageBytes);
			}

			return null;
		}

#if NET8_0_OR_GREATER
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private MetricSample GetLongValueFromFile(string path, string sampleName)
		{
			using var fs = new FileStream(path, Options);
			Span<byte> buffer = stackalloc byte[20]; // this size should always be sufficient to read the max long value as a string.
			var bytes = fs.Read(buffer);
#if DEBUG
			var fileValue = Encoding.UTF8.GetString(buffer);
#endif
			if (bytes > 0 && Utf8Parser.TryParse(buffer, out long value, out _))
				return new MetricSample(sampleName, value);

			return null;
		}
#endif

		private const byte Newline = (byte)'\n';
		private const byte Space = (byte)' ';
		private static ReadOnlySpan<byte> _totalInactiveFile => "total_inactive_file"u8;
		private static ReadOnlySpan<byte> _inactiveFile => "inactive_file"u8;

		public bool IsEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) =>
			IsSystemProcessCgroupMemoryMemLimitBytesEnabled(disabledMetrics) ||
			IsSystemProcessCgroupMemoryMemUsageBytesEnabled(disabledMetrics);

		private static bool IsSystemProcessCgroupMemoryMemLimitBytesEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) => !WildcardMatcher.IsAnyMatch(
			disabledMetrics, SystemProcessCgroupMemoryMemLimitBytes);

		private static bool IsSystemProcessCgroupMemoryMemUsageBytesEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) => !WildcardMatcher.IsAnyMatch(
			disabledMetrics, SystemProcessCgroupMemoryMemUsageBytes);

		private static bool IsTotalMemoryEnabled(IReadOnlyList<WildcardMatcher> disabledMetrics) =>
			!WildcardMatcher.IsAnyMatch(disabledMetrics, FreeAndTotalMemoryProvider.TotalMemory);
	}

	/// <summary>
	/// Holds the collection of relevant cgroup files
	/// </summary>
	internal sealed class CgroupFiles
	{
		public CgroupFiles(string maxMemoryFile, string usedMemoryFile, string statMemoryFile)
		{
			MaxMemoryFile = maxMemoryFile;
			UsedMemoryFile = usedMemoryFile;
			StatMemoryFile = statMemoryFile;
		}

		public string MaxMemoryFile { get; }
		public string StatMemoryFile { get; }
		public string UsedMemoryFile { get; }
	}
}
