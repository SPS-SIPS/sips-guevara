namespace SPSS.Domain.Models.DTOs;

public sealed class SMSSettings
{
    public string URL { get; set; } = null!;
    public string Key { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool SendSMS { get; set; } = false;

}
