using Google.Authenticator;
using System.Security.Cryptography;


namespace SPSS.Infrastructure.Identity.Services;

public class TFAService(JwtSettings jwtSettings) : ITFAService
{
    private readonly JwtSettings _jwtSettings = jwtSettings;

    public string GenerateSecretKey(string email)
    {
        var tfa = new TwoFactorAuthenticator();
        var setupInfo = tfa.GenerateSetupCode(_jwtSettings.Issuer, email, GenerateRandomSecret(), false, 3);
        return setupInfo.ManualEntryKey;
    }

    private static string GenerateRandomSecret()
    {
        var randomBytes = new byte[10];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Base32Encoding.ToString(randomBytes);
    }

    public string GetQRCodeUri(string email, string secret)
    {
        var tfa = new TwoFactorAuthenticator();
        var setupInfo = tfa.GenerateSetupCode(_jwtSettings.Issuer, email, secret, false, 3);
        return setupInfo.QrCodeSetupImageUrl;
    }

    public bool Verify(string secret, string code)
    {
        var tfa = new TwoFactorAuthenticator();
        return tfa.ValidateTwoFactorPIN(secret, code, TimeSpan.FromSeconds(_jwtSettings.TFATimeToleranceInSeconds));
    }
}


