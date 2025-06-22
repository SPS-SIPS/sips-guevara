using System.Collections.Generic;

namespace SPSS.Domain.Models.DTOs.Reports;

public class FeeDistributionReportDto
{
    public List<FeeDistributionItemDto> Items { get; set; } = new();
}

public class FeeDistributionItemDto
{
    public string Bic { get; set; } = string.Empty;
    public decimal TotalFeesReceived { get; set; }
}
