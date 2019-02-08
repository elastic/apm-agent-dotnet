﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Helpers
{
	internal static class StacktraceHelper
	{
		/// <summary>
		/// Turns a System.Diagnostic.StackFrame[] into a <see cref="Stacktrace" /> list which can be reported to the APM Server
		/// </summary>
		/// <param name="capturingFor">Just for logging.</param>
		/// <returns>A prepared List that can be passed to the APM server</returns>
		internal static List<Stacktrace> GenerateApmStackTrace(StackFrame[] frames, AbstractLogger logger, string capturingFor)
		{
			var retVal = new List<Stacktrace>(frames.Length);

			try
			{
				retVal.AddRange(from item in frames
					let fileName = item?.GetMethod()?.DeclaringType?.Assembly?.GetName()?.Name
					where !string.IsNullOrEmpty(fileName)
					select new Stacktrace
					{
						Function = item?.GetMethod()?.Name,
						FileName = fileName,
						Module = item?.GetMethod()?.ReflectedType?.Name
					});
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
