using System;
using System.IO;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal class SystemInfoHelper
	{
		internal Api.System ReadContainerId(IApmLogger logger)
		{
			var system = new Api.System();

			try
			{
				if (File.Exists("/proc/self/cgroup"))
				{
					using (var sr = GetStreamReader() )
					{
						var line = sr.ReadLine();

						while (line != null)
						{
							var fields = line.Split(':');
							if (fields.Length == 3)
							{
								var dirAndId = fields[2].Split('/');

								if (dirAndId.Length == 2)
								{
									var id = dirAndId[1].ToLower().EndsWith(".scope") ? dirAndId[1].Substring(".scope".Length) : dirAndId[1];
									system.Container = new Container { Id = id };
								}
							}
							line = sr.ReadLine();
						}
					}
				}
			}
			catch (Exception e)
			{
				logger.Error()?.LogException(e, "Failed reading container id");
			}

			return system;
		}

		protected virtual StreamReader GetStreamReader()
		{

			return new StreamReader("/proc/self/cgroup");
		}
	}
}
