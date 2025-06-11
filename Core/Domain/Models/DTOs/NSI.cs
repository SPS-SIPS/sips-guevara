namespace SPSS.Domain.Models.DTOs;

public sealed class NSI
{
    public string Directory { get; set; } = "";
    public bool GenerateFeesFile { get; set; } = false;
    public string? FeeTTC { get; set; } = null;
    public string? TxnTTC { get; set; } = null;
}
