using CinemaProject.Data;
using CinemaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CinemaProject.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Roles = AppRoles.Customer)]
public sealed class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Challenge();
        var customer = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == customerId, cancellationToken);
        if (customer is null) return Challenge();

        var orders = await db.TicketOrders.AsNoTracking()
            .Where(order => order.CustomerAccountId == customerId)
            .OrderByDescending(order => order.CreatedUtc)
            .Select(order => new CustomerOrderRowViewModel
            {
                OrderId = order.Id,
                MovieName = order.Movie.Name,
                Quantity = order.Quantity,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency.ToUpper(),
                Status = order.Status,
                CreatedUtc = order.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        return View(new CustomerDashboardViewModel
        {
            Name = customer.Name,
            Email = customer.Email ?? string.Empty,
            Orders = orders
        });
    }
}
