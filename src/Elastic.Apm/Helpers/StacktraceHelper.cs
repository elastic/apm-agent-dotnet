using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Helpers
{
	public static class StacktraceHelper
	{
		/// <summary>
		/// Turns a System.Diagnostic.StackFrame[] into a <see cref="Stacktrace" /> list which can be reported to the APM Server
		/// </summary>
		/// <param name="capturingFor">Just for logging.</param>
		/// <returns>A prepared List that can be passed to the APM server</returns>
		public static List<Stacktrace> GenerateApmStackTrace(StackFrame[] frames, AbstractLogger logger, string capturingFor)
		{
			var retVal = new List<Stacktrace>(frames.Length);

			try
			{
				foreach (var item in frames)
				{
					var fileName = item?.GetMethod()?.DeclaringType?.Assembly?.GetName()?.Name;
					if (string.IsNullOrEmpty(fileName)) continue; //since filename is required by the server, if we don't have it we skip the frame

					retVal.Add(new Stacktrace
					{
						Function = item?.GetMethod()?.Name,
						Filename = fileName,
						Module = item?.GetMethod()?.ReflectedType?.Name
					});
				}
			}
			catch (Exception e)
			{
				logger.LogWarning($"Failed capturing stacktrace for {capturingFor}");
				logger.LogDebug($"{e.GetType().Name}: {e.Message}");
			}

			return retVal;
		}
	}
}
