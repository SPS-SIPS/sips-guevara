namespace SPSS.Domain.Common;

public sealed class TokenRequestBody
{
    public string? GrantType { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
