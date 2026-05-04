namespace SPSS.Domain.Models.DTOs;

public sealed class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public TimeSpan TokenLifetime { get; set; }
    public TimeSpan RefreshTokenLifetime { get; set; }
    public TimeSpan OTPTokenLifeTime { get; set; }
    public TimeSpan ResetTokenLifetime { get; set; }
    public TimeSpan TempTokenLifetime { get; set; }
    public TimeSpan ForceChangeTokenLifeTime { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool AddRolesToAccessToken { get; set; }
    public int TFATimeToleranceInSeconds { get; set; } = 30;
}
