using System.Security.Claims;
using CinemaProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CinemaProject.Services;

public sealed class CinemaUserClaimsPrincipalFactory(
    UserManager<CustomerAccount> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<CustomerAccount, IdentityRole<int>>(userManager, roleManager, options)
{
    private readonly IdentityOptions _identityOptions = options.Value;

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(CustomerAccount user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var nameType = _identityOptions.ClaimsIdentity.UserNameClaimType;
        if (identity.FindFirst(nameType) is { } userNameClaim)
            identity.RemoveClaim(userNameClaim);
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));
        if (!string.IsNullOrWhiteSpace(user.ProfileImage))
            identity.AddClaim(new Claim("profile_image", user.ProfileImage));
        return identity;
    }
}
