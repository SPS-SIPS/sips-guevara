using SPSS.Domain.Enums;

namespace SPSS.Infrastructure.Services;

public sealed class PasswordManager(IPasswordHash hash) : IPasswordManager
{
    public readonly IPasswordHash _hash = hash;

    public string GeneratePasswordHash(User user, string password)
    {
        return _hash.HashPassword(user, password);
    }

    private PasswordVerificationResult VerifyPasswordHash(User user, string providedPassword)
    {
        return _hash.VerifyHashedPassword(user, user.PasswordHash, providedPassword);
    }

    public bool CheckPassword(User user, string password)
    {
        if (user == null)
        {
            return false;
        }
        var result = VerifyPasswordHash(user, password);

        var success = result != PasswordVerificationResult.Failed;
        if (!success)
        {
            // we can log this bad request if we want to.
        }
        return success;
    }
}