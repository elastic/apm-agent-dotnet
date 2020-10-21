﻿// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace AspNetFullFrameworkSampleApp
{
	public class RouteConfig
	{
		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute(
				"Default",
				"{controller}/{action}/{id}",
				new { controller = "Home", action = "Index", id = UrlParameter.Optional },
				new { notRoutedWebforms = new NotRoutedWebformsConstraint() },
				new[] { "AspNetFullFrameworkSampleApp.Controllers" }
			);

			routes.MapPageRoute(Webforms.RoutedWebforms,
				Webforms.RoutedWebforms,
				"~/Webforms.aspx"
			);
		}
	}

	public class NotRoutedWebformsConstraint : IRouteConstraint
	{
		public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection) =>
			values.TryGetValue("controller", out var controller) && (string)controller != Webforms.RoutedWebforms;
	}
}
