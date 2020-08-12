// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace SampleAspNetCoreApp.Controllers
{
	public class AccountController : Controller
	{
		private readonly SignInManager<IdentityUser> _signInManager;
		private readonly UserManager<IdentityUser> _userManager;

		public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) =>
			(_userManager, _signInManager) = (userManager, signInManager);

		public IActionResult Login() => View();

		[HttpPost]
		public async Task<IActionResult> LoginUser([FromForm] string userName, [FromForm] string password)
		{
			var res = await _signInManager.PasswordSignInAsync(userName, password, true, false);

			if (res.Succeeded)
				return Redirect("/Home/Index");

			return View("Login");
		}

		public IActionResult Register() => View();

		[HttpPost]
		public async Task<IActionResult> RegisterUser([FromForm] string userName, [FromForm] string password)
		{
			var newUser = new IdentityUser { UserName = userName };
			var res = await _userManager.CreateAsync(newUser, password);

			if (res.Succeeded)
				ViewData["msg"] = "User registered, now you can log in";
			else
				ViewData["msg"] = $"Failed registering user: {res.Errors.First().Description}";

			return View("Register");
		}

		public async Task<IActionResult> LogOut()
		{
			await _signInManager.SignOutAsync();
			return Redirect("/Home/Index");
		}
	}
}
