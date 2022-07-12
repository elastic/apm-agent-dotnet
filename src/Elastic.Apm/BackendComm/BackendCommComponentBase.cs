// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
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
		private ManualResetEventSlim _loopStarted;
		protected Thread WorkLoopThread;
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

		protected abstract void WorkLoopIteration();

		internal bool IsRunning => WorkLoopThread.IsAlive;

		protected void StartWorkLoop()
		{
			StartWorkLoopThread();

			_logger.Debug()?.Log("Waiting for work loop started event...");
			_loopStarted.Wait();
			_logger.Debug()?.Log("Work loop started signaled");
		}

		protected void StartWorkLoopThread()
		{
			_loopStarted = new ManualResetEventSlim();

			WorkLoopThread = new Thread(WorkLoop) { Name = $"ElasticApm{_dbgDerivedClassName}", IsBackground = true };
			WorkLoopThread.Start();
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
					WorkLoopIteration();
				}
				catch (OperationCanceledException)
				{
					_logger.Debug()
						?.Log(nameof(WorkLoop) + "OperationCanceledException - Current thread: {ThreadDesc}"
							, DbgUtils.CurrentThreadDesc);
				}
				catch (Exception ex)
				{
					if (ex is AggregateException aggregateException && aggregateException.InnerExceptions.Any(e => e is TaskCanceledException))
					{
						_logger.Debug()
							?.LogException(ex, nameof(WorkLoop) + "TaskCanceledException -  Current thread: {ThreadDesc}",
								DbgUtils.CurrentThreadDesc);
					}
					else
					{
						_logger.Error()
							?.LogException(ex, nameof(WorkLoop) + " Current thread: {ThreadDesc}", DbgUtils.CurrentThreadDesc);
					}
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

				WorkLoopThread.Join();

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
