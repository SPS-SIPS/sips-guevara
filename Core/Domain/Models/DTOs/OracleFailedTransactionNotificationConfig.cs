namespace SPSS.Domain.Models.DTOs;

public sealed class OracleFailedTransactionNotificationConfig
{
    public bool Enabled { get; set; } = true;
    public int LookbackHours { get; set; } = 24;
    public DateTimeOffset FirstWindowStartAt { get; set; } = new(2026, 7, 12, 16, 0, 0, TimeSpan.Zero);
    public string[] FailureStatuses { get; set; } = ["2"];
}
