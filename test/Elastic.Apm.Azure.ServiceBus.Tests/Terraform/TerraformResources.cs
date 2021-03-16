// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using Elastic.Apm.Azure.ServiceBus.Tests.Azure;
using ProcNet;
using ProcNet.Std;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.Azure.ServiceBus.Tests.Terraform
{
	/// <summary>
	/// Interact with Terraform templates to apply and destroy resources
	/// </summary>
	public class TerraformResources
	{
		private readonly string _resourceDirectory;
		private readonly IDictionary<string, string> _environment;
		private static readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(10);
		private IMessageSink _messageSink;

		public TerraformResources(string resourceDirectory, AzureCredentials credentials, IMessageSink messageSink = null)
		{
			if (resourceDirectory is null)
				throw new ArgumentNullException(nameof(resourceDirectory));

			if (!Directory.Exists(resourceDirectory))
				throw new DirectoryNotFoundException($"Directory does not exist {resourceDirectory}");

			_resourceDirectory = resourceDirectory;
			_environment = credentials.ToTerraformEnvironmentVariables();
			_messageSink = messageSink;
		}

		private ObservableProcess CreateProcess(params string[] arguments)
		{
			var startArguments = new StartArguments("terraform", arguments)
			{
				WorkingDirectory = _resourceDirectory,
				Environment = _environment
			};

			return new ObservableProcess(startArguments);
		}

		private void RunProcess(ObservableProcess process, Action<LineOut> onLine = null)
		{
			var capturedLines = new List<string>();
			ExceptionDispatchInfo e = null;

			process.SubscribeLines(line =>
				{
					capturedLines.Add(line.Line);
					onLine?.Invoke(line);
				},
				exception => e = ExceptionDispatchInfo.Capture(exception));

			var completed = process.WaitForCompletion(_defaultTimeout);

			if (!completed)
			{
				process.Dispose();
				throw new TerraformResourceException(
					$"terraform {_resourceDirectory} timed out after {_defaultTimeout}", -1, capturedLines);
			}

			if (e != null)
			{
				throw new TerraformResourceException(
					$"terraform {_resourceDirectory} did not succeed", e.SourceException);
			}

			if (process.ExitCode != 0)
			{
				throw new TerraformResourceException(
					$"terraform {_resourceDirectory} did not succeed", process.ExitCode.Value, capturedLines);
			}
		}

		public void Init()
		{
			using var process = CreateProcess("init", "-no-color");
			RunProcess(process, _messageSink is null ? null: line => _messageSink.OnMessage(new DiagnosticMessage(line.Line)));
		}

		/// <summary>
		/// Applies the terraform infrastructure with the supplied variables
		/// </summary>
		/// <param name="variables"></param>
		public void Apply(IDictionary<string, string> variables = null)
		{
			var args = new List<string>
			{
				"apply",
				"-auto-approve",
				"-no-color",
				"-input=false"
			};

			if (variables != null)
			{
				foreach (var variable in variables)
				{
					args.Add("-var");
					args.Add($"{variable.Key}={variable.Value}");
				}
			}

			using var process = CreateProcess(args.ToArray());
			RunProcess(process, _messageSink is null ? null: line => _messageSink.OnMessage(new DiagnosticMessage(line.Line)));
		}

		/// <summary>
		/// Reads an output value from applied terraform managed infrastructure.
		/// </summary>
		/// <param name="name">The name of the output value to read</param>
		/// <returns></returns>
		public string Output(string name)
		{
			var output = new StringBuilder();
			using var process = CreateProcess($"output", "-raw", "-no-color", name);
			RunProcess(process, line =>
			{
				if (!line.Error)
					output.Append(line.Line);
			});

			return output.ToString();
		}

		/// <summary>
		/// Destroys the terraform managed infrastructure
		/// </summary>
		/// <exception cref="Exception"></exception>
		public void Destroy(IDictionary<string, string> variables = null)
		{
			var args = new List<string>
			{
				"destroy",
				"-auto-approve",
				"-no-color",
				"-input=false"
			};

			if (variables != null)
			{
				foreach (var variable in variables)
				{
					args.Add("-var");
					args.Add($"{variable.Key}={variable.Value}");
				}
			}

			using var process = CreateProcess(args.ToArray());
			RunProcess(process);
		}
	}
}
