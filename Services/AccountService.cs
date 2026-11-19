using CinemaProject.Models;
using Microsoft.AspNetCore.Identity;

namespace CinemaProject.Services;

public sealed class AccountService(
    UserManager<CustomerAccount> users,
    RoleManager<IdentityRole<int>> roles) : IAccountService
{
    public async Task<AccountRegistrationResult> RegisterAsync(
        string name,
        string email,
        string password,
        string? profileImage,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedEmail = email.Trim();
        if (await users.FindByEmailAsync(normalizedEmail) is not null)
        {
            return AccountRegistrationResult.EmailAlreadyRegistered;
        }

        var user = new CustomerAccount
        {
            Name = name.Trim(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            ProfileImage = profileImage,
            CreatedUtc = DateTime.UtcNow
        };

        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return AccountRegistrationResult.Failed;
        }

        if (!await roles.RoleExistsAsync(AppRoles.Customer))
        {
            await users.DeleteAsync(user);
            return AccountRegistrationResult.Failed;
        }

        var assigned = await users.AddToRoleAsync(user, AppRoles.Customer);
        if (!assigned.Succeeded)
        {
            await users.DeleteAsync(user);
            return AccountRegistrationResult.Failed;
        }

        return AccountRegistrationResult.Created;
    }

    public async Task<PasswordResetLinkData?> CreatePasswordResetLinkAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return null;
        }

        var token = await users.GeneratePasswordResetTokenAsync(user);
        return new PasswordResetLinkData(user.Id, user.Email!, user.Name, token);
    }

    public async Task<bool> ResetPasswordAsync(
        int userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        var result = await users.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded;
    }
}
