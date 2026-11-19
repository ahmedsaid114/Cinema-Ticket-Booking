using Microsoft.AspNetCore.Identity;

namespace CinemaProject.Models;

public sealed class CustomerAccount : IdentityUser<int>
{
    public string Name { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }

    public DateTime CreatedUtc { get; set; }

    public ICollection<TicketOrder> TicketOrders { get; set; } = new List<TicketOrder>();
}
