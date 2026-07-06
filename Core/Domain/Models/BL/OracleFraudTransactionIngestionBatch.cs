namespace SPSS.Domain.Models.BL;

public sealed class OracleFraudTransactionIngestionBatch : BaseEntity<long>
{
    public string Source { get; set; } = string.Empty;
    public long FromUtrnno { get; set; }
    public long ToUtrnno { get; set; }
    public long HighWatermarkUtrnno { get; set; }
    public DateTimeOffset ProducerTimestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
    public int TotalCount { get; set; }
    public int ReceivedCount { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int RejectedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> SampleErrors { get; set; } = [];
}

public sealed class OracleFraudTransactionIngestionBatchConfiguration
    : IEntityTypeConfiguration<OracleFraudTransactionIngestionBatch>
{
    public void Configure(EntityTypeBuilder<OracleFraudTransactionIngestionBatch> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Source).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
        builder.Property(e => e.SampleErrors).IsRequired();

        builder.HasIndex(e => new { e.Source, e.FromUtrnno, e.ToUtrnno, e.HighWatermarkUtrnno }).IsUnique();
        builder.HasIndex(e => e.Source);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ReceivedAt);
        builder.HasIndex(e => e.ProcessedAt);
    }
}
