// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Security.Claims;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Models;
using Microsoft.AspNet.Identity;

namespace AspNetFullFrameworkSampleApp.Services.Auth
{
	/// <summary>
	/// ASP.NET Identity factory for creating <see cref="ClaimsIdentity"/> for users of this application
	/// </summary>
	public class ApplicationUserClaimsIdentityFactory : ClaimsIdentityFactory<ApplicationUser, string>
	{
		public override async Task<ClaimsIdentity> CreateAsync(UserManager<ApplicationUser, string> manager, ApplicationUser user, string authenticationType)
		{
			var claimsIdentity =  await base.CreateAsync(manager, user, authenticationType);

			// Add the email claim
			claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, user.Email));

			return claimsIdentity;
		}
	}
}
