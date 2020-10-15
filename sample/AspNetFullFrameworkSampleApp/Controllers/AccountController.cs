using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AspNetFullFrameworkSampleApp.ActionFilters;
using AspNetFullFrameworkSampleApp.Bootstrap;
using AspNetFullFrameworkSampleApp.Data;
using AspNetFullFrameworkSampleApp.Models;
using AspNetFullFrameworkSampleApp.Services.Auth;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataProtection;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	public class AccountController : ControllerBase
	{
		private ApplicationSignInManager _signInManager;
		private ApplicationUserManager _userManager;

		// TODO: dependency injection over service creation...
		private ApplicationSignInManager SignInManager =>
			_signInManager ?? new ApplicationSignInManager(UserManager, HttpContext.GetOwinContext().Authentication);

		private ApplicationUserManager UserManager =>
			_userManager ?? new ApplicationUserManager(
				new UserStore<ApplicationUser>(new SampleDataDbContext()),
				new DpapiDataProtectionProvider("ASP.NET Full Framework sample app"));

		[RedirectIfAuthenticated]
		public ActionResult Login(string returnUrl)
		{
			ViewBag.ReturnUrl = returnUrl;
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[RedirectIfAuthenticated]
		public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
		{
			if (!ModelState.IsValid)
				return View(model);

			// This doesn't count login failures towards account lockout
			// To enable password failures to trigger account lockout, change to shouldLockout: true
			var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
			switch (result)
			{
				case SignInStatus.Success:
					return RedirectToLocal(returnUrl);
				case SignInStatus.Failure:
				default:
					ModelState.AddModelError("", "Invalid login attempt.");
					return View(model);
			}
		}

		[RedirectIfAuthenticated]
		public ActionResult Register() => View();

		[HttpPost]
		[ValidateAntiForgeryToken]
		[RedirectIfAuthenticated]
		public async Task<ActionResult> Register(RegisterViewModel model)
		{
			if (ModelState.IsValid)
			{
				var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
				var result = await UserManager.CreateAsync(user, model.Password);
				if (result.Succeeded)
				{
					var code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
					var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

					// IMPORTANT: this would normally be sent as an email to confirm, but for simplicity, will be rendered on the next page
					AddAlert(new SuccessAlert(
						"Account created",
						"You can now log in. Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>"));

					return RedirectToAction("Index", "Home");
				}
				AddErrors(result);
			}

			return View(model);
		}

		public async Task<ActionResult> ConfirmEmail(string userId, string code)
		{
			if (userId == null || code == null)
				return View("Error");

			var result = await UserManager.ConfirmEmailAsync(userId, code);
			return View(result.Succeeded ? "ConfirmEmail" : "Error");
		}

		[RedirectIfAuthenticated]
		public ActionResult ForgotPassword() => View();

		[HttpPost]
		[ValidateAntiForgeryToken]
		[RedirectIfAuthenticated]
		public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
		{
			if (ModelState.IsValid)
			{
				var user = await UserManager.FindByNameAsync(model.Email);
				if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
				{
					// Don't reveal that the user does not exist or is not confirmed
					return View("ForgotPasswordConfirmation");
				}

				var code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
				var callbackUrl = Url.Action(
					"ResetPassword",
					"Account",
					new { userId = user.Id, code }, Request.Url.Scheme);

				// IMPORTANT: this would normally be sent as an email to confirm, but for simplicity, will be rendered on the next page
				AddAlert(new SuccessAlert(
					"Password reset",
					"Please reset the password for your account by clicking <a href=\"" + callbackUrl + "\">here</a>"));

				return RedirectToAction("ForgotPasswordConfirmation", "Account");
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		[RedirectIfAuthenticated]
		public ActionResult ForgotPasswordConfirmation() => View();

		[RedirectIfAuthenticated]
		public ActionResult ResetPassword(string code) => code == null ? View("Error") : View();

		[HttpPost]
		[ValidateAntiForgeryToken]
		[RedirectIfAuthenticated]
		public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}
			var user = await UserManager.FindByNameAsync(model.Email);
			if (user == null)
			{
				// Don't reveal that the user does not exist
				return RedirectToAction("ResetPasswordConfirmation", "Account");
			}
			var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
			if (result.Succeeded)
			{
				return RedirectToAction("ResetPasswordConfirmation", "Account");
			}
			AddErrors(result);
			return View();
		}

		[RedirectIfAuthenticated]
		public ActionResult ResetPasswordConfirmation() => View();

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize]
		public ActionResult LogOff()
		{
			AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
			return RedirectToAction("Index", "Home");
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_userManager?.Dispose();
				_userManager = null;

				_signInManager?.Dispose();
				_signInManager = null;
			}

			base.Dispose(disposing);
		}

		private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

		private void AddErrors(IdentityResult result)
		{
			foreach (var error in result.Errors)
			{
				ModelState.AddModelError("", error);
			}
		}

		private ActionResult RedirectToLocal(string returnUrl)
		{
			if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);

			return RedirectToAction("Index", "Home");
		}
	}
}
