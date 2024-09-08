namespace SPSS.Infrastructure.Identity.Services;
public sealed class FakeUserService : IIdentityService
{
    public FakeUserService()
    {
        Id = -1;
        Name = "";
        Username = "";
    }
    public int Id { get; }

    public bool IsAuthenticated { get; }

    public string Name { get; }
    public string Username { get; }

    public string TenantId => "";

    public bool HasClaim(string type) => false;

    public bool IsInRole(string role) => false;

    public bool IsMaster() => false;
    public bool Authorize(string token) => false;
}