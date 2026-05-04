namespace SPSS.API.Models;

public sealed class GenerateQRRequest
{
    public int CurrencyCode { get; set; }
    public string? MerchantId { get; set; }
    public string? MerchantName { get; set; }
    public int MerchantCategoryCode { get; set; }
    public string? MerchantCity { get; set; }
    public string? PostalCode { get; set; }
    public string? StoreLabel { get; set; }
    public string? TerminalLabel { get; set; }
}

public sealed class P2PRequest
{
    public string? FiName { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public string? AccountName { get; set; }
    public decimal Amount { get; set; }
    public string? Particulars { get; set; } // narrative
}