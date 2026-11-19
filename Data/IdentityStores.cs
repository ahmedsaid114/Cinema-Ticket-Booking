using System.Security.Claims;
using CinemaProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CinemaProject.Data;

public sealed class IdentityUserStore(ApplicationDbContext db, IdentityErrorDescriber errors) :
    IUserStore<CustomerAccount>,
    IQueryableUserStore<CustomerAccount>,
    IUserPasswordStore<CustomerAccount>,
    IUserEmailStore<CustomerAccount>,
    IUserRoleStore<CustomerAccount>,
    IUserSecurityStampStore<CustomerAccount>,
    IUserLockoutStore<CustomerAccount>,
    IUserTwoFactorStore<CustomerAccount>,
    IUserClaimStore<CustomerAccount>
{
    public IQueryable<CustomerAccount> Users => db.Users;

    public async Task<IdentityResult> CreateAsync(CustomerAccount user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(CustomerAccount user, CancellationToken cancellationToken)
    {
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        db.Entry(user).State = EntityState.Modified;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return IdentityResult.Failed(errors.ConcurrencyFailure());
        }
    }

    public async Task<IdentityResult> DeleteAsync(CustomerAccount user, CancellationToken cancellationToken)
    {
        db.Users.Remove(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return IdentityResult.Failed(errors.ConcurrencyFailure());
        }
    }

    public Task<CustomerAccount?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        int.TryParse(userId, out var id) ? db.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken) : Task.FromResult<CustomerAccount?>(null);

    public Task<CustomerAccount?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(user => user.NormalizedUserName == normalizedUserName, cancellationToken);

    public Task<string> GetUserIdAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
    public Task<string?> GetUserNameAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
    public Task SetUserNameAsync(CustomerAccount user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
    public Task SetNormalizedUserNameAsync(CustomerAccount user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
    public Task SetPasswordHashAsync(CustomerAccount user, string? passwordHash, CancellationToken cancellationToken) { user.PasswordHash = passwordHash; return Task.CompletedTask; }
    public Task<string?> GetPasswordHashAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);
    public Task<bool> HasPasswordAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash is not null);
    public Task SetEmailAsync(CustomerAccount user, string? email, CancellationToken cancellationToken) { user.Email = email; return Task.CompletedTask; }
    public Task<string?> GetEmailAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.Email);
    public Task<bool> GetEmailConfirmedAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.EmailConfirmed);
    public Task SetEmailConfirmedAsync(CustomerAccount user, bool confirmed, CancellationToken cancellationToken) { user.EmailConfirmed = confirmed; return Task.CompletedTask; }
    public Task<CustomerAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    public Task<string?> GetNormalizedEmailAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmail);
    public Task SetNormalizedEmailAsync(CustomerAccount user, string? normalizedEmail, CancellationToken cancellationToken) { user.NormalizedEmail = normalizedEmail; return Task.CompletedTask; }
    public Task SetSecurityStampAsync(CustomerAccount user, string stamp, CancellationToken cancellationToken) { user.SecurityStamp = stamp; return Task.CompletedTask; }
    public Task<string?> GetSecurityStampAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.SecurityStamp);

    public async Task AddToRoleAsync(CustomerAccount user, string normalizedRoleName, CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleOrDefaultAsync(item => item.NormalizedName == normalizedRoleName, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{normalizedRoleName}' does not exist.");
        if (!await db.UserRoles.AnyAsync(link => link.UserId == user.Id && link.RoleId == role.Id, cancellationToken))
            db.UserRoles.Add(new IdentityUserRole<int> { UserId = user.Id, RoleId = role.Id });
    }

    public async Task RemoveFromRoleAsync(CustomerAccount user, string normalizedRoleName, CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleOrDefaultAsync(item => item.NormalizedName == normalizedRoleName, cancellationToken);
        if (role is null) return;
        var link = await db.UserRoles.SingleOrDefaultAsync(item => item.UserId == user.Id && item.RoleId == role.Id, cancellationToken);
        if (link is not null) db.UserRoles.Remove(link);
    }

    public async Task<IList<string>> GetRolesAsync(CustomerAccount user, CancellationToken cancellationToken)
    {
        return await (from link in db.UserRoles
                      join role in db.Roles on link.RoleId equals role.Id
                      where link.UserId == user.Id
                      select role.Name!).ToListAsync(cancellationToken);
    }

    public async Task<bool> IsInRoleAsync(CustomerAccount user, string normalizedRoleName, CancellationToken cancellationToken)
    {
        return await (from link in db.UserRoles
                      join role in db.Roles on link.RoleId equals role.Id
                      where link.UserId == user.Id && role.NormalizedName == normalizedRoleName
                      select link.UserId).AnyAsync(cancellationToken);
    }

    public async Task<IList<CustomerAccount>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        return await (from link in db.UserRoles
                      join role in db.Roles on link.RoleId equals role.Id
                      join user in db.Users on link.UserId equals user.Id
                      where role.NormalizedName == normalizedRoleName
                      select user).ToListAsync(cancellationToken);
    }

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.LockoutEnd);
    public Task SetLockoutEndDateAsync(CustomerAccount user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken) { user.LockoutEnd = lockoutEnd; return Task.CompletedTask; }
    public Task<int> IncrementAccessFailedCountAsync(CustomerAccount user, CancellationToken cancellationToken) { user.AccessFailedCount++; return Task.FromResult(user.AccessFailedCount); }
    public Task ResetAccessFailedCountAsync(CustomerAccount user, CancellationToken cancellationToken) { user.AccessFailedCount = 0; return Task.CompletedTask; }
    public Task<int> GetAccessFailedCountAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.AccessFailedCount);
    public Task<bool> GetLockoutEnabledAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.LockoutEnabled);
    public Task SetLockoutEnabledAsync(CustomerAccount user, bool enabled, CancellationToken cancellationToken) { user.LockoutEnabled = enabled; return Task.CompletedTask; }
    public Task<bool> GetTwoFactorEnabledAsync(CustomerAccount user, CancellationToken cancellationToken) => Task.FromResult(user.TwoFactorEnabled);
    public Task SetTwoFactorEnabledAsync(CustomerAccount user, bool enabled, CancellationToken cancellationToken) { user.TwoFactorEnabled = enabled; return Task.CompletedTask; }

    public async Task<IList<Claim>> GetClaimsAsync(CustomerAccount user, CancellationToken cancellationToken) =>
        await db.UserClaims.Where(claim => claim.UserId == user.Id).Select(claim => claim.ToClaim()).ToListAsync(cancellationToken);

    public Task AddClaimsAsync(CustomerAccount user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        db.UserClaims.AddRange(claims.Select(claim => { var item = new IdentityUserClaim<int> { UserId = user.Id }; item.InitializeFromClaim(claim); return item; }));
        return Task.CompletedTask;
    }

    public async Task ReplaceClaimAsync(CustomerAccount user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        await RemoveClaimsAsync(user, [claim], cancellationToken);
        await AddClaimsAsync(user, [newClaim], cancellationToken);
    }

    public async Task RemoveClaimsAsync(CustomerAccount user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        var existing = await db.UserClaims.Where(item => item.UserId == user.Id).ToListAsync(cancellationToken);
        var claimList = claims.ToList();
        db.UserClaims.RemoveRange(existing.Where(item => claimList.Any(claim => item.ClaimType == claim.Type && item.ClaimValue == claim.Value)));
    }

    public async Task<IList<CustomerAccount>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        var ids = db.UserClaims.Where(item => item.ClaimType == claim.Type && item.ClaimValue == claim.Value).Select(item => item.UserId);
        return await db.Users.Where(user => ids.Contains(user.Id)).ToListAsync(cancellationToken);
    }

    public void Dispose() { }
}

