// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Claims;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace AspNetFullFrameworkSampleApp.Services.Auth
{
	public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
	{
		public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
			: base(userManager, authenticationManager)
		{
		}

		public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user) => 
			user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);

		public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context) => 
			new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
	}
}
