namespace SPSS.Domain.Interfaces;

public interface IIdentityService
{
    int Id { get; }
    int[] StoreIds { get; }
    int TenantId { get; }
    bool IsAuthenticated { get; }
    string Name { get; }
    string Username { get; }
    bool IsInRole(string role);
    bool HasClaim(string type);
    bool Authorize(string token);
    bool IsMaster();
}
