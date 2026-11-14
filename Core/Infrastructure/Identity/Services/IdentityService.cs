namespace SPSS.Infrastructure.Identity.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly HttpContext _context;
    private readonly ITokenService _tokenService;
    private readonly List<ApiClient> _apiClients = [];
    private readonly static string policyName = "galawo_auth";
    public IdentityService(IHttpContextAccessor httpContextAccessor, ITokenService tokenService, List<ApiClient> apiClients)
    {
        _context = httpContextAccessor.HttpContext!;
        _tokenService = tokenService;
        _apiClients = apiClients;
        IsAuthenticated = Authorize();
        if (IsAuthenticated)
        {
            var user = _context.User;
            Id = int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : default;
            Name = user.FindFirstValue(JwtRegisteredClaimNames.Name) ?? "";
            Username = user.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "";
            TenantId = user.FindFirstValue("tenantId") ?? "";
        }
        else
        {
            Id = -1;
            Name = "Not Logged In";
            TenantId = "";
            Username = "";
        }
    }

    public bool Authorize(string token = "")
    {
        if (_context == null || _context.Request == null)
        {
            return false;
        }

        // Bearer authentication (including live session validation) is performed by middleware.
        if (_context.Request.Headers.ContainsKey("Authorization"))
        {
            return _context.User.Identity?.IsAuthenticated == true;
        }

        // API Key & Secret authentication
        if (_context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader) &&
            _context.Request.Headers.TryGetValue("X-API-Secret", out var apiSecretHeader))
        {
            var apiKey = apiKeyHeader.ToString();
            var apiSecret = apiSecretHeader.ToString();
            var client = _apiClients.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ClientId) && !string.IsNullOrWhiteSpace(x.ClientSecret) &&
                SecretEquals(x.ClientId, apiKey) && SecretEquals(x.ClientSecret, apiSecret));
            if (client != null)
            {
                var claims = new[]
                {
                    new System.Security.Claims.Claim("api_client_id", client.ClientId.ToString()),
                    new System.Security.Claims.Claim("api_client_name", client.ClientName ?? "")
                };
                var identity = new ClaimsIdentity(claims, "ApiKey");
                var principal = new ClaimsPrincipal(identity);
                foreach (var item in client.Scopes)
                {
                    claims =
                    [
                        new System.Security.Claims.Claim("role", item)
                    ];
                    identity.AddClaims(claims);
                }
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

    private static bool SecretEquals(string expected, string actual)
        => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(expected)),
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(actual)));

    public int Id { get; }

    public bool IsAuthenticated { get; }

    public string Name { get; }
    public string Username { get; }
    public string TenantId { get; }

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