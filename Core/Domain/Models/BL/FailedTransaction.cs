namespace SPSS.Domain.Models.BL;

public sealed class FailedTransaction : BaseEntity<long>
{
    public long IpsTransactionId { get; set; }
    public DateTimeOffset SnapshotTimestamp { get; set; }
    public DateTimeOffset SnapshotFromDate { get; set; }
    public DateTimeOffset SnapshotToDate { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public List<string> RuleIds { get; set; } = [];
    public string? BankFrom { get; set; }
    public string? BankFromName { get; set; }
    public string? BankTo { get; set; }
    public string? BankToName { get; set; }
    public DateTimeOffset? CreatedDate { get; set; }
    public DateTimeOffset? ModifiedDate { get; set; }
    public DateTimeOffset? LastModifiedDate { get; set; }
    public DateTimeOffset? AcceptanceDate { get; set; }
    public string? TxId { get; set; }
    public string? Type { get; set; }
    public string? Amount { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public long? SttlId { get; set; }
    public string? Currency { get; set; }
    public string? IntrBkSttlmCurr { get; set; }
    public string? IntrBkSttlmAtm { get; set; }
    public string? DbtrAcctId { get; set; }
    public string? DbtrAcctType { get; set; }
    public string? DbtrName { get; set; }
    public string? DbtrAddress { get; set; }
    public string? CdtrAcctId { get; set; }
    public string? CdtrName { get; set; }
    public string? CdtrAddress { get; set; }
    public string? RequestId { get; set; }
    public string? TransCode { get; set; }
    public string? OldBalance { get; set; }
    public string? NewBalance { get; set; }
    public string? AliasFrom { get; set; }
    public string? AliasTo { get; set; }
    public string? AliasTypeFrom { get; set; }
    public string? AliasTypeTo { get; set; }
    public string? Error { get; set; }
    public string? ErrorReason { get; set; }
    public string? RawTransactionJson { get; set; }
    public string? TransactionsJson { get; set; }
    public string? MessagesJson { get; set; }
}

public sealed class FailedTransactionConfiguration : IEntityTypeConfiguration<FailedTransaction>
{
    public void Configure(EntityTypeBuilder<FailedTransaction> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.IpsTransactionId).IsRequired();
        builder.Property(e => e.SnapshotTimestamp).IsRequired();
        builder.Property(e => e.SnapshotFromDate).IsRequired();
        builder.Property(e => e.SnapshotToDate).IsRequired();
        builder.Property(e => e.ReceivedAt).IsRequired();
        builder.Property(e => e.RuleIds);

        builder.Property(e => e.BankFrom).HasMaxLength(20);
        builder.Property(e => e.BankTo).HasMaxLength(20);
        builder.Property(e => e.TxId).HasMaxLength(100);
        builder.Property(e => e.RequestId).HasMaxLength(100);
        builder.Property(e => e.Type).HasMaxLength(50);
        builder.Property(e => e.Status).HasMaxLength(50);
        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.IntrBkSttlmCurr).HasMaxLength(10);
        builder.Property(e => e.ErrorReason).HasMaxLength(100);

        builder.HasIndex(e => e.IpsTransactionId).IsUnique();
        builder.HasIndex(e => e.TxId);
        builder.HasIndex(e => e.RequestId);
        builder.HasIndex(e => e.BankFrom);
        builder.HasIndex(e => e.BankTo);
        builder.HasIndex(e => e.CreatedDate);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.SnapshotFromDate);
        builder.HasIndex(e => e.SnapshotToDate);
    }
}
