// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.Functions.Tests;

public enum FunctionType { Isolated, InProcess }

public class IsolatedContext : AzureFunctionTestContextBase
{
	protected override Uri BaseUri { get; } = new("http://localhost:7071");
	public override string WebsiteName { get; } = "testfaas";
	public override string RuntimeName { get; } = "dotnet-isolated";

	public IsolatedContext() : base(FunctionType.Isolated) { }
}

public class InProcessContext : AzureFunctionTestContextBase
{
	protected override Uri BaseUri { get; } = new("http://localhost:17073");
	public override string WebsiteName { get; } = "testfaas";
	public override string RuntimeName { get; } = "dotnet";

	public InProcessContext() : base(FunctionType.InProcess) { }
}

public abstract class AzureFunctionTestContextBase : IDisposable
{
	private readonly AutoResetEvent _waitForTransactionDataEvent = new(false);
	private readonly MockApmServer _apmServer;
	private readonly Process _funcProcess;
	protected abstract Uri BaseUri { get; }
	public abstract string WebsiteName { get; }
	public abstract string RuntimeName { get; }

	public bool IsFirst { get; internal set; }

	internal AzureFunctionTestContextBase(FunctionType functionType)
	{
		IsFirst = true;
		_apmServer = new MockApmServer(new InMemoryBlockingLogger(LogLevel.Warning), nameof(AzureFunctionsIsolatedTests));
		_apmServer.OnReceive += o =>
		{
			if (!_apmServer.ReceivedData.Transactions.IsEmpty)
				_waitForTransactionDataEvent.Set();
		};
		var port = _apmServer.FindAvailablePortToListen();
		LogLines.Add($"Starting APM Server on port: {port}");
		_apmServer.RunInBackground(port);

		var solutionRoot = SolutionPaths.Root;
		var name = functionType switch
		{
			FunctionType.Isolated => "Isolated",
			FunctionType.InProcess => "InProcess",
			_ => throw new Exception($"Unsupported Azure function type: {functionType}")
		};
		var workingDir = Path.Combine(solutionRoot, "test", "azure", "applications", $"Elastic.AzureFunctionApp.{name}");
		LogLines.Add($"func working directory: {workingDir}");
		Directory.Exists(workingDir).Should().BeTrue();

		// https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local
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
		_funcProcess.ErrorDataReceived += (sender, args) => LogLines.Add("[func] [ERROR] " + args.Data);
		_funcProcess.OutputDataReceived += (sender, args) =>
		{
			if (args.Data != null)
			{
				LogLines.Add("[func] " + args.Data);
				if (args.Data != null && args.Data.Contains("Host lock lease acquired by instance ID"))
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

	public Uri CreateUri(string path) => new(BaseUri, path);

	internal async Task<TransactionDto> InvokeFunction(ITestOutputHelper output, Uri uri)
	{
		var transactionName = $"GET {uri.AbsolutePath}";
		output.WriteLine($"Invoking {uri} ...");
		var attempt = 0;
		const int maxAttempts = 360;
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Add("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
		while (++attempt < maxAttempts)
		{
			try
			{
				var result = await httpClient.GetAsync(uri);
				var s = await result.Content.ReadAsStringAsync();
				output.WriteLine(s);
				break;
			}
			catch (Exception ex)
			{
				output.WriteLine(ex.ToString());
				LogLines.Add($"Failed: {ex}");
				await Task.Delay(TimeSpan.FromSeconds(1));
			}
		}

		attempt.Should().BeLessThan(maxAttempts, $"Could not connect to function running on {uri}");
		var transaction = WaitForTransaction(transactionName);
		//Assert_MetaData(_context.GetMetaData());
		//Assert_ColdStart(transaction);
		return transaction;
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
	internal MetadataDto GetMetaData() => _apmServer.ReceivedData.Metadata.First();
}

