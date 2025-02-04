namespace SPSS.Domain.Models.DTOs;

public class NSITransaction
{
    public string? EndToEndId { get; set; }
    public string? TxId { get; set; }
    public string? Priority { get; set; }
    public string? ServiceLevel { get; set; }
    public string? LocalInstrument { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? DebtorBIC { get; set; }
    public string? CreditorBIC { get; set; }
}