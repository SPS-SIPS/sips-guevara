namespace SPSS.Domain.Enums;
public sealed class PasswordVerificationResult : Enumeration<int>
{
    public static PasswordVerificationResult Failed = new(1, nameof(Failed), string.Empty);
    public static PasswordVerificationResult Success = new(2, nameof(Success), string.Empty);

    public PasswordVerificationResult(int id, string name, string groupId)
        : base(id, name, groupId)
    {
    }
}
