using Microsoft.EntityFrameworkCore;
using SPSS.Infrastructure.Persistence;

namespace SPSS.Infrastructure.Identity.Services;

public static class SessionValidation
{
    // This context deliberately has no audit interceptors: authentication must finish
    // before request-scoped identity/auditing services are constructed.
    public static async Task<bool> ValidateAsync(StorageBroker storage, ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) ||
            !Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Jti), out var jti)) return false;

        var user = await storage.Users.AsNoTracking().Include(x => x.Participant)
            .Include(x => x.Roles).ThenInclude(x => x.Role).ThenInclude(x => x.RoleClaims)
            .SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null || !user.Active || user.DeletedOn != null ||
            string.IsNullOrEmpty(user.SecurityStamp) ||
            principal.FindFirstValue("security_stamp") != user.SecurityStamp) return false;

        if (user.Participant != null && (user.Participant.DeletedOn != null || string.IsNullOrWhiteSpace(user.Participant.BicNo))) return false;
        var now = DateTimeOffset.UtcNow;
        var session = await storage.Tokens.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.Jti == jti &&
                x.TokenType == SPSS.Domain.Enums.TokenType.Refresh && !x.Invalidated && !x.Used && x.ExpiresOn > now, ct);
        if (session is null) return false;

        // Use current permissions and tenant membership, rather than a stale JWT snapshot.
        var identity = (ClaimsIdentity)principal.Identity!;
        foreach (var claim in identity.FindAll("role").Concat(identity.FindAll("tenantId")).ToArray())
            identity.RemoveClaim(claim);
        identity.AddClaim(new System.Security.Claims.Claim("tenantId", user.Participant?.BicNo ?? ""));
        if (!user.SecurityStamp.StartsWith("bootstrap:", StringComparison.Ordinal))
            foreach (var permission in user.Roles.Where(x => x.Role.Active).SelectMany(x => x.Role.RoleClaims).Select(x => x.Type).Distinct())
                identity.AddClaim(new System.Security.Claims.Claim("role", permission));
        return true;
    }
}
