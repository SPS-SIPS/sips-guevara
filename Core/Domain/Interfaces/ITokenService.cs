using System.Security.Claims;

namespace SPSS.Domain.Interfaces;

public interface ITokenService
{
    Task<LoginDto> GenerateToken(User user, IStorageBroker storage, CancellationToken cancellationToken);
    ClaimsPrincipal? ValidateJwt(string token);
    (Guid jit, int Id, string audience) ParseJwt(string token);
    ClaimsPrincipal? GetClaimsPrincipalFromToken(string token);

}