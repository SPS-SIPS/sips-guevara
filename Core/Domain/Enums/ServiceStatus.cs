namespace SPSS.Domain.Enums;

public sealed class ServiceStatus : Enumeration<int>
{
    public static ServiceStatus Up = new(1, nameof(Up), "green");
    public static ServiceStatus Down = new(0, nameof(Down), "red");

    public ServiceStatus()
    {
    }
    public ServiceStatus(int id, string name, string groupId)
        : base(id, name, groupId)
    {
    }
}

