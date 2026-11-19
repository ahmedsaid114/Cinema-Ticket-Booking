namespace CinemaProject.ViewModels;

// These are presentation models. Map your EF entities to them in the controller.
public sealed class CinemaHomeViewModel
{
    public IReadOnlyList<MovieCardViewModel> Movies { get; init; } = [];
    public IReadOnlyList<CategoryViewModel> Categories { get; init; } = [];
}

public sealed class MovieListViewModel
{
    public IReadOnlyList<MovieCardViewModel> Movies { get; init; } = [];
    public IReadOnlyList<CategoryViewModel> Categories { get; init; } = [];
    public int? CategoryId { get; init; }
    public string? Search { get; init; }
}

public sealed class MovieDetailsViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public DateTime ReleaseDate { get; init; }
    public string? MainImg { get; init; }
    public string CategoryName { get; init; } = "";
    public string CinemaName { get; init; } = "";
    public string? CinemaAddress { get; init; }
    public IReadOnlyList<string> Actors { get; init; } = [];
    public IReadOnlyList<string> Images { get; init; } = [];
}

public sealed class MovieCardViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public DateTime ReleaseDate { get; init; }
    public string? MainImg { get; init; }
    public string CategoryName { get; init; } = "";
    public string CinemaName { get; init; } = "";
}

public sealed class CategoryViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

public sealed class AdminDashboardViewModel
{
    public int PaidOrders { get; init; }
    public int TicketsSold { get; init; }
    public int PendingOrders { get; init; }
    public decimal GrossRevenue { get; init; }
    public IReadOnlyList<AdminOrderRowViewModel> RecentOrders { get; init; } = [];
}

public sealed class AdminOrderRowViewModel
{
    public int OrderId { get; init; }
    public string CustomerName { get; init; } = "";
    public string MovieName { get; init; } = "";
    public int Quantity { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "EGP";
    public string Status { get; init; } = "";
    public DateTime CreatedUtc { get; init; }
}

public sealed class TicketPaymentResultViewModel
{
    public int OrderId { get; init; }
    public string MovieName { get; init; } = "";
    public int Quantity { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "EGP";
    public bool IsPaid { get; init; }
}


