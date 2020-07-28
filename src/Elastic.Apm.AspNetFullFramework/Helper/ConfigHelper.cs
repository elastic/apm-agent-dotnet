using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework.Helper
{
	public class ConfigHelper
	{
		public static IConfigurationReader CreateReader(IApmLogger logger)
		{
			if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings[ConfigConsts.KeyNames.ConfigurationReaderType]))
			{
				try
				{
					var type = Type.GetType(ConfigurationManager.AppSettings[ConfigConsts.KeyNames.ConfigurationReaderType]);
					if (Activator.CreateInstance(type, logger) is IConfigurationReader reader)
					{
						return reader;
					}
				}
				catch (Exception ex)
				{
					logger.Error()?.LogException(ex, "GetConfigReader exception");
				}
			}

			return null;
		}
	}
}
