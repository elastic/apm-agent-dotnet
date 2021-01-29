// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Reflection;
using System.Web;
using Elastic.Apm.Logging;

namespace Elastic.Apm.AspNetFullFramework
{
	internal static class AspNetVersion
	{
		/// <summary>
		/// Gets the ASP.NET engine version
		/// </summary>
		/// <param name="logger">The logger</param>
		/// <returns>the engine version, or N/A if it cannot be found</returns>
		public static string GetEngineVersion(IApmLogger logger)
		{
			var aspNetVersion = "N/A";
			try
			{
				// We would like to report the same ASP.NET version as the one printed at the bottom of the error page
				// (see https://github.com/microsoft/referencesource/blob/master/System.Web/ErrorFormatter.cs#L431)
				// It is stored in VersionInfo.EngineVersion
				// (see https://github.com/microsoft/referencesource/blob/3b1eaf5203992df69de44c783a3eda37d3d4cd10/System.Web/Util/versioninfo.cs#L91)
				// which is unfortunately an internal property of an internal class in System.Web assembly so we use reflection to get it
				const string versionInfoTypeName = "System.Web.Util.VersionInfo";
				var versionInfoType = typeof(HttpRuntime).Assembly.GetType(versionInfoTypeName);
				if (versionInfoType == null)
				{
					logger.Error()
						?.Log("Type {TypeName} was not found in assembly {AssemblyFullName} - {AspNetVersion} will be used as ASP.NET version",
							versionInfoTypeName, typeof(HttpRuntime).Assembly.FullName, aspNetVersion);
					return aspNetVersion;
				}

				const string engineVersionPropertyName = "EngineVersion";
				var engineVersionProperty = versionInfoType.GetProperty(engineVersionPropertyName,
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (engineVersionProperty == null)
				{
					logger.Error()
						?.Log("Property {PropertyName} was not found in type {TypeName} - {AspNetVersion} will be used as ASP.NET version",
							engineVersionPropertyName, versionInfoType.FullName, aspNetVersion);
					return aspNetVersion;
				}

				var engineVersionPropertyValue = (string)engineVersionProperty.GetValue(null);
				if (engineVersionPropertyValue == null)
				{
					logger.Error()
						?.Log("Property {PropertyName} (in type {TypeName}) is of type {TypeName} and not a string as expected" +
							" - {AspNetVersion} will be used as ASP.NET version",
							engineVersionPropertyName, versionInfoType.FullName, engineVersionPropertyName.GetType().FullName, aspNetVersion);
					return aspNetVersion;
				}

				aspNetVersion = engineVersionPropertyValue;
			}
			catch (Exception ex)
			{
				logger.Error()?.LogException(ex, "Failed to obtain ASP.NET version - {AspNetVersion} will be used as ASP.NET version", aspNetVersion);
			}

			logger.Debug()?.Log("Found ASP.NET version: {AspNetVersion}", aspNetVersion);
			return aspNetVersion;
		}
	}
}
