// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Runtime.InteropServices;
using Elastic.Apm.Logging;

#if NET6_0_OR_GREATER
using System.Buffers;
using System.Buffers.Text;
#endif

namespace Elastic.Apm.Metrics.Linux
{
	internal static class GlobalMemoryStatus
	{
		public const string ProcMemInfo = "/proc/meminfo";

#if NET6_0_OR_GREATER
		private static readonly FileStreamOptions Options = new() { BufferSize = 0, Mode = FileMode.Open, Access = FileAccess.Read };
		private static readonly byte Space = (byte)' ';

		private static ReadOnlySpan<byte> MemAvailable => "MemAvailable:"u8;
		private static ReadOnlySpan<byte> MemTotal => "MemTotal:"u8;
		private static ReadOnlySpan<byte> KB => " kB"u8;
#endif

		public static (long totalMemory, long availableMemory) GetTotalAndAvailableSystemMemory(IApmLogger logger)
			=> GetTotalAndAvailableSystemMemory(logger, null, false);

		internal static (long totalMemory, long availableMemory) GetTotalAndAvailableSystemMemory(
			IApmLogger logger, string pathPrefix, bool ignoreOs)
		{
			(long, long) failure = (-1, -1);
			long totalMemory = -1, availableMemory = -1;

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !ignoreOs)
			{
				logger.Trace()
					?.Log("{ClassName} detected a non-Linux OS, therefore"
						+ " proc/meminfo will not be reported", nameof(GlobalMemoryStatus));

				return failure;
			}

			var memInfoPath = !string.IsNullOrEmpty(pathPrefix)
				? Path.Combine(pathPrefix, ProcMemInfo.Substring(1))
				: ProcMemInfo;

			if (!File.Exists(memInfoPath))
			{
				logger.Error()?.Log("Unable to load memory information from {ProcMemInfo}", ProcMemInfo);
				return failure;
			}
			try
			{
#if NET6_0_OR_GREATER
				using var fs = new FileStream(memInfoPath, Options);
				var buffer = ArrayPool<byte>.Shared.Rent(8192); // Should easily be large enough for max meminfo file.

				try
				{
					var read = fs.Read(buffer);

					if (read == 0)
						return failure;

					var span = buffer.AsSpan().Slice(0, read);

					var memAvailable = span.IndexOf(MemAvailable);
					if (memAvailable >= 0)
					{
						var slice = span.Slice(memAvailable + MemAvailable.Length + 1);
						var position = 0;
						while (true)
						{
							if (slice[position] != Space)
								break;

							position++;
						}

						if (position > 0 && Utf8Parser.TryParse(slice.Slice(position), out long value, out var consumed))
						{
							availableMemory = slice.Slice(position + consumed, 3).SequenceEqual(KB)
								? value *= 1024
								: value;
						}
					}

					var memTotal = span.IndexOf(MemTotal);
					if (memTotal >= 0)
					{
						var slice = span.Slice(memTotal + MemTotal.Length + 1);
						var position = 0;
						while (true)
						{
							if (slice[position] != Space)
								break;

							position++;
						}

						if (position > 0 && Utf8Parser.TryParse(slice.Slice(position), out long value, out var consumed))
						{
							totalMemory = slice.Slice(position + consumed, 3).SequenceEqual(KB)
								? value *= 1024
								: value;
						}
					}

					return (totalMemory, availableMemory);
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(buffer);
				}
#else
				using var sr = new StreamReader(memInfoPath);

				var hasMemFree = false;
				var hasMemTotal = false;
				var samples = 0;

				var line = sr.ReadLine();

				while (samples != 2)
				{
					if (line != null && line.Contains("MemAvailable:"))
					{
						availableMemory = GetEntry(line, "MemAvailable:");
						samples++;
						hasMemFree = true;
					}
					if (line != null && line.Contains("MemTotal:"))
					{
						totalMemory = GetEntry(line, "MemTotal:");
						samples++;
						hasMemTotal = true;
					}

					if (hasMemFree && hasMemTotal)
						break;

					line = sr.ReadLine();

					if (line is null)
						break;
				}

				return (totalMemory, availableMemory);

				static long GetEntry(string line, string name)
				{
					var nameIndex = line.IndexOf(name, StringComparison.Ordinal);
					if (nameIndex < 0)
						return -1;

					var values = line.Substring(line.IndexOf(name, StringComparison.Ordinal) + name.Length);

					if (string.IsNullOrWhiteSpace(values))
						return -1;

					var items = values.Trim().Split(' ');

					return items.Length switch
					{
						1 when long.TryParse(items[0], out var res) => res,
						2 when items[1].ToLowerInvariant() == "kb" && long.TryParse(items[0], out var res) => res * 1024,
						_ => -1,
					};
				}
#endif
			}
			catch (IOException e)
			{
				logger.Info()?.LogException(e, "Error collecting memory data from {path}.", memInfoPath);
			}

			return failure;
		}
	}
}
