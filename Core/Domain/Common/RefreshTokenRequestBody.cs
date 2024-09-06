namespace SPSS.Domain.Common;

public sealed record RefreshTokenRequestBody(string? GrantType, string? RefreshToken);
