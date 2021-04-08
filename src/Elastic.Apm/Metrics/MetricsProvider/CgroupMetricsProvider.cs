// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	/// <summary>
	/// Provides cgroup metrics, if applicable
	/// </summary>
	internal class CgroupMetricsProvider : IMetricsProvider
	{
		private readonly bool _collectMemLimitBytes;
		private readonly bool _collectMemUsageBytes;
		private readonly bool _collectStatsInactiveFileBytes;
		private readonly IApmLogger _logger;
		private const string ProcSelfCgroup = "/proc/self/cgroup";
		private const string ProcSelfMountinfo = "/proc/self/mountinfo";
		private const string DefaultSysFsCgroup = "/sys/fs/cgroup";
		private const string Cgroup1MaxMemory = "memory.limit_in_bytes";
		private const string Cgroup1UsedMemory = "memory.usage_in_bytes";
		private const string Cgroup2MaxMemory = "memory.max";
		private const string Cgroup2UsedMemory = "memory.current";
		private const string CgroupMemoryStat = "memory.stat";
		private const string Cgroup1Unlimited = "9223372036854771712";
		private const string Cgroup2Unlimited = "max";

		internal const string SystemProcessCgroupMemoryMemLimitBytes = "system.process.cgroup.memory.mem.limit.bytes";
		internal const string SystemProcessCgroupMemoryMemUsageBytes = "system.process.cgroup.memory.mem.usage.bytes";
		internal const string SystemProcessCgroupMemoryStatsInactiveFileBytes = "system.process.cgroup.memory.stats.inactive_file.bytes";

		internal static readonly Regex MemoryCgroup = new Regex("^\\d+:memory:.*");
		internal static readonly Regex Cgroup1MountPoint = new Regex("^\\d+? \\d+? .+? .+? (.*?) .*cgroup.*memory.*");
		internal static readonly Regex Cgroup2MountPoint = new Regex("^\\d+? \\d+? .+? .+? (.*?) .*cgroup2.*cgroup.*");

		private readonly CgroupFiles _cGroupFiles;

		/// <summary>
		/// Initializes a new instance of <see cref="CgroupMetricsProvider"/>
		/// </summary>
		/// <param name="logger">the logger</param>
		/// <param name="collectMemLimitBytes">whether to collect <see cref="SystemProcessCgroupMemoryMemLimitBytes"/> metric</param>
		/// <param name="collectMemUsageBytes">whether to collect <see cref="SystemProcessCgroupMemoryMemUsageBytes"/> metric</param>
		/// <param name="collectStatsInactiveFileBytes">whether to collect <see cref="SystemProcessCgroupMemoryStatsInactiveFileBytes"/> metric</param>
		public CgroupMetricsProvider(IApmLogger logger, bool collectMemLimitBytes = true, bool collectMemUsageBytes = true, bool collectStatsInactiveFileBytes = true)
			: this(ProcSelfCgroup, ProcSelfMountinfo, logger, collectMemLimitBytes, collectMemUsageBytes, collectStatsInactiveFileBytes)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CgroupMetricsProvider"/>
		/// </summary>
		/// <param name="procSelfCGroup">the <see cref="ProcSelfCgroup"/> file</param>
		/// <param name="mountInfo">the <see cref="ProcSelfMountinfo"/> file</param>
		/// <param name="logger">the logger</param>
		/// <param name="collectMemLimitBytes">whether to collect <see cref="SystemProcessCgroupMemoryMemLimitBytes"/> metric</param>
		/// <param name="collectMemUsageBytes">whether to collect <see cref="SystemProcessCgroupMemoryMemUsageBytes"/> metric</param>
		/// <param name="collectStatsInactiveFileBytes">whether to collect <see cref="SystemProcessCgroupMemoryStatsInactiveFileBytes"/> metric</param>
		/// <remarks>
		///	Used for testing
		/// </remarks>
		internal CgroupMetricsProvider(string procSelfCGroup, string mountInfo, IApmLogger logger, bool collectMemLimitBytes = true, bool collectMemUsageBytes = true, bool collectStatsInactiveFileBytes = true)
		{
			_collectMemLimitBytes = collectMemLimitBytes;
			_collectMemUsageBytes = collectMemUsageBytes;
			_collectStatsInactiveFileBytes = collectStatsInactiveFileBytes;
			_logger = logger.Scoped(nameof(CgroupMetricsProvider));
			_cGroupFiles = FindCGroupFiles(procSelfCGroup, mountInfo);

			IsMetricAlreadyCaptured = true;
		}

		private CgroupFiles FindCGroupFiles(string procSelfCGroup, string mountInfo)
		{
			if (!File.Exists(procSelfCGroup))
			{
				_logger.Debug()?.Log("{File} does not exist. Cgroup metrics will not be reported", procSelfCGroup);
				return null;
			}

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
				_logger.Info()?.Log(
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

		internal static string ApplyCgroupRegex(Regex regex, string mountLine) {
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

		public int ConsecutiveNumberOfFailedReads { get; set; }
		public string DbgName { get; } = nameof(CgroupMetricsProvider);

		public IEnumerable<MetricSet> GetSamples()
		{
			if (_cGroupFiles is null)
				return null;

			var samples = new List<MetricSample>(3);

			if (_collectStatsInactiveFileBytes)
				GetStatsInactiveFileBytesMetric(samples);

			if (_collectMemUsageBytes)
				GetMemoryMemUsageBytes(samples);

			if (_collectMemLimitBytes)
				GetMemoryMemLimitBytes(samples);

			return new List<MetricSet> { new MetricSet(TimeUtils.TimestampNow(), samples) };
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private void GetMemoryMemLimitBytes(List<MetricSample> samples)
		{
			if (_cGroupFiles.MaxMemoryFile is null)
				return;

			try
			{
				using var reader = new StreamReader(_cGroupFiles.MaxMemoryFile);
				var line = reader.ReadLine();
				if (double.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
					samples.Add(new MetricSample(SystemProcessCgroupMemoryMemLimitBytes, value));
			}
			catch (IOException e)
			{
				_logger.Info()?.LogException(e, "error collecting {Metric} metric", SystemProcessCgroupMemoryMemLimitBytes);
			}
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private void GetMemoryMemUsageBytes(List<MetricSample> samples)
		{
			try
			{
				using var reader = new StreamReader(_cGroupFiles.UsedMemoryFile);
				var line = reader.ReadLine();
				if (double.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
					samples.Add(new MetricSample(SystemProcessCgroupMemoryMemUsageBytes, value));
			}
			catch (IOException e)
			{
				_logger.Info()?.LogException(e, "error collecting {metric} metric", SystemProcessCgroupMemoryMemUsageBytes);
			}
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private void GetStatsInactiveFileBytesMetric(List<MetricSample> samples)
		{
			try
			{
				using var reader = new StreamReader(_cGroupFiles.StatMemoryFile);
				string statLine;
				string inactiveBytes = null;
				while ((statLine = reader.ReadLine()) != null)
				{
					var statLineSplit = statLine.Split(' ');
					if (statLineSplit.Length > 1)
					{
						if ("total_inactive_file".Equals(statLineSplit[0]))
						{
							inactiveBytes = statLineSplit[1];
							break;
						}

						if ("inactive_file".Equals(statLineSplit[0]))
							inactiveBytes = statLineSplit[1];
					}
				}

				if (inactiveBytes != null && double.TryParse(inactiveBytes, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
					samples.Add(new MetricSample(SystemProcessCgroupMemoryStatsInactiveFileBytes, value));
			}
			catch (IOException e)
			{
				_logger.Info()?.LogException(e, "error collecting {Metric} metric", SystemProcessCgroupMemoryStatsInactiveFileBytes);
			}
		}

		public bool IsMetricAlreadyCaptured { get; }
	}

	/// <summary>
	/// Holds the collection of relevant cgroup files
	/// </summary>
	internal class CgroupFiles
	{
		public string MaxMemoryFile { get; }
		public string UsedMemoryFile { get; }
		public string StatMemoryFile { get; }

		public CgroupFiles(string maxMemoryFile, string usedMemoryFile, string statMemoryFile)
		{
			MaxMemoryFile = maxMemoryFile;
			UsedMemoryFile = usedMemoryFile;
			StatMemoryFile = statMemoryFile;
		}
	}
}
