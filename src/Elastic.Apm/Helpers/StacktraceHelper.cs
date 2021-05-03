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
using Elastic.Apm.Libraries.Ben.Demystifier;
using Elastic.Apm.Logging;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Helpers
{
	internal static class StacktraceHelper
	{
		private const string DefaultAsyncMethodName = "MoveNext";
		private static readonly ElasticVersion V710 = new ElasticVersion(7, 10, 0, null);

		/// <summary>
		/// Turns a System.Diagnostic.StackFrame[] into a <see cref="CapturedStackFrame" /> list which can be reported to the APM
		/// Server
		/// </summary>
		/// <param name="frames">The stack frames to rewrite into APM stack traces</param>
		/// <param name="logger">The logger to emit exceptions on should one occur</param>
		/// <param name="apmServerInfo">The ServerInfo instance to query the server version</param>
		/// <param name="dbgCapturingFor">Just for logging.</param>
		/// <param name="configurationReader">
		/// Config reader - this controls the collection of stack traces (e.g. limit on frames,
		/// etc)
		/// </param>
		/// <returns>A prepared List that can be passed to the APM server</returns>
		internal static List<CapturedStackFrame> GenerateApmStackTrace(StackFrame[] frames, IApmLogger logger,
			IConfigurationReader configurationReader, IApmServerInfo apmServerInfo, string dbgCapturingFor
		)
		{
			var stackTraceLimit = configurationReader.StackTraceLimit;
			if (stackTraceLimit == 0)
				return null;

			// new StackTrace(skipFrames: n) skips frames from the top of the stack (currently executing method is top)
			// the StackTraceLimit feature takes the top n frames, so unfortunately we currently capture the whole stack trace and just take
			// the top `configurationReader.StackTraceLimit` frames.
			var len = stackTraceLimit == -1 ? frames.Length : stackTraceLimit;
			var retVal = new List<CapturedStackFrame>(len);

			logger.Trace()?.Log("transform stack frames");

			try
			{
				for (var index = 0; index < len; index++)
				{
					var frame = frames[index];
					var className = frame?.GetMethod()
						?.DeclaringType?.FullName; //see: https://github.com/elastic/apm-agent-dotnet/pull/240#discussion_r289619196

					var functionName = GetRealMethodName(frame?.GetMethod());
					if(frame is EnhancedStackFrame enhancedStackFrame && enhancedStackFrame.IsRecursive)
						functionName += $" x {enhancedStackFrame.MethodInfo.RecurseCount}";

					var fileName = frame?.GetFileName();

					logger.Trace()?.Log("{MethodName}, {lineNo}", functionName, frame?.GetFileLineNumber());

					var capturedStackFrame = new CapturedStackFrame
					{
						Function = functionName ?? "N/A",
						Module = frame?.GetMethod()?.ReflectedType?.Assembly.FullName,
						LineNo = frame?.GetFileLineNumber() ?? 0,
						AbsPath = frame?.GetFileName() // optional property
					};

					if (apmServerInfo?.Version < V710)
						// In pre 7.10, Kibana shows stack traces in format: `[FileName] in [MethodName]` and there is no way to show ClassName.
						// For .NET that format is less useful especially because in some cases we only have a `.dll` file as filename.
						// Therefore as a workaround we send the real classname in the file name field and
						// we don't send anything in the ClassName field, since that's not used.
						// If versions 7.09 is out of support, this code can be removed.
						capturedStackFrame.FileName = string.IsNullOrWhiteSpace(className) ? "N/A" : className;
					else
					{
						// FileName is either the .cs file or the assembly location as fallback
						capturedStackFrame.FileName =
							string.IsNullOrWhiteSpace(fileName)
								? string.IsNullOrEmpty(frame?.GetMethod()?.GetType().Assembly.Location) ? "n/a" :
								frame.GetMethod()?.GetType().Assembly.Location
								: fileName;

						capturedStackFrame.ClassName = string.IsNullOrWhiteSpace(className) ? "N/A" : className;
					}

					retVal.Add(capturedStackFrame);
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
		/// <param name="apmServerInfo">The server info instance to query the APM Server version</param>
		/// <returns>A prepared List that can be passed to the APM server</returns>
		internal static List<CapturedStackFrame> GenerateApmStackTrace(Exception exception, IApmLogger logger, string dbgCapturingFor,
			IConfigurationReader configurationReader, IApmServerInfo apmServerInfo
		)
		{
			var stackTraceLimit = configurationReader.StackTraceLimit;

			if (stackTraceLimit == 0)
				return null;

			try
			{
				try
				{
					return GenerateApmStackTrace(
						new EnhancedStackTrace(exception).GetFrames(), logger, configurationReader, apmServerInfo, dbgCapturingFor);
				}
				catch (Exception e)
				{
					logger?.Debug()
						?
						.LogException(e, "Failed generating stack trace with EnhancedStackTrace - using fallback without demystification");
					// Fallback, see https://github.com/elastic/apm-agent-dotnet/issues/957
					// This callstack won't be demystified, but at least it'll be captured.
					return GenerateApmStackTrace(
						new StackTrace(exception, true).GetFrames(), logger, configurationReader, apmServerInfo, dbgCapturingFor);
				}
			}
			catch (Exception e)
			{
				logger?.Warning()
					?.Log("Failed extracting stack trace from exception for {ApmContext}."
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
