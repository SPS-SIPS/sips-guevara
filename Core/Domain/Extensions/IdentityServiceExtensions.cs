namespace SPSS.Domain.Extensions;
public static class IdentityServiceExtensions
{
    public static async ValueTask<int> InvalidateRefreshTokens(this IIdentityService identity, IStorageBroker storage, CancellationToken cancellationToken)
    {
        var tokens = await storage.Tokens.Where(x => x.UserId.Equals(identity.Id) && !x.Used && !x.Invalidated)
        .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Invalidated = true;
        }

        return await storage.SaveChangesAsync(cancellationToken);
    }
}
