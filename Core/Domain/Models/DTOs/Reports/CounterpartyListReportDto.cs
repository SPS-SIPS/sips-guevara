using System.Collections.Generic;

namespace SPSS.Domain.Models.DTOs.Reports;

public class CounterpartyListReportDto
{
    public List<CounterpartyListItemDto> TopSenders { get; set; } = new();
    public List<CounterpartyListItemDto> TopReceivers { get; set; } = new();
}

public class CounterpartyListItemDto
{
    public string Bic { get; set; } = string.Empty;
    public long TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}
