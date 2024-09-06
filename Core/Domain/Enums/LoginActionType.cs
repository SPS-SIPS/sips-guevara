namespace SPSS.Domain.Enums;

public sealed class LoginActionType : Enumeration<int>
{
    public static LoginActionType Verify = new(1, nameof(Verify), "blue");
    public static LoginActionType Continue = new(2, nameof(Continue), "pink");
    public static LoginActionType ForcePWDChange = new(2, nameof(ForcePWDChange), "green");

    public LoginActionType()
    {
    }
    public LoginActionType(int id, string name, string groupId)
        : base(id, name, groupId)
    {
    }
}
