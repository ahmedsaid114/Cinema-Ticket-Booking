using CinemaProject.Models;
using CinemaProject.Repositories;
using CinemaProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskCinema.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.AdminAccess)]
public sealed class HomeController(IUnitOfWork unitOfWork) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var paidOrders = unitOfWork.TicketOrders.QueryNoTracking()
            .Where(order => order.Status == TicketOrderStatuses.Paid);

        var model = new AdminDashboardViewModel
        {
            PaidOrders = await paidOrders.CountAsync(cancellationToken),
            TicketsSold = await paidOrders.SumAsync(order => (int?)order.Quantity, cancellationToken) ?? 0,
            GrossRevenue = await paidOrders.SumAsync(order => (decimal?)order.TotalAmount, cancellationToken) ?? 0m,
            PendingOrders = await unitOfWork.TicketOrders.QueryNoTracking()
                .CountAsync(order => order.Status == TicketOrderStatuses.Pending, cancellationToken),
            RecentOrders = await unitOfWork.TicketOrders.QueryNoTracking()
                .OrderByDescending(order => order.CreatedUtc)
                .Take(8)
                .Select(order => new AdminOrderRowViewModel
                {
                    OrderId = order.Id,
                    CustomerName = order.CustomerAccount.Name,
                    MovieName = order.Movie.Name,
                    Quantity = order.Quantity,
                    TotalAmount = order.TotalAmount,
                    Currency = order.Currency.ToUpper(),
                    Status = order.Status,
                    CreatedUtc = order.CreatedUtc
                })
                .ToListAsync(cancellationToken)
        };

        return View(model);
    }
}
