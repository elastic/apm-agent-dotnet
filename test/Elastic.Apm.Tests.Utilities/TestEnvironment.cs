// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Runtime.InteropServices;

namespace Elastic.Apm.Tests.Utilities
{
	/// <summary>
	/// Provides static properties used to identify test environment conditions that may be used to
	/// conditionally skip tests.
	/// </summary>
	public static class TestEnvironment
	{
		/// <summary>
		/// LEGACY: this IsCi check is no longer valid on GitHub Actions.
		/// </summary>
		public static bool IsCi { get; }

		/// <summary>
		/// Will be <see langword="true"/> when the tests are running under GitHub Actions.
		/// </summary>
		public static bool IsGitHubActions { get; }

		/// <summary>
		/// Will be <see langword="true"/> when the tests are running on a Linux OS.
		/// </summary>
		public static bool IsLinux { get; }

		/// <summary>
		/// Will be <see langword="true"/> when the tests are running on a Windows OS.
		/// </summary>
		public static bool IsWindows { get; }

		static TestEnvironment()
		{
			IsCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
			IsGitHubActions = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTION"));
			IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
		}
	}
}
