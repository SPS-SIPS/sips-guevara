namespace SPSS.Domain.Enums;

public sealed class ServiceType : Enumeration<int>
{
    public static ServiceType VPN = new(1, nameof(VPN), "blue");
    public static ServiceType CallbackEndpoint = new(2, "Callback Endpoint", "pink");

    public ServiceType()
    {
    }
    public ServiceType(int id, string name, string groupId)
        : base(id, name, groupId)
    {
    }
}

