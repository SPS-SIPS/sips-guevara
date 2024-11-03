using System.Text;
using Microsoft.IdentityModel.Tokens;
using SPSS.Domain.Enums;
using SPSS.Domain.Exceptions;
using UAParser;

namespace SPSS.Infrastructure.Identity.Services;

public sealed class TokenService(JwtSettings settings, TokenValidationParameters validationParams, IHttpContextAccessor httpContextAccessor, IRolesCache roles) : ITokenService
{
    private readonly JwtSettings _settings = settings;
    private readonly TokenValidationParameters _validationParams = validationParams;
    private readonly HttpContext _httpContext = httpContextAccessor.HttpContext!;
    private readonly IRolesCache _roles = roles;

    public async Task<LoginDto> GenerateToken(User user, IStorageBroker storage, CancellationToken cancellationToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_settings.Secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.UniqueName, user.Phone),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Iss, _settings.Issuer),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Aud, _settings.Audience),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Name, user.Username),
                new System.Security.Claims.Claim("tenantId", user.Participant?.Id ?? ""),
            ]),
            Expires = DateTime.UtcNow.Add(_settings.TokenLifetime),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var userRolesIds = await storage.UserRoles
            .Where(x => x.UserId.Equals(user.Id))
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        if (userRolesIds != null)
        {
            var roles = await _roles.GetRoles([.. userRolesIds]);
            foreach (var role in roles)
            {
                foreach (var claim in role.Claims)
                {
                    tokenDescriptor.Subject.AddClaim(new System.Security.Claims.Claim("role", claim.Type));
                }
            }
        }

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenExpiresIn = tokenDescriptor.Expires?.Subtract(DateTime.UtcNow) ?? TimeSpan.Zero;
        var refreshTokenExpiresIn = DateTime.UtcNow.Add(_settings.RefreshTokenLifetime);
        var refreshTokenId = Guid.Empty;

        var (ip, device, browser, os) = GetClientInfo();
        var refreshToken = new Token
        {
            Jti = Guid.Parse(token.Id),
            UserId = user.Id,
            CreatedOn = DateTime.UtcNow,
            ExpiresOn = refreshTokenExpiresIn,
            TokenType = TokenType.Refresh,
            IPAddress = ip,
            UserAgent = $"{browser}",
            OS = os
        };
        await storage.Tokens.AddAsync(refreshToken, cancellationToken);
        refreshTokenId = refreshToken.Id;

        int totalSecondsBeforeRefreshTokenExpires = (int)refreshTokenExpiresIn.Subtract(DateTime.UtcNow).TotalSeconds;

        await storage.SaveChangesAsync(cancellationToken);

        return new LoginDto(tokenHandler.WriteToken(token), refreshTokenId, ExpiresIn: tokenExpiresIn.Seconds, RefreshExpiresIn: totalSecondsBeforeRefreshTokenExpires);
    }
    private (string ip, string device, string browser, string os) GetClientInfo()
    {
        // get ip address of request from header, when running inside docker container behind nginx proxy
        var ip = _httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ip))
        {
            ip = _httpContext.Connection.RemoteIpAddress?.ToString();
        }
        if (ip != null && ip.Contains(':'))
        {
            ip = _httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }
        var userAgent = _httpContext.Request.Headers["User-Agent"].ToString();
        var uaParser = Parser.GetDefault();
        var clientInfo = uaParser.Parse(userAgent);
        var device = clientInfo.Device.ToString();
        var browser = clientInfo.UA.ToString();
        var os = clientInfo.OS.ToString();
        return (ip ?? "", device, browser, os);
    }
    public (Guid jit, int Id, string audience) ParseJwt(string token)
    {
        var claimsPrincipal = GetClaimsPrincipalFromToken(token) ?? throw new BadRequestException("Invalid Token");

        var expiryDateUnix = long.Parse(claimsPrincipal.Claims.Single(x => x.Type.Equals(JwtRegisteredClaimNames.Exp)).Value);
        var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(expiryDateUnix);
        var jti = Guid.TryParse(claimsPrincipal.Claims.Single(x => x.Type.Equals(JwtRegisteredClaimNames.Jti)).Value, out Guid id) ? id : Guid.Empty;
        var userId = int.TryParse(claimsPrincipal.Claims.Single(x => x.Type.Equals(JwtRegisteredClaimNames.Sub)).Value, out int sub) ? sub : -1;
        var audience = claimsPrincipal.Claims.Single(x => x.Type.Equals(JwtRegisteredClaimNames.Aud)).Value;
        if (_settings.Audience != audience)
        {
            throw new BadRequestException("Invalid Token");
        }
        return (jti, userId, audience);
    }

    public ClaimsPrincipal? ValidateJwt(string token)
    {
        var claimsPrincipal = GetClaimsPrincipalFromToken(token);
        if (claimsPrincipal == null)
        {
            return null;
        }

        var expiryDateUnix = long.Parse(claimsPrincipal.Claims.Single(x => x.Type.Equals(JwtRegisteredClaimNames.Exp)).Value);

        var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        .AddSeconds(expiryDateUnix);

        if (expiryDateTimeUtc < DateTime.UtcNow)
        {
            return null;
        }
        return claimsPrincipal;
    }
    public ClaimsPrincipal? GetClaimsPrincipalFromToken(string token)
    {
        var @params = _validationParams;
        @params.ValidateLifetime = false;
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, @params, out var validatedToken);
            if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
            {
                return null;
            }
            return principal;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
    {
        return (validatedToken is JwtSecurityToken JwtSecurityToken) && JwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
    }

    public async Task<(int otp, Guid refreshTokenId)> GenerateOTP(User user, IStorageBroker storage, CancellationToken cancellationToken)
    {
        var otp = new Random().Next(1000, 9999);
        var (ip, _, browser, os) = GetClientInfo();

        var refreshToken = new Token
        {
            Jti = Guid.NewGuid(),
            UserId = user.Id,
            CreatedOn = DateTime.UtcNow,
            ExpiresOn = DateTime.UtcNow.Add(_settings.OTPTokenLifeTime),
            TokenType = TokenType.OTP,
            Value = otp,
            IPAddress = ip,
            UserAgent = $"{browser}",
            OS = os
        };
        await storage.Tokens.AddAsync(refreshToken, cancellationToken);
        await storage.SaveChangesAsync(cancellationToken);

        return (otp, refreshToken.Id);
    }

    public async Task<(int otp, Guid refreshTokenId)> GenerateOTP(Token token, IStorageBroker storage, CancellationToken cancellationToken)
    {
        var otp = new Random().Next(1000, 9999);
        var (ip, _, browser, os) = GetClientInfo();

        var refreshToken = new Token
        {
            Jti = Guid.NewGuid(),
            UserId = token.UserId,
            CreatedOn = DateTime.UtcNow,
            ExpiresOn = DateTime.UtcNow.Add(_settings.OTPTokenLifeTime),
            TokenType = TokenType.OTP,
            Value = otp,
            IPAddress = ip,
            UserAgent = $"{browser}",
            OS = os
        };
        await storage.Tokens.AddAsync(refreshToken, cancellationToken);
        await storage.SaveChangesAsync(cancellationToken);

        return (otp, refreshToken.Id);
    }
}
