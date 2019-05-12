using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;

namespace Elastic.Apm.Metrics
{
	internal class MetricsCollector : IDisposable
	{
		private const string ProcessCpuTotalPct = "system.process.cpu.total.norm.pct";
		private const string ProcessVirtualMemory = "system.process.memory.size";

		private const string ProcessWorkingSetMemory = "system.process.memory.rss.bytes";
		private const string SystemCpuTotalPct = "system.cpu.total.norm.pct";

		private const string TotalMemory = "system.memory.total";
		private const string FreeMemory = "system.memory.actual.free";

		private readonly Timer _timer = new Timer(1000);

		public MetricsCollector(IApmLogger logger, IPayloadSender payloadSender)
		{
			_logger = logger.Scoped(nameof(MetricsCollector));
			_payloadSender = payloadSender;

			logger.Debug()?.Log("starting MetricsCollector");

			_timer.Elapsed += (sender, args) =>
			{
				CollectAllMetrics();
			};
		}

		internal void StartCollecting() => _timer.Start();

		private bool _first = true;
		private  TimeSpan _lastCurrentProcessCpuTime;

		private DateTime _lastTick;

		private TimeSpan _lastCurrentTotalProcessCpuTime;

		private DateTime _lastTotalTick;

		private readonly IApmLogger _logger;
		private readonly IPayloadSender _payloadSender;
		private PerformanceCounter _processorTimePerfCounter;

		private int i = 0;

		internal void CollectAllMetrics()
		{

			//_timer.Stop();
			i++;
			try
			{
				var samples = new List<Sample>();

				var workingSetAndVirtualMem = GetProcessWorkingSetAndVirtualMemory();
				if (workingSetAndVirtualMem != null)
					samples.AddRange(workingSetAndVirtualMem);

				var totalAndFreeMemory = GetTotalAndFreeMemoryMemory();
				if (totalAndFreeMemory != null)
					samples.AddRange(totalAndFreeMemory);

				var (isSystemTotalCpuAvailable, systemTotalCpuTime) = GetSystemTotalCpuTime();
				if (isSystemTotalCpuAvailable) samples.Add(new Sample(SystemCpuTotalPct, systemTotalCpuTime));

				var (isProcessCpuAvailable, processCpuValue) = GetProcessTotalCpuTime();
				if (isProcessCpuAvailable) samples.Add(new Sample(ProcessCpuTotalPct, processCpuValue));

				var metricSet = new Metrics(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000, samples);
				_payloadSender.QueueMetrics(metricSet);
				_logger.Debug()?.Log("Metrics collected: {data}", samples.Aggregate("" ,(i, j) => i.ToString() + ", " + j.ToString() ));
			}
			catch (Exception e)
			{
				_logger.Error()?.LogExceptionWithCaller(e);
			}

			if (i != 2)
			{
				//_timer.Start();
			}
		}

		internal IEnumerable<Sample> GetProcessWorkingSetAndVirtualMemory()
		{
			var process = Process.GetCurrentProcess();
			var virtualMemory = process.VirtualMemorySize64;
			var workingSet = process.WorkingSet64;

			var retVal = new List<Sample>();

			if(virtualMemory != 0)
				 retVal.Add(new Sample(ProcessVirtualMemory, virtualMemory));

			if(workingSet != 0)
				retVal.Add(new Sample(ProcessWorkingSetMemory, workingSet));

			return retVal;
		}

		private object _lock = new object();

		internal (bool, double) GetProcessTotalCpuTime()
		{
			//Console.WriteLine("Start measure");
			var timespan = DateTime.UtcNow;
			//Console.WriteLine($"Current time: {timespan.Minute}.{timespan.Second} {timespan.Millisecond}");
			var cpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

			Console.WriteLine($"CpuTIme: {cpuUsage.TotalSeconds}");
			//Console.WriteLine("End measure");
			//Console.WriteLine($"Current cpuUsage: {cpuUsage.TotalMilliseconds}");

			if (!_first)
			{
//				var startTime = DateTime.UtcNow;
//				var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
//				await Task.Delay(500);
//
//				var endTime = DateTime.UtcNow;
//				var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
//				var cpuUsedMs1 = (endCpuUsage - startCpuUsage).TotalMilliseconds;
//				var totalMsPassed1 = (endTime - startTime).TotalMilliseconds;
//				var cpuUsageTotal1 = cpuUsedMs1 / (Environment.ProcessorCount * totalMsPassed1);
//

				var cpuUsedMs = (cpuUsage - _lastCurrentProcessCpuTime).TotalMilliseconds;
				var totalMsPassed = (timespan - _lastTick).TotalMilliseconds;
				var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);


				_first = false;
				_lastTick = timespan;
				_lastCurrentProcessCpuTime = cpuUsage;
				return (true, cpuUsageTotal);
			}

