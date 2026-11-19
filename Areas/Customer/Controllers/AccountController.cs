using CinemaProject.Services;
using CinemaProject.Models;
using CinemaProject.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CinemaProject.Areas.Customer.Controllers;

[Area("Customer")]
public sealed class AccountController : Controller
{
    private readonly IAccountService _accounts;
    private readonly UserManager<CustomerAccount> _users;
    private readonly SignInManager<CustomerAccount> _signIn;
    private readonly IProfileImageStorage _profileImages;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accounts,
        UserManager<CustomerAccount> users,
        SignInManager<CustomerAccount> signIn,
        IProfileImageStorage profileImages,
        IEmailSender emailSender,
        ILogger<AccountController> logger)
    {
        _accounts = accounts;
        _users = users;
        _signIn = signIn;
        _profileImages = profileImages;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return await DashboardRedirectAsync(await _users.GetUserAsync(User));
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginInputModel());
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return await DashboardRedirectAsync(await _users.GetUserAsync(User));
        }

        return View(new RegisterInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginInputModel model,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var signIn = await _signIn.PasswordSignInAsync(
            model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);
        if (!signIn.Succeeded)
        {
            ModelState.AddModelError(string.Empty, signIn.IsLockedOut
                ? "This account is temporarily locked. Try again later."
                : "The email or password is incorrect.");
            return View(model);
        }

        var account = await _users.FindByEmailAsync(model.Email.Trim());
        if (account is null) return RedirectToAction("Index", "Home");

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return await DashboardRedirectAsync(account);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterInputModel model,
        IFormFile? profileImage,
        CancellationToken cancellationToken)
    {
        if (profileImage is { Length: > 0 }
            && !await _profileImages.IsValidImageAsync(profileImage, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "Choose a JPG, PNG, or WEBP image smaller than 3 MB.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? profileImagePath = null;
        if (profileImage is { Length: > 0 })
        {
            profileImagePath = await _profileImages.SaveAsync(profileImage, cancellationToken);
        }

        AccountRegistrationResult result;
        try
        {
            result = await _accounts.RegisterAsync(
                model.Name, model.Email, model.Password, profileImagePath, cancellationToken);
        }
        catch
        {
            _profileImages.Delete(profileImagePath);
            throw;
        }

        if (result == AccountRegistrationResult.EmailAlreadyRegistered)
        {
            _profileImages.Delete(profileImagePath);
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        if (result != AccountRegistrationResult.Created)
        {
            _profileImages.Delete(profileImagePath);
            ModelState.AddModelError(string.Empty, "Your account could not be created. Please check your details and try again.");
            return View(model);
        }

        var account = await _users.FindByEmailAsync(model.Email.Trim());
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Your account was created. Please sign in.");
            return View("Login", new LoginInputModel { Email = model.Email });
        }

        await _signIn.SignInAsync(account, isPersistent: false);

        return await DashboardRedirectAsync(account);
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordInputModel());

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordInputModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var reset = await _accounts.CreatePasswordResetLinkAsync(model.Email, cancellationToken);
            if (reset is not null)
            {
                var resetUrl = Url.ActionLink(
                    action: nameof(ResetPassword),
                    controller: "Account",
                    values: new { area = "Customer", userId = reset.UserId, token = reset.Token },
                    protocol: Request.Scheme);

                if (!string.IsNullOrWhiteSpace(resetUrl))
                {
                    await _emailSender.SendPasswordResetLinkAsync(
                        reset.Email, reset.Name, resetUrl, cancellationToken);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not send a password reset email.");
        }

        ViewData["ResetRequestSent"] = true;
        return View(new ForgotPasswordInputModel());
    }

    [HttpGet]
    public IActionResult ResetPassword(int userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return View("ResetPasswordInvalid");
        }

        return View(new ResetPasswordInputModel { UserId = userId, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordInputModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var reset = await _accounts.ResetPasswordAsync(model.UserId, model.Token, model.Password, cancellationToken);
        if (!reset)
        {
            return View("ResetPasswordInvalid");
        }

        TempData["PasswordResetSuccess"] = "Your password has been updated. You can sign in now.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private async Task<IActionResult> DashboardRedirectAsync(CustomerAccount? user)
    {
        if (user is not null && await _users.IsInRoleAsync(user, AppRoles.SuperAdmin))
            return RedirectToAction("Index", "SuperAdmin", new { area = "Admin" });
        if (user is not null && await _users.IsInRoleAsync(user, AppRoles.Admin))
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
    }
}
