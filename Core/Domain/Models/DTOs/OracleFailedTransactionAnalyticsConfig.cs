namespace SPSS.Domain.Models.DTOs;

public sealed class OracleFailedTransactionAnalyticsConfig
{
    public bool Enabled { get; set; } = true;
    public int RunIntervalHours { get; set; } = 1;
    public DateTimeOffset FirstWindowStartAt { get; set; } = new(2026, 7, 12, 0, 0, 0, TimeSpan.Zero);
    public int LookbackDays { get; set; } = 30;
    public int RetentionDays { get; set; } = 90;
    public string[] FailureStatuses { get; set; } = ["2"];
}
