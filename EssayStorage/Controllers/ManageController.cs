using EmailApp.Services;
using EssayStorage.Data;
using EssayStorage.Models;
using EssayStorage.Models.Database;
using EssayStorage.Models.ManageViewModels;
using EssayStorage.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace EssayStorage.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly UrlEncoder _urlEncoder;
        private ApplicationDbContext _db;
        IHostingEnvironment _appEnvironment; 
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        private const string RecoveryCodesKey = nameof(RecoveryCodesKey);

        public ManageController(
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          IEmailSender emailSender,
          ILogger<ManageController> logger,
          UrlEncoder urlEncoder,
          ApplicationDbContext db,
          IHostingEnvironment hostingEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _urlEncoder = urlEncoder;
            _db = db;
            _appEnvironment = hostingEnvironment;
        }
        
        [HttpPost]
        public async Task<bool> SendConfirmationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await SendConfirmationOnEmail(user, code);
            return true;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUser();
            var model = CreateIndexViewModelByUser(user);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IndexViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var result = await ChangeEmailAndNameIfNeed(model);
            return result;
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await GetCurrentUser();
            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword) return RedirectToAction(nameof(SetPassword));
            var model = new ChangePasswordViewModel { StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var user = await GetCurrentUser();
            var result = await ChangeUserPassword(model, user);
            return result;
        }

        [HttpGet]
        public async Task<IActionResult> SetPassword()
        {
            var user = await GetCurrentUser();
            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
                return RedirectToAction(nameof(ChangePassword));
            var model = new SetPasswordViewModel { StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var user = await GetCurrentUser();
            var addPasswordResult = await AddUserPassword(model, user);
            return addPasswordResult;
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLogins()
        {
            var user = await GetCurrentUser();
            var model = new ExternalLoginsViewModel { CurrentLogins = await _userManager.GetLoginsAsync(user) };
            model.OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => model.CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            model.ShowRemoveButton = await _userManager.HasPasswordAsync(user) || model.CurrentLogins.Count > 1;
            model.StatusMessage = StatusMessage;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkLogin(string provider)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            var redirectUrl = Url.Action(nameof(LinkLoginCallback));
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        public async Task<IActionResult> LinkLoginCallback()
        {
            var user = await GetCurrentUser();
            var info = await GetExternalLoginInfo(user);
            var result = await AddExternalLogin(user, info);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            StatusMessage = "The external login was added.";
            return RedirectToAction(nameof(ExternalLogins));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel model)
        {
            var user = await GetCurrentUser();
            var result = await _userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
            if (!result.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occurred removing external login for user with ID '{user.Id}'.");
            }
            await _signInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "The external login was removed.";
            return RedirectToAction(nameof(ExternalLogins));
        }

        [HttpGet]
        public IActionResult NewEssay()
        {
            return View("EditEssay");
        }

        [HttpGet]
        public async Task<IActionResult> UserEssays()
        {
            var user = await GetCurrentUser();
            List<Essay> essays = _db.Essays.Where((e) => e.UserId == user.Id).ToList();
            ViewData.Add("essays", essays);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (file != null)
            {
                string path = "/images/avatars/" + user.UserName + ".png";
                using (var filestream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                    await file.CopyToAsync(filestream);
                user.PicturePath = path;
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
            }
            IndexViewModel model = CreateIndexViewModelByUser(user);
            model.PicturePath = user.PicturePath;
            return RedirectToAction("Index", model);
        }

        private async Task<IdentityResult> AddExternalLogin(ApplicationUser user, ExternalLoginInfo info)
        {
            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
                throw new ApplicationException($"Unexpected error occurred adding external login for user with ID '{user.Id}'.");
            return result;
        }

        private async Task<ExternalLoginInfo> GetExternalLoginInfo(ApplicationUser user)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
                throw new ApplicationException($"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
            return info;
        }

        private async Task<IActionResult> AddUserPassword(SetPasswordViewModel model, ApplicationUser user)
        {
            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                AddErrors(addPasswordResult);
                return View(model);
            }
            await _signInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "Your password has been set.";
            return RedirectToAction(nameof(SetPassword));
        }

        private async Task<IActionResult> ChangeUserPassword(ChangePasswordViewModel model, ApplicationUser user)
        {
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return View(model);
            }
            await _signInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "Your password has been changed.";
            return RedirectToAction(nameof(ChangePassword));
        }

        private async Task<IActionResult> ChangeEmailIfNeed(IndexViewModel model, ApplicationUser user)
        {
            if (model.Email != user.Email)
            {
                try { await _userManager.SetEmailAsync(user, model.Email); }
                catch { ViewData.Add("error", "e-mail"); return View("ManageError"); }
            }
            return null;
        }

        private async Task<IActionResult> ChangeNameIfNeed(IndexViewModel model, ApplicationUser user)
        {
            if (model.Username != user.UserName)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Username);
                if (!setUserNameResult.Succeeded)
                {
                    ViewData.Add("error", "username"); return View("ManageError");
                }
            }
            return null;
        }

        private async Task<IActionResult> ChangeEmailAndNameIfNeed(IndexViewModel model)
        {
            var user = await GetCurrentUser();
            var changeEmailResult = await ChangeEmailIfNeed(model, user);
            if (changeEmailResult != null) return changeEmailResult;
            var changeNameResult = await ChangeNameIfNeed(model, user);
            if (changeNameResult != null) return changeNameResult;
            StatusMessage = "Your profile has been updated";
            return RedirectToAction(nameof(Index));
        }

        private async Task<ApplicationUser> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            return user;
        }

        private IndexViewModel CreateIndexViewModelByUser(ApplicationUser user)
        {
            return new IndexViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                IsEmailConfirmed = user.EmailConfirmed,
                StatusMessage = StatusMessage,
                PicturePath = user.PicturePath
            };
        }

        private async Task SendConfirmationOnEmail(ApplicationUser user, string code)
        {
            var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
            EmailService emailService = new EmailService();
            await emailService.SendEmailAsync(user.Email, "Confirm your account",
                $"Подтвердите регистрацию, перейдя по ссылке: <a href='{callbackUrl}'>link</a>");
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }


        #endregion
    }
}
