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
		private Thread _workLoopThread;
		private readonly string _dbgDerivedClassName;

		internal BackendCommComponentBase(bool isEnabled, IApmLogger logger, string dbgDerivedClassName, Service service
			, IConfiguration configuration, HttpMessageHandler httpMessageHandler = null
		)
		{
			_dbgName = $"{ThisClassName} ({dbgDerivedClassName})";
			_dbgDerivedClassName = dbgDerivedClassName;
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
		}

		protected abstract Task WorkLoopIteration();

		internal bool IsRunning => _workLoopThread.IsAlive;

		protected void StartWorkLoop()
		{
			_workLoopThread = new Thread(WorkLoop) { Name = $"ElasticApm{_dbgDerivedClassName}", IsBackground = true };
			_workLoopThread.Start();

			_logger.Debug()?.Log("Waiting for work loop started event...");
			_loopStarted.Wait();
			_logger.Debug()?.Log("Work loop started signaled");
		}

		private void WorkLoop()
		{
			_logger.Debug()?.Log("Signaling work loop started event...");
			_loopStarted.Set();

			while (!CancellationTokenSource.IsCancellationRequested)
			{
				try
				{
					// This runs on the dedicated work loop thread
					// In order to make sure iterations don't overlap we wait for the current iteration - the intention here is to block
					WorkLoopIteration().Wait();
				}
				catch (OperationCanceledException)
				{
					_logger.Debug()
						?.Log(nameof(WorkLoop) + "OperationCanceledException - Current thread: {ThreadDesc}"
							, DbgUtils.CurrentThreadDesc);
				}
				catch (Exception ex)
				{
					_logger.Error()
						?.LogException(ex, nameof(WorkLoop) + " Current thread: {ThreadDesc}", DbgUtils.CurrentThreadDesc);
				}
			}

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

				_workLoopThread.Join();

				_logger.Debug()?.Log("Disposing HttpClient...");
				HttpClient.Dispose();

				_logger.Debug()?.Log("Disposing CancellationTokenSource...");
				CancellationTokenSource.Dispose();

				_logger.Debug()?.Log("Exiting...");
			});
		}

		protected void ThrowIfDisposed()
		{
			if (_disposableHelper != null && _disposableHelper.HasStarted)
				throw new ObjectDisposedException( /* objectName: */ _dbgName);
		}
	}
}
