namespace SPSS.Domain.Interfaces;

public interface IPasswordHash
{
    string HashPassword(User user, string password);
    PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword);
}
