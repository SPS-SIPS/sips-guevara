namespace SPSS.Domain.Models.BL;

public class BicInteraction
{
    public long Id { get; set; }
    public string Bic { get; set; } = string.Empty;
    public long TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}