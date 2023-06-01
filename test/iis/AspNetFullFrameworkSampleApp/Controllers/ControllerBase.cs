// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.Web.Mvc;
using AspNetFullFrameworkSampleApp.Bootstrap;
using AspNetFullFrameworkSampleApp.Extensions;
using AspNetFullFrameworkSampleApp.Mvc;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	public abstract class ControllerBase : Controller
	{
		protected void AddAlert(Alert alert) => TempData.Put("alert", alert);

		protected ActionResult JsonBadRequest(object content) => new JsonBadRequestResult { Data = content, };

		protected ActionResult Stream(Stream stream, string contentType, int statusCode) => new StreamResult(stream, contentType, statusCode);
	}
}
