using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Elastic.Apm.Metrics.Windows
{

	[StructLayout(LayoutKind.Sequential)]
	internal struct MEMORYSTATUSEX
	{
		internal uint dwLength;
		internal uint dwMemoryLoad;
		internal ulong ullTotalPhys;
		internal ulong ullAvailPhys;
		internal ulong ullTotalPageFile;
		internal ulong ullAvailPageFile;
		internal ulong ullTotalVirtual;
		internal ulong ullAvailVirtual;
		internal ulong ullAvailExtendedVirtual;
	}

	internal class GlobalMemoryStatus
	{
		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer); //ref. vs. [In, Out], see: https://www.pinvoke.net/default.aspx/kernel32.globalmemorystatusex

		[return: MarshalAs(UnmanagedType.U8)]
		[DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern ulong GetLastError();

		internal static (ulong total, ulong avail) GetTotalPhysAndAvailPhys()
		{
			var statEX = new MEMORYSTATUSEX
			{
				dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX))
			};

			GlobalMemoryStatusEx(ref statEX);
			return (statEX.ullTotalPhys, statEX.ullAvailPhys);
		}

	}
}
