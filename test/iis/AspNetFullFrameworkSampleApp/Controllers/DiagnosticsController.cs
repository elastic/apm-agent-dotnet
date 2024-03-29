// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using AspNetFullFrameworkSampleApp.Models;
using NLog.Common;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	/// Note that this application is used by Elastic.Apm.AspNetFullFramework.Tests so changing it might break the tests
	public class DiagnosticsController : ControllerBase
	{
		internal const string DiagnosticsPageRelativePath = "Diagnostics";

		public ActionResult Index()
		{
			var model = new DiagnosticsViewModel();

			var envVarsBuilder = new StringBuilder();
			foreach (var nameValue in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(nv => nv.Key))
				envVarsBuilder.AppendLine($"{nameValue.Key}={nameValue.Value}");
			model.EnvironmentVariables = envVarsBuilder.ToString();

			model.LogFilePath = Environment.GetEnvironmentVariable(LoggingConfig.LogFileEnvVarName);

			if (model.LogFilePath != null)
			{
				try
				{
					model.LogFileContent = System.IO.File.ReadAllText(model.LogFilePath);
				}
				catch (Exception ex)
				{
					model.LogFileReadException = ex;
				}
			}

			var inMemoryLogContentBuilder = new StringBuilder();
			foreach (var line in LoggingConfig.LogMemoryTarget.Logs)
				inMemoryLogContentBuilder.AppendLine(line);
			model.InMemoryLogContent = inMemoryLogContentBuilder.ToString();

			model.LoggingSubsystemInternalLogContent = InternalLogger.LogWriter?.ToString();

			return View(model);
		}
	}
}
