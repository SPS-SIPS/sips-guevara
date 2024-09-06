namespace SPSS.Domain.Enums;

public sealed class TokenType : Enumeration<int>
{
    public static TokenType Refresh = new(1, nameof(Refresh), "blue");
    public static TokenType OTP = new(2, nameof(OTP), "pink");

    public TokenType()
    {
    }
    public TokenType(int id, string name, string groupId)
        : base(id, name, groupId)
    {
    }
}