			lock (_lock)
			{
				_first = false;
				_lastTick = timespan;
				_lastCurrentProcessCpuTime = cpuUsage;
			}

			return (false, 0);
		}

		private bool _firstTotal = true;

		internal (bool, double) GetSystemTotalCpuTime()
		{
			//Perf data:    CollectTotalCpuTime2X |    504.067 us | 143.7222 us |
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				if (_processorTimePerfCounter == null)
					_processorTimePerfCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

				var val = _processorTimePerfCounter.NextValue();

				return (true, (double)val / 100);
			}

			//The x-plat implementation is ~18x slower, than perf. counters on Windows
			//Therefore this is only a fallback for in case of non-Windows OSs
			var timespan = DateTime.UtcNow;
			TimeSpan cpuUsage;

			foreach (var proc in Process.GetProcesses())
			{
				try
				{
					cpuUsage += proc.TotalProcessorTime;
				}
				catch(Exception)
				{
					//Log warning 1 for inaccuracy
				}
			}

			if (!_firstTotal)
			{
				var cpuUsedMs = (cpuUsage - _lastCurrentTotalProcessCpuTime).TotalMilliseconds;
				var totalMsPassed = (timespan - _lastTotalTick).TotalMilliseconds;
				var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

				_firstTotal = false;
				_lastTotalTick = timespan;
				_lastCurrentTotalProcessCpuTime = cpuUsage;

				return (true, cpuUsageTotal);
			}

			_firstTotal = false;
			_lastTotalTick = timespan;
			_lastCurrentTotalProcessCpuTime = cpuUsage;

			return (false, 0);
		}

		internal IEnumerable<Sample> GetTotalAndFreeMemoryMemory()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{

				var (totalMemory, freeMemory) = Windows.GlobalMemoryStatus.GetTotalPhysAndAvailPhys();

				if (totalMemory == 0 || freeMemory == 0)
					return null;

				return new List<Sample>(2) { new Sample(FreeMemory, freeMemory), new Sample(TotalMemory, totalMemory) };
			}

			if (RuntimeInformation.IsOSPlatform((OSPlatform.Linux)))
			{
				var retVal = new List<Sample>();

				try
				{
					using (var sr = new StreamReader("/proc/meminfo"))
					{
						var line = sr.ReadLine();

						while (line != null || retVal.Count != 2) //TODO: break early if possible
						{
							if (line.Contains("MemFree:"))
							{
								var (suc, res) = GetEntry(line, "MemFree:");
								if (suc)
								{
									retVal.Add(new Sample(FreeMemory, res));
								}
							}
							if (line.Contains("MemTotal:"))
							{
								var (suc, res) = GetEntry(line, "MemTotal:");
								if (suc)
								{
									retVal.Add(new Sample(TotalMemory, res));
								}
							}

							line = sr.ReadLine();
						}
					}
				}
				catch(Exception e)
				{
					Console.WriteLine($"Exception: {e.GetType()} - {e.Message}"); //TODO!!

				}

				return retVal;
			}

			(bool, ulong) GetEntry(string line, string name)
			{
				var nameIndex = line.IndexOf(name, StringComparison.Ordinal);
				if (nameIndex < 0)
					return (false, 0);

				var values = line.Substring(line.IndexOf(name, StringComparison.Ordinal) + name.Length);

				if (!string.IsNullOrWhiteSpace(values))
				{
					var items = values.Trim().Split(' ');


					if (items.Length == 1)
					{
						if (ulong.TryParse(items[0], out ulong res))
							return (true, res);
					}
					if (items.Length == 2 && items[1].ToLower() == "kb")
					{
						if (ulong.TryParse(items[0], out ulong res))
							return (true, res * 1024);
					}


				}
				return (false,0);
			}

			return null;
		}

		public void Dispose()
		{
			_timer?.Stop();
			_timer?.Dispose();
		}
	}
}
