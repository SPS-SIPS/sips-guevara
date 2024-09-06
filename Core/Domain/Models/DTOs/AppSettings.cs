namespace SPSS.Domain.Models.DTOs;

public sealed class AppSettings
{
    public string[] RestrictedRoles { get; set; } = [];
    public bool ForcePWDChange { get; set; } = false;
    public int? CacheInMins { get; set; }
}
