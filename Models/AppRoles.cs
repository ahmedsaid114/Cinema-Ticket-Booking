namespace CinemaProject.Models;

public static class AppRoles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    public static readonly string[] All = [Customer, Admin, SuperAdmin];
    public const string AdminAccess = Admin + "," + SuperAdmin;
}

public sealed class SuperAdminDashboardViewModel
{
    public int TotalUsers { get; init; }
    public int Customers { get; init; }
    public int Admins { get; init; }
    public int SuperAdmins { get; init; }
}

public sealed class UserRoleRowViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public sealed class EditUserRoleViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string SelectedRole { get; set; } = AppRoles.Customer;
}

public sealed class CustomerDashboardViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<CustomerOrderRowViewModel> Orders { get; init; } = [];
}

public sealed class CustomerOrderRowViewModel
{
    public int OrderId { get; init; }
    public string MovieName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
}
