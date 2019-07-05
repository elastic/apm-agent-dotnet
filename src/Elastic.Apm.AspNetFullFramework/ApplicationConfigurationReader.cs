using System;
using System.Configuration;
using System.Security;
using System.Text;
using System.Web.Hosting;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework
{
	internal class ApplicationConfigurationReader : EnvironmentConfigurationReader
	{
		internal new const string Origin = "Configuration Provider";

		public ApplicationConfigurationReader(IApmLogger logger = null) : base(logger) { }

		protected override string DiscoverServiceName() => DiscoverFullFrameworkServiceName() ?? base.DiscoverServiceName();

		private string DiscoverFullFrameworkServiceName()
		{
			var retVal = new StringBuilder();
			AppendIfNotNull(FindSiteName());
			AppendIfNotNull(FindAppPoolName());
			return retVal.ToString();

			void AppendIfNotNull(string nameToAppend)
			{
				if (nameToAppend != null)
				{
					retVal.AppendSeparatedIfNotEmpty("_", nameToAppend);
				}
			}
		}

		private string FindAppPoolName()
		{
			try
			{
				return Environment.GetEnvironmentVariable("APP_POOL_ID");
			}
			catch (SecurityException ex)
			{
				Logger.Error()?.Log("Failed to get app pool name: {Exception}", ex);
				return null;
			}
		}

		private string FindSiteName()
		{
			try
			{
				return HostingEnvironment.SiteName;
			}
			catch (SecurityException ex)
			{
				Logger.Error()?.Log("Failed to get site name: {Exception}", ex);
				return null;
			}
		}

		protected override ConfigurationKeyValue Read(string key)
		{
			var value = ConfigurationManager.AppSettings[key];
			return value != null ? new ConfigurationKeyValue(key, value, Origin) : base.Read(key);
		}
	}
}
