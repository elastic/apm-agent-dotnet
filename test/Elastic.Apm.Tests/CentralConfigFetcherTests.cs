using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable ImplicitlyCapturedClosure

namespace Elastic.Apm.Tests
{
	public class CentralConfigFetcherTests : LoggingTestBase
	{
		private const string ThisClassName = nameof(CentralConfigFetcherTests);

		public CentralConfigFetcherTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[Fact]
		public void Dispose_stops_the_thread()
		{
			CentralConfigFetcher lastCentralConfigFetcher;
			using (var agent = new ApmAgent(new AgentComponents()))
			{
				lastCentralConfigFetcher = (CentralConfigFetcher)agent.CentralConfigFetcher;
				lastCentralConfigFetcher.IsRunning.Should().BeTrue();
			}
			lastCentralConfigFetcher.IsRunning.Should().BeFalse();
		}

		[Theory]
		[InlineData(5)]
		[InlineData(9)]
		[InlineData(10)]
		[InlineData(11)]
		[InlineData(20)]
		[InlineData(40)]
		public void Create_many_concurrent_instances(int numberOfAgentInstances)
		{
			var agents = new ApmAgent[numberOfAgentInstances];
			numberOfAgentInstances.Repeat(i =>
			{
				agents[i] = new ApmAgent(new AgentComponents());
				((CentralConfigFetcher)agents[i].CentralConfigFetcher).IsRunning.Should().BeTrue();
				((PayloadSenderV2)agents[i].PayloadSender).IsRunning.Should().BeTrue();
			});

			numberOfAgentInstances.Repeat(i =>
			{
				LoggerBase.Context[$"Thread: `{Thread.CurrentThread.Name}' (Managed ID: {Thread.CurrentThread.ManagedThreadId})"
						+ $": {ThisClassName}.{DbgUtils.GetCurrentMethodName()}"] = "Before agents[i].Dispose()."
					+ $" numberOfAgentInstances: {numberOfAgentInstances}. i: {i}.";
				agents[i].Dispose();
				((CentralConfigFetcher)agents[i].CentralConfigFetcher).IsRunning.Should().BeFalse();
				((PayloadSenderV2)agents[i].PayloadSender).IsRunning.Should().BeFalse();
			});

			LoggerBase.Context[$"Thread: `{Thread.CurrentThread.Name}' (Managed ID: {Thread.CurrentThread.ManagedThreadId})"
				+ $": {ThisClassName}.{DbgUtils.GetCurrentMethodName()}"] = "Done";
		}

		[Theory]
		[InlineData(5)]
		[InlineData(9)]
		[InlineData(10)]
		[InlineData(11)]
		[InlineData(20)]
		[InlineData(40)]
		public async Task Concurrent_HttpClients(int numberOfInstances)
		{
			var config = new MockConfigSnapshot();
			var serviceInfo = Service.GetDefaultService(config, LoggerBase);
			var httpClients = new HttpClient[numberOfInstances];
			numberOfInstances.Repeat(i => { httpClients[i] = BackendCommUtils.BuildHttpClient(LoggerBase, config, serviceInfo, TestDisplayName); });

			var getRequests = new Task[numberOfInstances];

			numberOfInstances.Repeat(i =>
			{
				LoggerBase.Context[$"Thread: `{Thread.CurrentThread.Name}' (Managed ID: {Thread.CurrentThread.ManagedThreadId})"
					+ $": {TestDisplayName}"] = $"Before httpClients[i].GetAsync. i: {i}.";
				try
				{
					getRequests[i] = httpClients[i].GetAsync("/");
					LoggerBase.Context[$"Thread: `{Thread.CurrentThread.Name}' (Managed ID: {Thread.CurrentThread.ManagedThreadId})"
						+ $": {TestDisplayName}"] = $"httpClients[i].GetAsync: {getRequests[i].Status}. i: {i}.";
				}
				catch (Exception ex)
				{
					LoggerBase.Context[$"Thread: `{Thread.CurrentThread.Name}' (Managed ID: {Thread.CurrentThread.ManagedThreadId})"
						+ $": {TestDisplayName}"] = $"httpClients[i].GetAsync: {ex.GetType().FullName}: {ex.Message}. i: {i}.";
				}
			});

			LoggerBase.Context[$"Thread: `{Thread.CurrentThread.Name}' (Managed ID: {Thread.CurrentThread.ManagedThreadId})"
				+ $": {TestDisplayName}"] = $"Before await Task.WhenAll(getRequests)";
			try
			{
				await Task.WhenAll(getRequests);
			}
			catch (Exception ex)
			{
				LoggerBase.Context[$"Thread: `{Thread.CurrentThread.Name}' (Managed ID: {Thread.CurrentThread.ManagedThreadId})"
					+ $": {TestDisplayName}"] = $"await Task.WhenAll(getRequests): {ex.GetType().FullName}: {ex.Message}";
			}

			numberOfInstances.Repeat(i =>
			{
				LoggerBase.Context[$"Thread: `{Thread.CurrentThread.Name}' (Managed ID: {Thread.CurrentThread.ManagedThreadId})"
					+ $": {TestDisplayName}"] = "Before httpClients[i].Dispose(). i: {i}.";
				httpClients[i].Dispose();
			});

			LoggerBase.Context[$"Thread: `{Thread.CurrentThread.Name}' (Managed ID: {Thread.CurrentThread.ManagedThreadId})"
				+ $": {TestDisplayName}"] = "Done";
		}
	}
}
