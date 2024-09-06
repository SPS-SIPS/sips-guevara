namespace SPSS.Domain.Common;

public sealed class TokenResponseBody
{
    public string? AccessToken { get; set; }
    public long? ExpiresIn { get; set; }
    public long? RefreshExpiresIn { get; set; }
    public string? TokenType { get; set; }
    public string? RefreshToken { get; set; }
}
