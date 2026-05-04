namespace SPSS.Domain.Common;
public class BackupConfiguration
{
    public string? Path { get; set; }
    public string? Expression { get; set; }
    public string? TimeZoneInfo { get; set; }
    public bool UseAuth { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }

}