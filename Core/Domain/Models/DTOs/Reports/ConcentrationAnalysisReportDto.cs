using System.Collections.Generic;

namespace SPSS.Domain.Models.DTOs.Reports;

public class ConcentrationAnalysisReportDto
{
    public List<ConcentrationItemDto> TopConcentrations { get; set; } = new();
    public decimal Top5SharePercent { get; set; }
    public decimal Top10SharePercent { get; set; }
}

public class ConcentrationItemDto
{
    public string Bic { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public long TotalTransactions { get; set; }
}
