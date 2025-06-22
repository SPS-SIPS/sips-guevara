namespace SPSS.Domain.Models.DTOs.Reports;

public class TrendPackReportDto
{
    public List<TrendPointDto> Points { get; set; } = [];
}

public class TrendPointDto
{
    public DateTime Date { get; set; }
    public decimal TotalValue { get; set; }
    public long TotalTransactions { get; set; }
}
