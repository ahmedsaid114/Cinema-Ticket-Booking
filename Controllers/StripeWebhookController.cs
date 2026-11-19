using System.Globalization;
using System.Text.Json;
using CinemaProject.Models;
using CinemaProject.Repositories;
using CinemaProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskCinema.Controllers;

[ApiController]
[AllowAnonymous]
[Route("payments/stripe/webhook")]
public sealed class StripeWebhookController(
    IUnitOfWork unitOfWork,
    IStripeCheckoutService stripe,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        if (!stripe.VerifyWebhookSignature(payload, Request.Headers["Stripe-Signature"].FirstOrDefault()))
        {
            return BadRequest();
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var eventType = root.GetProperty("type").GetString();
            var session = root.GetProperty("data").GetProperty("object");
            var sessionId = session.GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(sessionId)) return Ok();

            var order = await unitOfWork.TicketOrders.Query()
                .SingleOrDefaultAsync(item => item.StripeCheckoutSessionId == sessionId, cancellationToken);
            if (order is null)
            {
                logger.LogInformation("Received Stripe event for unknown checkout session {SessionId}.", sessionId);
                return Ok();
            }

            var metadataOrderId = session.TryGetProperty("metadata", out var metadata)
                && metadata.TryGetProperty("order_id", out var orderIdElement)
                ? orderIdElement.GetString()
                : null;
            var clientReferenceId = session.TryGetProperty("client_reference_id", out var referenceElement)
                ? referenceElement.GetString()
                : null;
            if (metadataOrderId != order.Id.ToString(CultureInfo.InvariantCulture)
                || clientReferenceId != order.Id.ToString(CultureInfo.InvariantCulture))
            {
                return BadRequest();
            }

            if (eventType is "checkout.session.completed" or "checkout.session.async_payment_succeeded")
            {
                var paymentStatus = session.TryGetProperty("payment_status", out var paymentStatusElement)
                    ? paymentStatusElement.GetString()
                    : null;
                if (paymentStatus == "paid")
                {
                    order.Status = TicketOrderStatuses.Paid;
                    order.PaidUtc ??= DateTime.UtcNow;
                    order.StripePaymentIntentId = ReadStringOrId(session, "payment_intent");
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            else if (eventType == "checkout.session.expired" && order.Status == TicketOrderStatuses.Pending)
            {
                order.Status = TicketOrderStatuses.Expired;
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else if (eventType == "checkout.session.async_payment_failed" && order.Status == TicketOrderStatuses.Pending)
            {
                order.Status = TicketOrderStatuses.Cancelled;
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Ok();
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Stripe sent an invalid webhook payload.");
            return BadRequest();
        }
    }

    private static string? ReadStringOrId(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null) return null;
        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.TryGetProperty("id", out var id) ? id.GetString() : null;
    }
}
