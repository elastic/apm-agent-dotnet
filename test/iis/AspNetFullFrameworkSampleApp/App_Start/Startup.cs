// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using AspNetFullFrameworkSampleApp.Models;
using AspNetFullFrameworkSampleApp.Services.Auth;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(AspNetFullFrameworkSampleApp.Startup))]
namespace AspNetFullFrameworkSampleApp
{
	public class Startup
	{
		public void Configuration(IAppBuilder app) => ConfigureAuth(app);

		// For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
		public void ConfigureAuth(IAppBuilder app) =>
			// Enable the application to use a cookie to store information for the signed in user
			app.UseCookieAuthentication(new CookieAuthenticationOptions
			{
				AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
				LoginPath = new PathString("/Account/Login"),
				Provider = new CookieAuthenticationProvider
				{
					// Enables the application to validate the security stamp when the user logs in.
					// This is a security feature which is used when you change a password or add an external login to your account.
					OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
						TimeSpan.FromMinutes(30),
						(manager, user) => manager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie))
				}
			});
	}
}
