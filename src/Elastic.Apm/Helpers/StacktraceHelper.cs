using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Helpers
{
	internal static class StacktraceHelper
	{
		private const string DefaultAsyncMethodName = "MoveNext";

		/// <summary>
		/// Turns a System.Diagnostic.StackFrame[] into a <see cref="CapturedStackFrame" /> list which can be reported to the APM Server
		/// </summary>
		/// <param name="frames">The stack frames to rewrite into APM stack traces</param>
		/// <param name="logger">The logger to emit exceptions on should one occur</param>
		/// <param name="capturingFor">Just for logging.</param>
		/// <returns>A prepared List that can be passed to the APM server</returns>
		internal static List<CapturedStackFrame> GenerateApmStackTrace(StackFrame[] frames, IApmLogger logger, string capturingFor)
		{
			var retVal = new List<CapturedStackFrame>(frames.Length);

			try
			{
				retVal.AddRange(from item in frames
					let fileName = item?.GetFileName()
					where !string.IsNullOrEmpty(fileName)
					select new CapturedStackFrame
					{
						Function = GetRealMethodName(item?.GetMethod()),
						FileName = fileName,
						Module = item?.GetMethod()?.ReflectedType?.Name,
						LineNo = item?.GetFileLineNumber() ?? 0
					});
			}
			catch (Exception e)
			{
				logger?.Warning()?.LogException(e, "Failed capturing stacktrace for {ApmContext}", capturingFor);
			}

			return retVal;
		}

		/// <summary>
		///  Turns an <see cref="Exception"/> into a <see cref="CapturedStackFrame" /> list which can be reported to the APM Server
		/// </summary>
		/// <param name="exception">The exception to rewrite into APM stack traces</param>
		/// <param name="logger">The logger to emit exceptions on should one occur</param>
		/// <param name="capturingFor">Just for logging.</param>
		/// <returns>A prepared List that can be passed to the APM server</returns>
		internal static List<CapturedStackFrame> GenerateApmStackTrace(Exception exception, IApmLogger logger, string capturingFor)
		{
			try
			{
				return GenerateApmStackTrace(new StackTrace(exception, true).GetFrames(), logger, capturingFor);
			}
			catch (Exception e)
			{
				logger?.Warning()?.LogException(e, "Failed extracting exception from stackTrace for {ApmContext}", capturingFor);
			}

			return null;
		}

		/// <summary>
		///  Finds real method name even for Async methods, full description of the issue is available here https://stackoverflow.com/a/28633192
		/// </summary>
		/// <param name="inputMethod">Method to discover</param>
		/// <returns>A real method name (even for async methods)</returns>
		private static string GetRealMethodName(MethodBase inputMethod)
		{
			if (inputMethod == null)
				return "";

			if (inputMethod.Name != DefaultAsyncMethodName)
				return inputMethod.Name;

			var declaredType = inputMethod.DeclaringType;

			if (declaredType == null)
				return DefaultAsyncMethodName;

			if (declaredType.GetInterfaces().All(i => i != typeof(IAsyncStateMachine)))
				return inputMethod.Name;

			var generatedType = inputMethod.DeclaringType;
			var originalType = generatedType?.DeclaringType;

			if (originalType == null)
				return DefaultAsyncMethodName;

			var foundMethod = originalType.GetMethods(BindingFlags.Instance | BindingFlags.Static |
			                                          BindingFlags.Public | BindingFlags.NonPublic |
			                                          BindingFlags.DeclaredOnly)
				.Single(m => m.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType == generatedType);

			return foundMethod.Name;
		}
	}
}
