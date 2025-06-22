namespace SPSS.Domain.Models.DTOs.Reports;

public class SystemOverviewReportDto
{
    public int TotalParticipants { get; set; }
    public long TotalTransactions { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalFees { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
