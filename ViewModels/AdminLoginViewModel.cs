using System.ComponentModel.DataAnnotations;

namespace CinemaProject.ViewModels;

public sealed class AdminLoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
