// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Text;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal static class CgroupFileHelper
	{
		internal sealed class CgroupPaths : IDisposable
		{
			public CgroupPaths(CgroupVersion cgroupVersion) => CgroupVersion = cgroupVersion;

			public CgroupVersion CgroupVersion { get; }
			public string RootPath { get; set; }
			public string ProcPath { get; set; }
			public string ProcSelfPath { get; set; }
			public string CgroupPath { get; set; }
			public string CgroupV1MemoryControllerPath { get; set; }
			public string CgroupV2SlicePath { get; set; }

			public void Dispose() => Directory.Delete(RootPath, true);
		}

		internal enum CgroupVersion
		{
			CgroupV1, CgroupV2
		}

		public const long DefaultMemoryLimitBytes = 7_964_778_496;
		public const long DefaultMemoryUsageBytes = 585_965_568;
		public const long DefaultMemInfoAvailableBytes = 15_758_585_856;
		public const long DefaultMemInfoTotalBytes = 16_599_289_856;

		public static CgroupPaths CreateDefaultCgroupFiles(CgroupVersion cgroupVersion)
		{
			var rootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			var procPath = Path.Combine(rootPath, "proc");
			var procSelfPath = Path.Combine(procPath, "self");
			var cgroupPath = Path.Combine(rootPath, "sys", "fs", "cgroup");
			var cgroupV1MemoryControllerPath = Path.Combine(cgroupPath, "memory");
			var cgroupV2SlicePath = Path.Combine(cgroupPath, "unified", "sample.slice");

			Directory.CreateDirectory(procSelfPath);
			Directory.CreateDirectory(cgroupV1MemoryControllerPath);
			Directory.CreateDirectory(cgroupV2SlicePath);

			var sb = new StringBuilder();

			// Create /proc/self/cgroup file
			using (var cgroup = new StreamWriter(File.Create(Path.Combine(procSelfPath, "cgroup"))))
			{
				sb.Clear();

				if (cgroupVersion == CgroupVersion.CgroupV1)
				{
					// Sample values retrieved from Ubuntu 22.04 (WSL2) running with cgroup v1 controllers
					sb.Append("41:misc:/").Append("\n");
					sb.Append("40:rdma:/").Append("\n");
					sb.Append("39:hugetlb:/").Append("\n");
					sb.Append("38:net_prio:/").Append("\n");
					sb.Append("37:perf_event:/").Append("\n");
					sb.Append("36:net_cls:/").Append("\n");
					sb.Append("35:freezer:/").Append("\n");
					sb.Append("34:blkio:/").Append("\n");
					sb.Append("33:cpuacct:/").Append("\n");
					sb.Append("32:cpu:/").Append("\n");
					sb.Append("31:cpuset:/").Append("\n");
					sb.Append("15:name=systemd:/").Append("\n");
					sb.Append("12:pids:/").Append("\n");
					sb.Append("6:devices:/").Append("\n");
					sb.Append("5:memory:/").Append("\n");
					sb.Append("0::/").Append("\n");
				}

				if (cgroupVersion == CgroupVersion.CgroupV2)
				{
					sb.Append("1:name=systemd:/").Append("\n");
					sb.Append("0::/sample.slice").Append("\n");
				}

				cgroup.Write(sb.ToString());
				cgroup.Flush();
			}

			using (var memInfo = new StreamWriter(File.Create(Path.Combine(procPath, "meminfo"))))
			{
				sb.Clear();
				sb.Append($"MemTotal:        {DefaultMemInfoTotalBytes / 1024} kB").Append("\n");
				sb.Append("MemFree:         4806144 kB").Append("\n");
				sb.Append("Buffers:          211756 kB").Append("\n");
				sb.Append("Cached:          1071092 kB").Append("\n");
				sb.Append("SwapTotal:       4194296 kB").Append("\n");
				sb.Append("SwapFree:        4194296 kB").Append("\n");
				sb.Append($"MemAvailable:    {DefaultMemInfoAvailableBytes / 1024} kB").Append("\n");

				memInfo.Write(sb.ToString());
				memInfo.Flush();
			}

			using (var memoryLimitInBytes = File.CreateText(Path.Combine(cgroupV1MemoryControllerPath, "memory.limit_in_bytes")))
			{
				memoryLimitInBytes.WriteAsync($"{DefaultMemoryLimitBytes}\n");
				memoryLimitInBytes.Flush();
			}

			using (var cgroup = new StreamWriter(File.Create(Path.Combine(procSelfPath, "mountinfo"))))
			{
				sb.Clear();

				if (cgroupVersion == CgroupVersion.CgroupV1)
				{
					// Based on WSL2 running with v1 controllers
					sb.Append("95 112 0:26 / /mnt/wsl rw,relatime shared:1 - tmpfs none rw").Append("\n");
					sb.Append("96 112 0:28 / /usr/lib/wsl/drivers ro,nosuid,nodev,noatime - 9p none ro,dirsync,aname=drivers;fmask=222;dmask=222,mmap,access=client,msize=65536,trans=fd,rfd=7,wfd=7").Append("\n");
					sb.Append("110 112 0:32 / /usr/lib/wsl/lib rw,relatime - overlay none rw,lowerdir=/gpu_lib_packaged:/gpu_lib_inbox,upperdir=/gpu_lib/rw/upper,workdir=/gpu_lib/rw/work").Append("\n");
					sb.Append("112 87 8:32 / / rw,relatime - ext4 /dev/sdc rw,discard,errors=remount-ro,data=ordered").Append("\n");
					sb.Append("113 112 0:43 / /mnt/wslg rw,relatime shared:2 - tmpfs none rw").Append("\n");
					sb.Append("114 113 8:32 / /mnt/wslg/distro ro,relatime shared:3 - ext4 /dev/sdc rw,discard,errors=remount-ro,data=ordered").Append("\n");
					sb.Append("132 112 0:2 /init /init ro - rootfs rootfs rw,size=8101876k,nr_inodes=2025469").Append("\n");
					sb.Append("156 112 0:5 / /dev rw,nosuid,relatime - devtmpfs none rw,size=8101904k,nr_inodes=2025476,mode=755").Append("\n");
					sb.Append("162 112 0:20 / /sys rw,nosuid,nodev,noexec,noatime - sysfs sysfs rw").Append("\n");
					sb.Append("165 112 0:79 / /proc rw,nosuid,nodev,noexec,noatime - proc proc rw").Append("\n");
					sb.Append("169 156 0:80 / /dev/pts rw,nosuid,noexec,noatime - devpts devpts rw,gid=5,mode=620,ptmxmode=000").Append("\n");
					sb.Append("172 112 0:81 / /run rw,nosuid,nodev - tmpfs none rw,mode=755").Append("\n");
					sb.Append("174 172 0:82 / /run/lock rw,nosuid,nodev,noexec,noatime - tmpfs none rw").Append("\n");
					sb.Append("176 172 0:83 / /run/shm rw,nosuid,nodev,noatime - tmpfs none rw").Append("\n");
					sb.Append("177 156 0:83 / /dev/shm rw,nosuid,nodev,noatime - tmpfs none rw").Append("\n");
					sb.Append("178 172 0:84 / /run/user rw,nosuid,nodev,noexec,noatime - tmpfs none rw,mode=755").Append("\n");
					sb.Append("179 165 0:27 / /proc/sys/fs/binfmt_misc rw,relatime - binfmt_misc binfmt_misc rw").Append("\n");
					sb.Append("180 162 0:85 / {cgroupPath} ro,nosuid,nodev,noexec - tmpfs tmpfs ro,size=4096k,nr_inodes=1024,mode=755").Append("\n");
					sb.Append($"182 180 0:56 / {cgroupPath}/cpuset rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,cpuset").Append("\n");
					sb.Append($"183 180 0:57 / {cgroupPath}/cpu rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,cpu").Append("\n");
					sb.Append($"184 180 0:58 / {cgroupPath}/cpuacct rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,cpuacct").Append("\n");
					sb.Append($"185 180 0:59 / {cgroupPath}/blkio rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,blkio").Append("\n");
					sb.Append($"186 180 0:60 / {cgroupPath}/memory rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,memory").Append("\n");
					sb.Append($"187 180 0:61 / {cgroupPath}/devices rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,devices").Append("\n");
					sb.Append($"188 180 0:62 / {cgroupPath}/freezer rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,freezer").Append("\n");
					sb.Append($"189 180 0:63 / {cgroupPath}/net_cls rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,net_cls").Append("\n");
					sb.Append($"190 180 0:64 / {cgroupPath}/perf_event rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,perf_event").Append("\n");
					sb.Append($"191 180 0:65 / {cgroupPath}/net_prio rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,net_prio").Append("\n");
					sb.Append($"192 180 0:66 / {cgroupPath}/hugetlb rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,hugetlb").Append("\n");
					sb.Append($"193 180 0:67 / {cgroupPath}/pids rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,pids").Append("\n");
					sb.Append($"194 180 0:68 / {cgroupPath}/rdma rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,rdma").Append("\n");
					sb.Append($"195 180 0:69 / {cgroupPath}/misc rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,misc").Append("\n");
					sb.Append("64 113 0:45 /etc/versions.txt /mnt/wslg/versions.txt rw,relatime shared:4 - overlay none rw,lowerdir=/systemvhd,upperdir=/system/rw/upper,workdir=/system/rw/work").Append("\n");
					sb.Append("52 112 0:43 /.X11-unix /tmp/.X11-unix ro,relatime shared:2 - tmpfs none rw").Append("\n");
					sb.Append("56 112 0:38 / /mnt/c rw,noatime - 9p drvfs rw,dirsync,aname=drvfs;path=C:\\;uid=1000;gid=1000;symlinkroot=/mnt/,mmap,access=client,msize=262144,trans=virtio").Append("\n");
					sb.Append($"68 180 0:39 / {cgroupPath}/systemd rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,xattr,name=systemd").Append("\n");
					sb.Append("69 156 0:40 / /dev/hugepages rw,relatime - hugetlbfs hugetlbfs rw,pagesize=2M").Append("\n");
					sb.Append("70 156 0:42 / /dev/mqueue rw,nosuid,nodev,noexec,relatime - mqueue mqueue rw").Append("\n");
					sb.Append("71 162 0:6 / /sys/kernel/debug rw,nosuid,nodev,noexec,relatime - debugfs debugfs rw").Append("\n");
					sb.Append("72 162 0:11 / /sys/kernel/tracing rw,nosuid,nodev,noexec,relatime - tracefs tracefs rw").Append("\n");
					sb.Append("73 162 0:41 / /sys/fs/fuse/connections rw,nosuid,nodev,noexec,relatime - fusectl fusectl rw").Append("\n");
					sb.Append("256 112 8:32 /snap /snap rw,relatime shared:6 - ext4 /dev/sdc rw,discard,errors=remount-ro,data=ordered").Append("\n");
					sb.Append("257 256 0:50 / /snap/bare/5 ro,nodev,relatime shared:44 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
					sb.Append("258 256 0:51 / /snap/gtk-common-themes/1535 ro,nodev,relatime shared:54 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
					sb.Append("259 256 0:52 / /snap/core22/607 ro,nodev,relatime shared:55 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
					sb.Append("260 256 0:53 / /snap/snapd/18933 ro,nodev,relatime shared:56 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
					sb.Append("261 256 0:54 / /snap/ubuntu-desktop-installer/967 ro,nodev,relatime shared:57 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
				}

				if (cgroupVersion == CgroupVersion.CgroupV2)
				{
					sb.Append("71 77 0:26 / /mnt/wsl rw,relatime shared:1 - tmpfs none rw").Append("\n");
					sb.Append("72 77 0:28 / /usr/lib/wsl/drivers ro,nosuid,nodev,noatime - 9p none ro,dirsync,aname=drivers;fmask=222;dmask=222,mmap,access=client,msize=65536,trans=fd,rfd=7,wfd=7").Append("\n");
					sb.Append("76 77 0:32 / /usr/lib/wsl/lib rw,relatime - overlay none rw,lowerdir=/gpu_lib_packaged:/gpu_lib_inbox,upperdir=/gpu_lib/rw/upper,workdir=/gpu_lib/rw/work").Append("\n");
					sb.Append("77 63 8:32 / / rw,relatime - ext4 /dev/sdc rw,discard,errors=remount-ro,data=ordered").Append("\n");
					sb.Append("78 77 0:37 / /mnt/wslg rw,relatime shared:2 - tmpfs none rw").Append("\n");
					sb.Append("79 78 8:32 / /mnt/wslg/distro ro,relatime shared:3 - ext4 /dev/sdc rw,discard,errors=remount-ro,data=ordered").Append("\n");
					sb.Append("97 77 0:2 /init /init ro - rootfs rootfs rw,size=8101876k,nr_inodes=2025469").Append("\n");
					sb.Append("98 77 0:5 / /dev rw,nosuid,relatime - devtmpfs none rw,size=8101904k,nr_inodes=2025476,mode=755").Append("\n");
					sb.Append("99 77 0:20 / /sys rw,nosuid,nodev,noexec,noatime - sysfs sysfs rw").Append("\n");
					sb.Append("100 77 0:49 / /proc rw,nosuid,nodev,noexec,noatime - proc proc rw").Append("\n");
					sb.Append("101 98 0:50 / /dev/pts rw,nosuid,noexec,noatime - devpts devpts rw,gid=5,mode=620,ptmxmode=000").Append("\n");
					sb.Append("102 77 0:51 / /run rw,nosuid,nodev - tmpfs none rw,mode=755").Append("\n");
					sb.Append("104 102 0:53 / /run/shm rw,nosuid,nodev,noatime - tmpfs none rw").Append("\n");
					sb.Append("105 98 0:53 / /dev/shm rw,nosuid,nodev,noatime - tmpfs none rw").Append("\n");
					sb.Append("106 102 0:54 / /run/user rw,nosuid,nodev,noexec,noatime - tmpfs none rw,mode=755").Append("\n");
					sb.Append("107 100 0:27 / /proc/sys/fs/binfmt_misc rw,relatime - binfmt_misc binfmt_misc rw").Append("\n");
					sb.Append("108 99 0:55 / /sys/fs/cgroup ro,nosuid,nodev,noexec - tmpfs tmpfs ro,size=4096k,nr_inodes=1024,mode=755").Append("\n");
					sb.Append($"109 108 0:21 / {cgroupPath}/unified rw,nosuid,nodev,noexec,relatime - cgroup2 cgroup2 rw,nsdelegate").Append("\n");
					sb.Append("113 78 0:39 /etc/versions.txt /mnt/wslg/versions.txt rw,relatime shared:4 - overlay none rw,lowerdir=/systemvhd,upperdir=/system/rw/upper,workdir=/system/rw/work").Append("\n");
					sb.Append("115 78 0:39 /usr/share/doc /mnt/wslg/doc rw,relatime shared:5 - overlay none rw,lowerdir=/systemvhd,upperdir=/system/rw/upper,workdir=/system/rw/work").Append("\n");
					sb.Append("117 77 0:37 /.X11-unix /tmp/.X11-unix ro,relatime shared:2 - tmpfs none rw").Append("\n");
					sb.Append("118 77 0:58 / /mnt/c rw,noatime - 9p drvfs rw,dirsync,aname=drvfs;path=C:\\;uid=1000;gid=1000;symlinkroot=/mnt/,mmap,access=client,msize=262144,trans=virtio").Append("\n");
					sb.Append("119 108 0:59 / /sys/fs/cgroup/systemd rw,nosuid,nodev,noexec,relatime - cgroup cgroup rw,xattr,name=systemd").Append("\n");
					sb.Append("120 98 0:60 / /dev/hugepages rw,relatime - hugetlbfs hugetlbfs rw,pagesize=2M").Append("\n");
					sb.Append("121 98 0:36 / /dev/mqueue rw,nosuid,nodev,noexec,relatime - mqueue mqueue rw").Append("\n");
					sb.Append("122 99 0:6 / /sys/kernel/debug rw,nosuid,nodev,noexec,relatime - debugfs debugfs rw").Append("\n");
					sb.Append("123 99 0:11 / /sys/kernel/tracing rw,nosuid,nodev,noexec,relatime - tracefs tracefs rw").Append("\n");
					sb.Append("124 99 0:61 / /sys/fs/fuse/connections rw,nosuid,nodev,noexec,relatime - fusectl fusectl rw").Append("\n");
					sb.Append("125 77 8:32 /snap /snap rw,relatime shared:6 - ext4 /dev/sdc rw,discard,errors=remount-ro,data=ordered").Append("\n");
					sb.Append("203 125 0:62 / /snap/bare/5 ro,nodev,relatime shared:30 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
					sb.Append("206 125 0:63 / /snap/core22/607 ro,nodev,relatime shared:42 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
					sb.Append("209 125 0:64 / /snap/gtk-common-themes/1535 ro,nodev,relatime shared:44 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
					sb.Append("212 125 0:65 / /snap/snapd/18933 ro,nodev,relatime shared:46 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
					sb.Append("215 125 0:66 / /snap/ubuntu-desktop-installer/967 ro,nodev,relatime shared:48 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
					sb.Append("516 106 0:37 /runtime-dir /run/user/1000 rw,relatime shared:2 - tmpfs none rw").Append("\n");
					sb.Append("517 125 0:71 / /snap/core22/864 ro,nodev,relatime shared:170 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
					sb.Append("526 125 0:72 / /snap/ubuntu-desktop-installer/1271 ro,nodev,relatime shared:206 - fuse.snapfuse snapfuse ro,user_id=0,group_id=0,allow_other").Append("\n");
				}

				cgroup.Write(sb.ToString());
				cgroup.Flush();
			}

			using (var maxMemory = File.CreateText(Path.Combine(cgroupV2SlicePath, "memory.max")))
			{
				maxMemory.WriteAsync($"{DefaultMemoryLimitBytes}\n");
				maxMemory.Flush();
			}

			using (var maxMemory = File.CreateText(Path.Combine(cgroupV2SlicePath, "memory.current")))
			{
				maxMemory.WriteAsync($"{DefaultMemoryUsageBytes}\n");
				maxMemory.Flush();
			}

			using (var usageInBytes = File.CreateText(Path.Combine(cgroupV1MemoryControllerPath, "memory.usage_in_bytes")))
			{
				usageInBytes.WriteAsync($"{DefaultMemoryUsageBytes}\n");
				usageInBytes.Flush();
			}

			var memoryStatePath = cgroupVersion == CgroupVersion.CgroupV1
				? Path.Combine(cgroupV1MemoryControllerPath, "memory.stat")
				: Path.Combine(cgroupV2SlicePath, "memory.stat");

			using (var memoryStat = File.CreateText(memoryStatePath))
			{
				sb.Clear();
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
				sb.Append("inactive_file 10407936").Append("\n");
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
				sb.Append("total_inactive_file 10407936").Append("\n");
				sb.Append("total_active_file 0").Append("\n");
				sb.Append("total_unevictable 0").Append("\n");
				sb.Append("recent_rotated_anon 231947").Append("\n");
				sb.Append("recent_rotated_file 2").Append("\n");
				sb.Append("recent_scanned_anon 231947").Append("\n");
				sb.Append("recent_scanned_file 2622").Append("\n");
				memoryStat.Write(sb.ToString());
				memoryStat.Flush();
			}

			return new(cgroupVersion)
			{
				RootPath = rootPath,
				ProcPath = procPath,
				ProcSelfPath = procSelfPath,
				CgroupPath = cgroupPath,
				CgroupV1MemoryControllerPath = cgroupV1MemoryControllerPath,
				CgroupV2SlicePath = cgroupV2SlicePath,
			};
		}
	}
}
