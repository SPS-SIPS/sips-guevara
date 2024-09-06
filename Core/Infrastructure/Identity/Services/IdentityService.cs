namespace SPSS.Infrastructure.Identity.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly HttpContext _context;
    private readonly ITokenService _tokenService;
    private readonly static string policyName = "galawo_auth";
    public IdentityService(IHttpContextAccessor httpContextAccessor, ITokenService tokenService)
    {
        _context = httpContextAccessor.HttpContext!;
        _tokenService = tokenService;
        IsAuthenticated = Authorize();
        if (IsAuthenticated)
        {
            var user = _context.User;
            Id = int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : default;
            Name = user.FindFirstValue(JwtRegisteredClaimNames.Name) ?? "";
            Username = user.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "";
            StoreIds = GetStores();
            TenantId = int.TryParse(user.FindFirstValue("tenantId"), out var rn) ? rn : default;
        }
        else
        {
            Id = -1;
            Name = "Not Logged In";
            StoreIds = [-1];
            TenantId = -1;
            Username = "";
        }
    }

    public bool Authorize(string token = "")
    {
        if (_context == null || _context.Request == null)
        {
            return false;
        }
        if (_context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var accessToken = authHeader.ToString().Split(' ').Last();

            var principal = _tokenService.ValidateJwt(accessToken);

            if (principal != null)
            {
                var ticket = new AuthenticationTicket(principal, policyName);
                var result = AuthenticateResult.Success(ticket);
                if (result.Succeeded)
                {
                    _context.User = result.Principal;
                }
                return result.Succeeded;
            }
        }
        return false;
    }

    public int Id { get; }

    public bool IsAuthenticated { get; }

    public string Name { get; }
    public string Username { get; }

    public int[] StoreIds { get; }
    public int TenantId { get; }

    public bool IsSPSS { get; }

    public bool HasClaim(string type)
    {
        var user = _context?.User;
        return user != null && user.HasClaim("role", type);
    }

    public bool IsInRole(string role)
    {
        var user = _context?.User;
        return user != null && user.HasClaim("role", role);
    }

    public bool IsMaster()
    {
        var user = _context?.User;
        return user != null && user.HasClaim("realm", "master");
    }

    private int[] GetStores()
    {
        var user = _context?.User;
        if (user == null)
        {
            return [];
        }
        return user.FindAll("stores").Select(x => int.TryParse(x.Value, out var id) ? id : default).ToArray();
    }
}