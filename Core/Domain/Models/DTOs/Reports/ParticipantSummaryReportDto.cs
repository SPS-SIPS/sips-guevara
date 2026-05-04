namespace SPSS.Domain.Models.DTOs.Reports;

public class ParticipantSummaryReportDto
{
    public string Bic { get; set; } = string.Empty;
    public long SentCount { get; set; }
    public decimal TotalAmountSent { get; set; }
    public long ReceivedCount { get; set; }
    public decimal TotalAmountReceived { get; set; }
    public decimal TotalFeesReceived { get; set; }
    public decimal NetPosition => TotalAmountReceived - TotalAmountSent;
}
