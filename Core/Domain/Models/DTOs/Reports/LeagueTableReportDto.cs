using System.Collections.Generic;

namespace SPSS.Domain.Models.DTOs.Reports;

public class LeagueTableReportDto
{
    public List<LeagueTableItemDto> Items { get; set; } = new();
}

public class LeagueTableItemDto
{
    public string Bic { get; set; } = string.Empty;
    public decimal NetPosition { get; set; }
    public long TotalTransactions { get; set; }
    public decimal TotalValue { get; set; }
}