public sealed class IdentityRoleStore(ApplicationDbContext db, IdentityErrorDescriber errors) : IRoleStore<IdentityRole<int>>
{
    public async Task<IdentityResult> CreateAsync(IdentityRole<int> role, CancellationToken cancellationToken)
    {
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(IdentityRole<int> role, CancellationToken cancellationToken)
    {
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        db.Entry(role).State = EntityState.Modified;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return IdentityResult.Failed(errors.ConcurrencyFailure());
        }
    }

    public async Task<IdentityResult> DeleteAsync(IdentityRole<int> role, CancellationToken cancellationToken)
    {
        db.Roles.Remove(role);
        try { await db.SaveChangesAsync(cancellationToken); return IdentityResult.Success; }
        catch (DbUpdateConcurrencyException) { return IdentityResult.Failed(errors.ConcurrencyFailure()); }
    }

    public Task<string> GetRoleIdAsync(IdentityRole<int> role, CancellationToken cancellationToken) => Task.FromResult(role.Id.ToString());
    public Task<string?> GetRoleNameAsync(IdentityRole<int> role, CancellationToken cancellationToken) => Task.FromResult(role.Name);
    public Task SetRoleNameAsync(IdentityRole<int> role, string? roleName, CancellationToken cancellationToken) { role.Name = roleName; return Task.CompletedTask; }
    public Task<string?> GetNormalizedRoleNameAsync(IdentityRole<int> role, CancellationToken cancellationToken) => Task.FromResult(role.NormalizedName);
    public Task SetNormalizedRoleNameAsync(IdentityRole<int> role, string? normalizedName, CancellationToken cancellationToken) { role.NormalizedName = normalizedName; return Task.CompletedTask; }
    public Task<IdentityRole<int>?> FindByIdAsync(string roleId, CancellationToken cancellationToken) =>
        int.TryParse(roleId, out var id) ? db.Roles.SingleOrDefaultAsync(role => role.Id == id, cancellationToken) : Task.FromResult<IdentityRole<int>?>(null);
    public Task<IdentityRole<int>?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken) =>
        db.Roles.SingleOrDefaultAsync(role => role.NormalizedName == normalizedRoleName, cancellationToken);
    public void Dispose() { }
}
