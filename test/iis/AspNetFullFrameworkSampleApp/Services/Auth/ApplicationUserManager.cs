// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using AspNetFullFrameworkSampleApp.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security.DataProtection;

namespace AspNetFullFrameworkSampleApp.Services.Auth
{
	/// <summary>
	/// ASP.NET Identity user manager for this application
	/// </summary>
	public class ApplicationUserManager : UserManager<ApplicationUser>
	{
		public ApplicationUserManager(IUserStore<ApplicationUser> store, IDataProtectionProvider dataProtectionProvider)
			: base(store)
		{
			UserValidator = new UserValidator<ApplicationUser>(this)
			{
				AllowOnlyAlphanumericUserNames = false,
				RequireUniqueEmail = true
			};

			PasswordValidator = new PasswordValidator
			{
				RequiredLength = 1,
				RequireNonLetterOrDigit = false,
				RequireDigit = false,
				RequireLowercase = false,
				RequireUppercase = false,
			};

			UserLockoutEnabledByDefault = false;
			DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
			MaxFailedAccessAttemptsBeforeLockout = 5;

			ClaimsIdentityFactory = new ApplicationUserClaimsIdentityFactory();
			UserTokenProvider =
				new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
		}
	}
}
