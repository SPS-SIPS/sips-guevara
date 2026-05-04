using SPSS.Domain.Models.Identity;

namespace SPSS.App.Common;
public static class Helpers
{
    public static string GeneratePassword()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringChars = new char[8];
        var random = new Random();
        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }

    public static async Task<Response<string>> ResetPasswordHelper(User user, IPasswordManager _passwordManager, IStorageBroker _storage, IIdentityService _identity, AppSettings _settings, CancellationToken cancellationToken)
    {

        user.SecurityStamp = Guid.NewGuid().ToString();
        var password = GeneratePassword();
        user.PasswordHash = _passwordManager.GeneratePasswordHash(user, password);

        await _storage.SaveChangesAsync(cancellationToken);
        await _identity.InvalidateRefreshTokens(_storage, cancellationToken);

        return new Response<string>(password);
    }

    public static string GenerateAccountCode(string parentCode, int level, int count)
    {
        if (level < 1 || level > 4)
        {
            throw new ArgumentException("Level must be between 1 and 4.");
        }

        int code = int.Parse(parentCode);
        int lc = count + 1;

        // 14 is our inventory account code
        bool isStartingWith14 = parentCode.StartsWith("14");

        int increment = level switch
        {
            1 => lc * 10000,
            2 => isStartingWith14 ? lc * 1000000 : lc * 1000,
            3 => isStartingWith14 ? lc * 10000 : lc,
            4 => lc,
            _ => throw new ArgumentException("Invalid level."),
        };

        if (isStartingWith14 && parentCode.Length < 9)
        {
            parentCode = parentCode.PadRight(9, '0');
            code = int.Parse(parentCode);
        }

        string generatedCode = (code + increment).ToString();

        return generatedCode;
    }
}
