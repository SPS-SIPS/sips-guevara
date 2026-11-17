namespace SPSS.App.Security;

public static class PermissionDelegation
{
    private static readonly HashSet<string> KnownPermissions = typeof(KnownClaims)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (string)field.GetValue(null)!).ToHashSet(StringComparer.Ordinal);

    // Managing a role/account is not authority to acquire or grant additional permissions.
    public static void EnsureWithinAuthority(IIdentityService caller, IEnumerable<string> permissions)
    {
        if (!caller.IsAuthenticated || !string.IsNullOrWhiteSpace(caller.TenantId) ||
            permissions.Any(permission => !KnownPermissions.Contains(permission) || !caller.IsInRole(permission)))
            throw new ForbiddenAccessException("You cannot manage or grant permissions outside your own authority.");
    }
}
