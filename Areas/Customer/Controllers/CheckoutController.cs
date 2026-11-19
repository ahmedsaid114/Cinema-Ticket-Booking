using System.Globalization;
using System.Security.Claims;
using CinemaProject.Models;
using CinemaProject.Repositories;
using CinemaProject.Services;
using CinemaProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskCinema.Areas.Customer.Controllers;

[Area("Customer")]
public sealed class CheckoutController(
    IUnitOfWork unitOfWork,
    IStripeCheckoutService stripe,
    ILogger<CheckoutController> logger,
    IWebHostEnvironment environment) : Controller
{
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Buy(int movieId, int quantity, CancellationToken cancellationToken)
    {
        if (quantity is < 1 or > 10)
        {
            TempData["PurchaseError"] = "Choose between 1 and 10 tickets.";
            return RedirectToAction("Details", "Movies", new { area = "Customer", id = movieId });
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
        {
            return Challenge();
        }

        var movie = await unitOfWork.Movies.Query()
            .SingleOrDefaultAsync(item => item.Id == movieId && item.Status, cancellationToken);
        if (movie is null || movie.Price <= 0)
        {
            return NotFound();
        }

        var order = new TicketOrder
        {
            CustomerAccountId = customerId,
            MovieId = movie.Id,
            Movie = movie,
            Quantity = quantity,
            UnitPrice = movie.Price,
            TotalAmount = movie.Price * quantity,
            Currency = "egp",
            Status = TicketOrderStatuses.Pending,
            CreatedUtc = DateTime.UtcNow
        };
        unitOfWork.TicketOrders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var successUrl = Url.Action(nameof(Success), "Checkout", new
            {
                area = "Customer",
                orderId = order.Id,
                session_id = "{CHECKOUT_SESSION_ID}"
            }, Request.Scheme)!.Replace("%7BCHECKOUT_SESSION_ID%7D", "{CHECKOUT_SESSION_ID}", StringComparison.OrdinalIgnoreCase);
            var cancelUrl = Url.Action(nameof(Cancelled), "Checkout", new
            {
                area = "Customer",
                orderId = order.Id
            }, Request.Scheme)!;

            var session = await stripe.CreateSessionAsync(order, email, successUrl, cancelUrl, cancellationToken);
            order.StripeCheckoutSessionId = session.Id;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Redirect(session.Url);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not start Stripe Checkout for ticket order {OrderId}.", order.Id);
            order.Status = TicketOrderStatuses.Cancelled;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            TempData["PurchaseError"] = environment.IsDevelopment()
                ? exception.Message
                : "Payment could not be started. Check the payment setup and try again.";
            return RedirectToAction("Details", "Movies", new { area = "Customer", id = movieId });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Success(int orderId, string? session_id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Challenge();
        var order = await unitOfWork.TicketOrders.Query()
            .Include(item => item.Movie)
            .SingleOrDefaultAsync(item => item.Id == orderId && item.CustomerAccountId == customerId, cancellationToken);
        if (order is null) return NotFound();

        if (order.Status == TicketOrderStatuses.Pending
            && !string.IsNullOrWhiteSpace(session_id)
            && string.Equals(session_id, order.StripeCheckoutSessionId, StringComparison.Ordinal))
        {
            try
            {
                var session = await stripe.GetSessionAsync(session_id, cancellationToken);
                if (session.PaymentStatus == "paid"
                    && session.OrderId == order.Id.ToString(CultureInfo.InvariantCulture)
                    && session.ClientReferenceId == order.Id.ToString(CultureInfo.InvariantCulture))
                {
                    order.Status = TicketOrderStatuses.Paid;
                    order.StripePaymentIntentId = session.PaymentIntentId;
                    order.PaidUtc = DateTime.UtcNow;
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not confirm Stripe Checkout for ticket order {OrderId}.", order.Id);
            }
        }

        return View("Result", new TicketPaymentResultViewModel
        {
            OrderId = order.Id,
            MovieName = order.Movie.Name,
            Quantity = order.Quantity,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency.ToUpperInvariant(),
            IsPaid = order.Status == TicketOrderStatuses.Paid
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Cancelled(int orderId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Challenge();
        var order = await unitOfWork.TicketOrders.Query()
            .SingleOrDefaultAsync(item => item.Id == orderId && item.CustomerAccountId == customerId, cancellationToken);
        if (order is null) return NotFound();
        if (order.Status == TicketOrderStatuses.Pending)
        {
            order.Status = TicketOrderStatuses.Cancelled;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        TempData["PurchaseError"] = "Payment was cancelled. You can try again.";
        return RedirectToAction("Details", "Movies", new { area = "Customer", id = order.MovieId });
    }
}
