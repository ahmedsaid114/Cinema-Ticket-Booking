using CinemaProject.Data;
using CinemaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace CinemaProject.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public sealed class SuperAdminController(
    ApplicationDbContext db,
    UserManager<CustomerAccount> users) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var customers = await users.GetUsersInRoleAsync(AppRoles.Customer);
        var admins = await users.GetUsersInRoleAsync(AppRoles.Admin);
        var superAdmins = await users.GetUsersInRoleAsync(AppRoles.SuperAdmin);
        var model = new SuperAdminDashboardViewModel
        {
            TotalUsers = await users.Users.CountAsync(cancellationToken),
            Customers = customers.Count,
            Admins = admins.Count,
            SuperAdmins = superAdmins.Count
        };

        ViewData["PaidOrders"] = await db.TicketOrders.CountAsync(order => order.Status == TicketOrderStatuses.Paid, cancellationToken);
        return View(model);
    }
}
