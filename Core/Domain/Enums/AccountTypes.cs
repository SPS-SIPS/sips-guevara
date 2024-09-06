namespace SPSS.Domain.Enums;

public sealed class AccountTypes : Enumeration<string>
{
    public static AccountTypes MobilePhone = new("MSIS", "Mobile Phone", "");
    public static AccountTypes AccountNo = new("ACCT", "Account No", "");
    public static AccountTypes EWallet = new("EWLT", "e-wallet identifier", "");
    public static AccountTypes AliasIdentifier = new("ALIS", "Alias identifier", "");

    public AccountTypes()
    {
    }
    public AccountTypes(string id, string name, string groupId)
        : base(id, name, groupId)
    {
    }
}