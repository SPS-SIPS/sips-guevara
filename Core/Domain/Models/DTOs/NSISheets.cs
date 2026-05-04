namespace SPSS.Domain.Models.DTOs;

public class NSISheets
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
}