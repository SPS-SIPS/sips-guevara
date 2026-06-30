namespace SPSS.Domain.Models.BL;

public sealed class OracleTransactionIngestionBatch : BaseEntity<long>
{
    public string Source { get; set; } = string.Empty;
    public long FromId { get; set; }
    public long ToId { get; set; }
    public long HighWatermarkId { get; set; }
    public DateTimeOffset ProducerTimestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<string> Statuses { get; set; } = [];
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
    public string? RawPayload { get; set; }
}

public sealed class OracleTransactionIngestionBatchConfiguration : IEntityTypeConfiguration<OracleTransactionIngestionBatch>
{
    public void Configure(EntityTypeBuilder<OracleTransactionIngestionBatch> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Source).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Statuses).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
        builder.Property(e => e.SampleErrors).IsRequired();

        builder.HasIndex(e => new { e.Source, e.FromId, e.ToId, e.HighWatermarkId }).IsUnique();
        builder.HasIndex(e => e.Source);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ReceivedAt);
        builder.HasIndex(e => e.ProcessedAt);
    }
}
