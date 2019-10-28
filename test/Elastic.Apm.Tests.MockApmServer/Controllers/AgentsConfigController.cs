using System;
using System.Runtime.CompilerServices;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Elastic.Apm.Tests.MockApmServer.Controllers
{
	[Route("config/v1/agents")]
	[ApiController]
	public class AgentsConfigController : ControllerBase
	{
		private const string ThisClassName = nameof(AgentsConfigController);
		private readonly IApmLogger _logger;

		private readonly MockApmServer _mockApmServer;

		public AgentsConfigController(MockApmServer mockApmServer)
		{
			_mockApmServer = mockApmServer;
			_logger = mockApmServer.InternalLogger.Scoped(ThisClassName + "#" + RuntimeHelpers.GetHashCode(this).ToString("X"));

			_logger.Debug()?.Log("Constructed with mock APM Server: {MockApmServer}", _mockApmServer);
		}

		[HttpGet]
		public IActionResult Get() => _mockApmServer.DoUnderLock(() =>
		{
			try
			{
				return GetImpl();
			}
			catch (Exception ex)
			{
				_logger.Error()?.LogException(ex, nameof(GetImpl) + " has thrown exception");
				throw;
			}
		});

		private IActionResult GetImpl()
		{
			_logger.Debug()
				?.Log("Received get-agents-config request with query string: {QueryString}."
					+ " Current thread: {ThreadDesc}."
					, Request.QueryString, DbgUtils.CurrentThreadDesc);

			var getAgentsConfig = _mockApmServer.GetAgentsConfig;
			if (getAgentsConfig == null) return NotFound("Get-agents-config API is not enabled");

			var result = getAgentsConfig(Request, Response);

			_logger.Debug()?.Log("Response to get-agents-config: {Response}", result);

			return result;
		}
	}
}
