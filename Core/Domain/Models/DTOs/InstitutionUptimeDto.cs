namespace SPSS.Domain.Models.DTOs;

public sealed class InstitutionUptimeDto
{
    public string InstitutionBic { get; set; } = default!;
    public double Uptime { get; set; }
    public int TotalChecks { get; set; }
    public int SuccessChecks { get; set; }
}
