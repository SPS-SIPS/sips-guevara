namespace SPSS.Domain.Models.BL;

public sealed class TransactionIngestionBatch : BaseEntity<long>
{
    public string BatchType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset FromDate { get; set; }
    public DateTimeOffset ToDate { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
    public string? SnapshotType { get; set; }
    public string? SnapshotStatus { get; set; }
    public int TotalCount { get; set; }
    public int ReceivedCount { get; set; }
    public int UniqueCount { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int RejectedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> SampleErrors { get; set; } = [];
}

public sealed class TransactionIngestionBatchConfiguration : IEntityTypeConfiguration<TransactionIngestionBatch>
{
    public void Configure(EntityTypeBuilder<TransactionIngestionBatch> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.BatchType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.SnapshotType).HasMaxLength(50);
        builder.Property(e => e.SnapshotStatus).HasMaxLength(50);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
        builder.Property(e => e.SampleErrors).IsRequired();

        builder.HasIndex(e => e.BatchType);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.FromDate);
        builder.HasIndex(e => e.ToDate);
        builder.HasIndex(e => e.ReceivedAt);
    }
}
