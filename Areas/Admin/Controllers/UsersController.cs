using CinemaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaProject.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public sealed class UsersController(UserManager<CustomerAccount> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accounts = await users.Users.OrderBy(user => user.Name).ToListAsync(cancellationToken);
        var rows = new List<UserRoleRowViewModel>(accounts.Count);
        foreach (var account in accounts)
        {
            var roles = await users.GetRolesAsync(account);
            rows.Add(new UserRoleRowViewModel
            {
                Id = account.Id,
                Name = account.Name,
                Email = account.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Unassigned"
            });
        }
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var user = await users.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound();
        var roles = await users.GetRolesAsync(user);
        ViewBag.Roles = AppRoles.All;
        return View(new EditUserRoleViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            SelectedRole = roles.FirstOrDefault() ?? AppRoles.Customer
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserRoleViewModel model, CancellationToken cancellationToken)
    {
        var user = await users.Users.SingleOrDefaultAsync(item => item.Id == model.Id, cancellationToken);
        if (user is null) return NotFound();
        if (!AppRoles.All.Contains(model.SelectedRole, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(model.SelectedRole), "Choose a valid role.");
        }

        var oldRoles = await users.GetRolesAsync(user);
        var isSelf = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == user.Id.ToString();
        if (isSelf && model.SelectedRole != AppRoles.SuperAdmin)
        {
            ModelState.AddModelError(nameof(model.SelectedRole), "You cannot remove your own SuperAdmin access.");
        }
        if (oldRoles.Contains(AppRoles.SuperAdmin, StringComparer.Ordinal)
            && model.SelectedRole != AppRoles.SuperAdmin
            && (await users.GetUsersInRoleAsync(AppRoles.SuperAdmin)).Count <= 1)
        {
            ModelState.AddModelError(nameof(model.SelectedRole), "The last SuperAdmin cannot be demoted.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Roles = AppRoles.All;
            return View(model);
        }

        var removed = oldRoles.Count == 0 || (await users.RemoveFromRolesAsync(user, oldRoles)).Succeeded;
        if (!removed)
        {
            ModelState.AddModelError(string.Empty, "The current roles could not be removed.");
            ViewBag.Roles = AppRoles.All;
            return View(model);
        }

        var assigned = await users.AddToRoleAsync(user, model.SelectedRole);
        if (!assigned.Succeeded)
        {
            if (oldRoles.Count > 0) await users.AddToRolesAsync(user, oldRoles);
            ModelState.AddModelError(string.Empty, string.Join(" ", assigned.Errors.Select(error => error.Description)));
            ViewBag.Roles = AppRoles.All;
            return View(model);
        }

        await users.UpdateSecurityStampAsync(user);

        TempData["SuccessMessage"] = $"{user.Name}'s role is now {model.SelectedRole}.";
        return RedirectToAction(nameof(Index));
    }
}
