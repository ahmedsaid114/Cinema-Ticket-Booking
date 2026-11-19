using CinemaProject.Models;

namespace CinemaProject.Services;

public sealed record StripeCheckoutSession(string Id, string Url);
public sealed record StripeSessionStatus(string Id, string? ClientReferenceId, string? OrderId, string PaymentStatus, string? PaymentIntentId);

public interface IStripeCheckoutService
{
    Task<StripeCheckoutSession> CreateSessionAsync(
        TicketOrder order,
        string customerEmail,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    Task<StripeSessionStatus> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    bool VerifyWebhookSignature(string payload, string? signatureHeader);
}
