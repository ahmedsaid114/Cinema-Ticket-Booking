using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaProject.Models;

public static class TicketOrderStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
}

public sealed class TicketOrder
{
    public int Id { get; set; }

    public int CustomerAccountId { get; set; }
    public CustomerAccount CustomerAccount { get; set; } = null!;

    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    [Range(1, 10)]
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Required, StringLength(3)]
    public string Currency { get; set; } = "egp";

    [Required, StringLength(20)]
    public string Status { get; set; } = TicketOrderStatuses.Pending;

    [StringLength(255)]
    public string? StripeCheckoutSessionId { get; set; }

    [StringLength(255)]
    public string? StripePaymentIntentId { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? PaidUtc { get; set; }
}
