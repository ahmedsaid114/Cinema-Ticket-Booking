using CinemaProject.Models;
using Microsoft.AspNetCore.Identity;

namespace CinemaProject.Services;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        bool syncBootstrapPasswords = false)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var roleName in AppRoles.All)
        {
            if (!await roles.RoleExistsAsync(roleName))
            {
                var result = await roles.CreateAsync(new IdentityRole<int>(roleName));
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Could not create the {roleName} role: {string.Join("; ", result.Errors.Select(error => error.Description))}");
            }
        }

        var users = services.GetRequiredService<UserManager<CustomerAccount>>();
        var superAdminEmail = configuration["SuperAdmin:Email"]?.Trim();
        var adminEmail = configuration["Admin:Email"]?.Trim();
        if (!string.IsNullOrWhiteSpace(superAdminEmail)
            && string.Equals(superAdminEmail, adminEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SuperAdmin:Email and Admin:Email must identify different accounts.");

        await EnsureBootstrapUserAsync(users, configuration, "SuperAdmin", AppRoles.SuperAdmin, "Cinema Super Admin", syncBootstrapPasswords);
        await EnsureBootstrapUserAsync(users, configuration, "Admin", AppRoles.Admin, "Cinema Admin", syncBootstrapPasswords);

        if ((await users.GetUsersInRoleAsync(AppRoles.SuperAdmin)).Count == 0)
        {
            throw new InvalidOperationException(
                "No SuperAdmin account exists. Set SuperAdmin:Email and SuperAdmin:Password in User Secrets or the production secret store, then restart the app.");
        }

        foreach (var user in users.Users.ToList())
        {
            if ((await users.GetRolesAsync(user)).Count == 0)
            {
                var assigned = await users.AddToRoleAsync(user, AppRoles.Customer);
                if (!assigned.Succeeded)
                    throw new InvalidOperationException($"Could not assign Customer access to {user.Email}: {string.Join("; ", assigned.Errors.Select(error => error.Description))}");
            }
        }
    }

    private static async Task EnsureBootstrapUserAsync(
        UserManager<CustomerAccount> users,
        IConfiguration configuration,
        string section,
        string role,
        string displayName,
        bool syncBootstrapPasswords)
    {
        var email = configuration[$"{section}:Email"]?.Trim();
        var password = configuration[$"{section}:Password"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password)) return;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException($"Configure both {section}:Email and {section}:Password in User Secrets or the environment.");

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new CustomerAccount
            {
                Name = displayName,
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                CreatedUtc = DateTime.UtcNow
            };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
                throw new InvalidOperationException($"Could not create the configured {role} account: {string.Join("; ", created.Errors.Select(error => error.Description))}");
        }
        else if (syncBootstrapPasswords && !await users.CheckPasswordAsync(user, password))
        {
            foreach (var validator in users.PasswordValidators)
            {
                var validation = await validator.ValidateAsync(users, user, password);
                if (!validation.Succeeded)
                    throw new InvalidOperationException($"The configured {role} password is invalid: {string.Join("; ", validation.Errors.Select(error => error.Description))}");
            }

            user.PasswordHash = users.PasswordHasher.HashPassword(user, password);
            user.SecurityStamp = Guid.NewGuid().ToString();
            var reset = await users.UpdateAsync(user);
            if (!reset.Succeeded)
                throw new InvalidOperationException($"Could not reset the configured {role} password: {string.Join("; ", reset.Errors.Select(error => error.Description))}");

            await users.ResetAccessFailedCountAsync(user);
            await users.SetLockoutEndDateAsync(user, null);
        }

        var currentRoles = await users.GetRolesAsync(user);
        if (currentRoles.Count > 1 || !currentRoles.Contains(role, StringComparer.Ordinal))
        {
            if (currentRoles.Count > 0)
            {
                var removed = await users.RemoveFromRolesAsync(user, currentRoles);
                if (!removed.Succeeded)
                    throw new InvalidOperationException($"Could not remove the existing roles from the configured {role} account: {string.Join("; ", removed.Errors.Select(error => error.Description))}");
            }
            var assigned = await users.AddToRoleAsync(user, role);
            if (!assigned.Succeeded)
                throw new InvalidOperationException($"Could not assign the {role} role: {string.Join("; ", assigned.Errors.Select(error => error.Description))}");
        }
    }
}
