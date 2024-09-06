namespace SPSS.Domain.Interfaces;

public interface IPasswordManager
{
    bool CheckPassword(User User, string password);
    string GeneratePasswordHash(User User, string password);
}