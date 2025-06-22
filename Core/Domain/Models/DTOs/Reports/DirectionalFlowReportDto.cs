using System.Collections.Generic;

namespace SPSS.Domain.Models.DTOs.Reports;

public class DirectionalFlowReportDto
{
    public List<DirectionalFlowItemDto> Flows { get; set; } = new();
}

public class DirectionalFlowItemDto
{
    public string FromBic { get; set; } = string.Empty;
    public string ToBic { get; set; } = string.Empty;
    public long TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}
