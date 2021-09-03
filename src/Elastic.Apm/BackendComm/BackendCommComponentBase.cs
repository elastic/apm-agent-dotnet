// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.BackendComm
{
	internal abstract class BackendCommComponentBase : IDisposable
	{
		private const string ThisClassName = nameof(BackendCommComponentBase);

		protected CancellationTokenSource CancellationTokenSource { get; }
		protected HttpClient HttpClient { get; }

		private readonly string _dbgName;
		private readonly DisposableHelper _disposableHelper;
		private readonly bool _isEnabled;
		private readonly IApmLogger _logger;
		private readonly ManualResetEventSlim _loopCompleted;
		private readonly ManualResetEventSlim _loopStarted;
		private readonly SingleThreadTaskScheduler _singleThreadTaskScheduler;

		internal BackendCommComponentBase(bool isEnabled, IApmLogger logger, string dbgDerivedClassName, Service service
			, IConfigurationSnapshot configuration, HttpMessageHandler httpMessageHandler = null
		)
		{
			_dbgName = $"{ThisClassName} ({dbgDerivedClassName})";
			_logger = logger?.Scoped(_dbgName);
			_isEnabled = isEnabled;

			if (!_isEnabled)
			{
				_logger.Debug()?.Log("Disabled - exiting without initializing any members used by work loop");
				return;
			}

			CancellationTokenSource = new CancellationTokenSource();

			_disposableHelper = new DisposableHelper();

			_loopStarted = new ManualResetEventSlim();
			_loopCompleted = new ManualResetEventSlim();

			HttpClient = BackendCommUtils.BuildHttpClient(logger, configuration, service, _dbgName, httpMessageHandler);

			_singleThreadTaskScheduler = new SingleThreadTaskScheduler($"ElasticApm{dbgDerivedClassName}", logger);
		}

		protected abstract Task WorkLoopIteration();

		internal bool IsRunning => _singleThreadTaskScheduler.IsRunning;

		private void PostToInternalTaskScheduler(string dbgActionDesc, Func<Task> asyncAction
			, TaskCreationOptions taskCreationOptions = TaskCreationOptions.None
		)
		{
#pragma warning disable 4014
			// We don't pass any CancellationToken on purpose because in some case (for example work loop)
			// we wait for asyncAction to start so we should never cancel it before it starts
			Task.Factory.StartNew(asyncAction, CancellationToken.None, taskCreationOptions, _singleThreadTaskScheduler);
#pragma warning restore 4014
			_logger.Debug()?.Log("Posted {DbgTaskDesc} to internal task scheduler", dbgActionDesc);
		}

		protected void StartWorkLoop()
		{
			PostToInternalTaskScheduler("Work loop", WorkLoop, TaskCreationOptions.LongRunning);

			_logger.Debug()?.Log("Waiting for work loop started event...");
			_loopStarted.Wait();
			_logger.Debug()?.Log("Work loop started signaled");
		}

		private async Task WorkLoop()
		{
			_logger.Debug()?.Log("Signaling work loop started event...");
			_loopStarted.Set();

			await ExceptionUtils.DoSwallowingExceptions(_logger, async () =>
				{
					while (!CancellationTokenSource.IsCancellationRequested)
						await WorkLoopIteration().ConfigureAwait(false);
					// ReSharper disable once FunctionNeverReturns
				}
				, dbgCallerMethodName: _dbgName + "." + DbgUtils.CurrentMethodName()).ConfigureAwait(false);

			_logger.Debug()?.Log("Signaling work loop completed event...");
			_loopCompleted.Set();
		}

		public void Dispose()
		{
			if (!_isEnabled)
			{
				_logger.Debug()?.Log("Disabled - nothing to dispose, exiting");
				return;
			}

			_disposableHelper.DoOnce(_logger, _dbgName, () =>
			{
				_logger.Debug()?.Log("Calling CancellationTokenSource.Cancel()...");
				CancellationTokenSource.Cancel();
				_logger.Debug()?.Log("Called CancellationTokenSource.Cancel()");

				_logger.Debug()
					?.Log("Waiting for loop to exit... Is cancellation token signaled: {IsCancellationRequested}",
						CancellationTokenSource.IsCancellationRequested);
				_loopCompleted.Wait();

				_logger.Debug()?.Log("Disposing _singleThreadTaskScheduler ...");
				_singleThreadTaskScheduler.Dispose();

				_logger.Debug()?.Log("Disposing HttpClient...");
				HttpClient.Dispose();

				_logger.Debug()?.Log("Disposing CancellationTokenSource...");
				CancellationTokenSource.Dispose();

				_logger.Debug()?.Log("Exiting...");
			});
		}

		protected void ThrowIfDisposed()
		{
			if (_disposableHelper.HasStarted) throw new ObjectDisposedException( /* objectName: */ _dbgName);
		}
	}
}
