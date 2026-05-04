namespace SPSS.Domain.Models.DTOs.Reports;

public class NetPositionReportDto
{
    public string Bic { get; set; } = string.Empty;
    public decimal NetPosition { get; set; }
}
