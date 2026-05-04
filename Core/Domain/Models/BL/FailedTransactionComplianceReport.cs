namespace SPSS.Domain.Models.BL;

public sealed class FailedTransactionComplianceReport : BaseEntity<long>
{
    public DateTimeOffset WindowStart { get; set; }
    public DateTimeOffset WindowEnd { get; set; }
    public DateTimeOffset BaselineStart { get; set; }
    public DateTimeOffset BaselineEnd { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public int TotalFailed { get; set; }
    public int BaselineTotalFailed { get; set; }
    public decimal BaselineAverageHourly { get; set; }
    public int UniqueDebtors { get; set; }
    public int UniqueCreditors { get; set; }
    public int HighRiskFindings { get; set; }
    public int MediumRiskFindings { get; set; }
    public string ReportJson { get; set; } = "{}";
}

public sealed class FailedTransactionComplianceReportConfiguration : IEntityTypeConfiguration<FailedTransactionComplianceReport>
{
    public void Configure(EntityTypeBuilder<FailedTransactionComplianceReport> builder)
    {
        builder.Ignore(e => e.DomainEvents);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.HasIndex(e => new { e.WindowStart, e.WindowEnd }).IsUnique();
        builder.HasIndex(e => e.GeneratedAt);
        builder.HasIndex(e => e.TotalFailed);
        builder.HasIndex(e => e.HighRiskFindings);
    }
}
