// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;

namespace Elastic.Apm.Azure.Functions.Tests;

public class AzureFunctionsTestFixture : IDisposable
{
	private readonly AutoResetEvent _waitForTransactionDataEvent = new(false);
	private readonly MockApmServer _apmServer;
	private readonly Process _funcProcess;

	public AzureFunctionsTestFixture()
	{
		_apmServer = new MockApmServer(new InMemoryBlockingLogger(LogLevel.Warning), nameof(AzureFunctionsTests));
		_apmServer.OnReceive += o =>
		{
			if (!_apmServer.ReceivedData.Transactions.IsEmpty)
				_waitForTransactionDataEvent.Set();
		};
		var port = _apmServer.FindAvailablePortToListen();
		LogLines.Add($"Starting APM Server on port: {port}");
		_apmServer.RunInBackground(port);

		var workingDir = Path.Combine(Directory.GetCurrentDirectory(),
			"../../../../../sample/Elastic.AzureFunctionApp.Isolated");
		LogLines.Add($"func working directory: {workingDir}");
		Directory.Exists(workingDir).Should().BeTrue();

		var funcToolIsReady = false;
		_funcProcess = new Process
		{
			StartInfo =
			{
				FileName = "func",
				Arguments = "start",
				WorkingDirectory = workingDir,
				EnvironmentVariables =
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ELASTIC_APM_FLUSH_INTERVAL"] = "0"
				},
				UseShellExecute = false
			}
		};
		_funcProcess.StartInfo.RedirectStandardOutput = true;
		_funcProcess.StartInfo.RedirectStandardError = true;
		_funcProcess.OutputDataReceived += (sender, args) => LogLines.Add("[func] [ERROR] " + args.Data);

		_funcProcess.OutputDataReceived += (sender, args) =>
		{
			if (args.Data != null)
			{
				LogLines.Add("[func] " + args.Data);
				if (args.Data != null && args.Data.Contains("Worker process started and initialized"))
					funcToolIsReady = true;
			}
		};

		LogLines.Add($"{DateTime.Now}: Starting func tool");
		_funcProcess.Start();
		_funcProcess.BeginOutputReadLine();
		for (var i = 0; i < 60; i++)
		{
			Thread.Sleep(1000);
			if (funcToolIsReady)
			{
				LogLines.Add($"{DateTime.Now}: func tool ready!");
				break;
			}
		}
	}

	public void Dispose()
	{
		_funcProcess.Kill();
		_apmServer.StopAsync().GetAwaiter().GetResult();
	}

	internal List<string> LogLines { get; } = new();

	internal TransactionDto WaitForTransaction(string name)
	{
		_waitForTransactionDataEvent.WaitOne(TimeSpan.FromSeconds(60));
		_apmServer.ReceivedData.Transactions.Should().HaveCountGreaterOrEqualTo(1);
		return _apmServer.ReceivedData.Transactions.First(t => t.Name == name);
	}

	internal void ClearTransaction() => _apmServer.ClearState();
}
