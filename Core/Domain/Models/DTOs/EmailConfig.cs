namespace SPSS.Domain.Models.DTOs;

public sealed class EmailConfig
{
    public string Server { get; set; } = null!;
    public int Port { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool IsSSL { get; set; } = false;
    public string TemplatesDirectory { get; set; } = null!;
    public string[] CC { get; set; } = null!;
}
