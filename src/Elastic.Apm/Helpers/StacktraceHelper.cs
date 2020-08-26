// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal static class StacktraceHelper
	{
		private const string DefaultAsyncMethodName = "MoveNext";

		/// <summary>
		/// Turns a System.Diagnostic.StackFrame[] into a <see cref="CapturedStackFrame" /> list which can be reported to the APM
		/// Server
		/// </summary>
		/// <param name="frames">The stack frames to rewrite into APM stack traces</param>
		/// <param name="logger">The logger to emit exceptions on should one occur</param>
		/// <param name="dbgCapturingFor">Just for logging.</param>
		/// <param name="configurationReader">
		/// Config reader - this controls the collection of stack traces (e.g. limit on frames,
		/// etc)
		/// </param>
		/// <returns>A prepared List that can be passed to the APM server</returns>
		internal static List<CapturedStackFrame> GenerateApmStackTrace(StackFrame[] frames, IApmLogger logger,
			IConfigurationReader configurationReader, string dbgCapturingFor
		)
		{
			var stackTraceLimit = configurationReader.StackTraceLimit;

			if (stackTraceLimit == 0)
				return null;

			if (stackTraceLimit > 0)
				// new StackTrace(skipFrames: n) skips frames from the top of the stack (currently executing method is top)
				// the StackTraceLimit feature takes the top n frames, so unfortunately we currently capture the whole stack trace and just take
				// the top `configurationReader.StackTraceLimit` frames. - This could be optimized.
				frames = frames.Take(stackTraceLimit).ToArray();

			var retVal = new List<CapturedStackFrame>(frames.Length);

			logger.Trace()?.Log("transform stack frames");

			try
			{
				foreach (var frame in frames)
				{
					var fileName = frame?.GetMethod()
						?.DeclaringType?.FullName; //see: https://github.com/elastic/apm-agent-dotnet/pull/240#discussion_r289619196

					var functionName = GetRealMethodName(frame?.GetMethod());

					logger.Trace()?.Log("{MethodName}, {lineNo}", functionName, frame?.GetFileLineNumber());

					retVal.Add(new CapturedStackFrame
					{
						Function = functionName ?? "N/A",
						FileName = string.IsNullOrWhiteSpace(fileName) ? "N/A" : fileName,
						Module = frame?.GetMethod()?.ReflectedType?.Assembly.FullName,
						LineNo = frame?.GetFileLineNumber() ?? 0,
						AbsPath = frame?.GetFileName() // optional property
					});
				}
			}
			catch (Exception e)
			{
				logger?.Warning()?.LogException(e, "Failed capturing stacktrace for {ApmContext}", dbgCapturingFor);
			}

			return retVal;
		}

		/// <summary>
		///  Turns an <see cref="Exception" /> into a <see cref="CapturedStackFrame" /> list which can be reported to the APM
		/// Server
		/// </summary>
		/// <param name="exception">The exception to rewrite into APM stack traces</param>
		/// <param name="logger">The logger to emit exceptions on should one occur</param>
		/// <param name="dbgCapturingFor">Just for logging.</param>
		/// <param name="configurationReader">
		/// Config reader - this controls the collection of stack traces (e.g. limit on frames,
		/// etc)
		/// </param>
		/// <returns>A prepared List that can be passed to the APM server</returns>
		internal static List<CapturedStackFrame> GenerateApmStackTrace(Exception exception, IApmLogger logger, string dbgCapturingFor,
			IConfigurationReader configurationReader
		)
		{
			var stackTraceLimit = configurationReader.StackTraceLimit;

			if (stackTraceLimit == 0)
				return null;

			try
			{
				return GenerateApmStackTrace(
					new EnhancedStackTrace(exception).GetFrames(), logger, configurationReader, dbgCapturingFor);
			}
			catch (Exception e)
			{
				logger?.Warning()?.Log("Failed extracting stack trace from exception for {ApmContext}."
					+ " Exception for failure to extract: {ExceptionForFailureToExtract}."
					+ " Exception to extract from: {ExceptionToExtractFrom}.",
					dbgCapturingFor, e, exception);
			}

			return null;
		}

		/// <summary>
		///  Finds real method name even for Async methods, full description of the issue is available here
		/// https://stackoverflow.com/a/28633192
		/// </summary>
		/// <param name="inputMethod">Method to discover</param>
		/// <returns>A real method name (even for async methods)</returns>
		private static string GetRealMethodName(MethodBase inputMethod)
		{
			if (inputMethod == null)
				return null;

			if (inputMethod.Name != DefaultAsyncMethodName)
				return inputMethod.Name;

			var declaredType = inputMethod.DeclaringType;

			if (declaredType == null)
				return DefaultAsyncMethodName;

			if (declaredType.GetInterfaces().All(i => i != typeof(IAsyncStateMachine)))
				return DefaultAsyncMethodName;

			var generatedType = inputMethod.DeclaringType;
			var originalType = generatedType?.DeclaringType;

			if (originalType == null)
				return DefaultAsyncMethodName;

			var foundMethod = originalType.GetMethods(BindingFlags.Instance | BindingFlags.Static |
					BindingFlags.Public | BindingFlags.NonPublic |
					BindingFlags.DeclaredOnly)
				.FirstOrDefault(m => m.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType == generatedType);

			return foundMethod?.Name ?? DefaultAsyncMethodName;
		}
	}
}
