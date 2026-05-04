namespace SPSS.Domain.Interfaces;

public interface ITFAService
{
    string GenerateSecretKey(string email);
    string GetQRCodeUri(string email, string secret);
    bool Verify(string secret, string code);
}