using CinemaProject.Models;
using CinemaProject.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CinemaProject.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class AccountController(
    UserManager<CustomerAccount> users,
    SignInManager<CustomerAccount> signIn) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new AdminLoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await signIn.PasswordSignInAsync(model.Email.Trim(), model.Password, false, lockoutOnFailure: true);
        var user = result.Succeeded ? await users.FindByEmailAsync(model.Email.Trim()) : null;
        if (user is null
            || (!await users.IsInRoleAsync(user, AppRoles.Admin)
                && !await users.IsInRoleAsync(user, AppRoles.SuperAdmin)))
        {
            if (result.Succeeded) await signIn.SignOutAsync();
            ModelState.AddModelError(string.Empty, result.IsLockedOut
                ? "This account is temporarily locked. Try again later."
                : "The email or password is incorrect, or this account does not have dashboard access.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return await users.IsInRoleAsync(user, AppRoles.SuperAdmin)
            ? RedirectToAction("Index", "SuperAdmin")
            : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
