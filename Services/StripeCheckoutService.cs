using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CinemaProject.Models;

namespace CinemaProject.Services;

public sealed class StripeCheckoutService(HttpClient httpClient, IConfiguration configuration) : IStripeCheckoutService
{
    private const string ApiBase = "https://api.stripe.com/v1/checkout/sessions";

    public async Task<StripeCheckoutSession> CreateSessionAsync(
        TicketOrder order,
        string customerEmail,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var apiKey = GetSecretKey();
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiBase);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", $"ticket-order-{order.Id}");

        var amountMinor = checked((long)Math.Round(order.TotalAmount * 100m, 0, MidpointRounding.AwayFromZero));
        var fields = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["customer_email"] = customerEmail,
            ["client_reference_id"] = order.Id.ToString(CultureInfo.InvariantCulture),
            ["success_url"] = successUrl,
            ["cancel_url"] = cancelUrl,
            ["line_items[0][quantity]"] = order.Quantity.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][currency]"] = order.Currency.ToLowerInvariant(),
            ["line_items[0][price_data][unit_amount]"] = checked((long)Math.Round(order.UnitPrice * 100m, 0, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = $"{order.Movie.Name} — cinema ticket",
            ["metadata[order_id]"] = order.Id.ToString(CultureInfo.InvariantCulture)
        };

        if (amountMinor <= 0)
        {
            throw new InvalidOperationException("The order amount must be greater than zero.");
        }

        request.Content = new FormUrlEncodedContent(fields);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            string? stripeMessage = null;
            try
            {
                using var errorDocument = JsonDocument.Parse(responseBody);
                if (errorDocument.RootElement.TryGetProperty("error", out var error)
                    && error.TryGetProperty("message", out var message))
                    stripeMessage = message.GetString();
            }
            catch (JsonException)
            {
                // Keep the diagnostic useful even if Stripe returns a non-JSON error body.
            }

            var detail = string.IsNullOrWhiteSpace(stripeMessage) ? string.Empty : $": {stripeMessage}";
            throw new InvalidOperationException($"Stripe Checkout could not be created (HTTP {(int)response.StatusCode}){detail}.");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var id = root.GetProperty("id").GetString();
        var url = root.GetProperty("url").GetString();
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Stripe returned an incomplete Checkout Session.");
        }

        return new StripeCheckoutSession(id, url);
    }

    public async Task<StripeSessionStatus> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/{Uri.EscapeDataString(sessionId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetSecretKey());
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Stripe Checkout status could not be verified (HTTP {(int)response.StatusCode}).");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        string? orderId = null;
        if (root.TryGetProperty("metadata", out var metadata)
            && metadata.TryGetProperty("order_id", out var metadataOrderId))
        {
            orderId = metadataOrderId.GetString();
        }

        return new StripeSessionStatus(
            root.GetProperty("id").GetString() ?? string.Empty,
            root.TryGetProperty("client_reference_id", out var reference) ? reference.GetString() : null,
            orderId,
            root.TryGetProperty("payment_status", out var paymentStatus) ? paymentStatus.GetString() ?? "unpaid" : "unpaid",
            ReadStringOrId(root, "payment_intent"));
    }

    public bool VerifyWebhookSignature(string payload, string? signatureHeader)
    {
        var webhookSecret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        long? timestamp = null;
        var signatures = new List<string>();
        foreach (var part in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0) continue;
            var name = part[..separator];
            var value = part[(separator + 1)..];
            if (name == "t" && long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)) timestamp = parsed;
            if (name == "v1") signatures.Add(value);
        }

        if (timestamp is null || signatures.Count == 0) return false;
        var toleranceSeconds = configuration.GetValue<int?>("Stripe:WebhookToleranceSeconds") ?? 300;
        var age = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp.Value);
        if (age > toleranceSeconds) return false;

        var signedPayload = Encoding.UTF8.GetBytes($"{timestamp.Value}.{payload}");
        var secretBytes = Encoding.UTF8.GetBytes(webhookSecret);
        var expected = HMACSHA256.HashData(secretBytes, signedPayload);
        foreach (var signature in signatures)
        {
            byte[] supplied;
            try { supplied = Convert.FromHexString(signature); }
            catch (FormatException) { continue; }
            if (CryptographicOperations.FixedTimeEquals(expected, supplied)) return true;
        }

        return false;
    }

    private string GetSecretKey() => configuration["Stripe:SecretKey"] is { Length: > 0 } key
        ? key
        : throw new InvalidOperationException("Stripe:SecretKey is not configured. Set Stripe__SecretKey as an environment variable.");

    private static string? ReadStringOrId(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null) return null;
        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.TryGetProperty("id", out var id) ? id.GetString() : null;
    }
}
