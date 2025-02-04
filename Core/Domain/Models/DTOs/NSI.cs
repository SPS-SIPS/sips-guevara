namespace SPSS.Domain.Models.DTOs;

public sealed class NSI
{
    public string IDirectory { get; set; } = "";
    public string ODirectory { get; set; } = "";
    public string BDirectory { get; set; } = "";
    public bool GenerateFeesFile { get; set; } = false;
}
