namespace SPSS.Domain.Models.BL;

public sealed class FailedTransaction : BaseEntity<long>
{
    public string? BankFrom { get; set; }
    public string? BankFromName { get; set; }
    public string? BankTo { get; set; }
    public string? BankToName { get; set; }
    public DateTimeOffset? CreatedDate { get; set; }
    public DateTimeOffset? ModifiedDate { get; set; }
    public DateTimeOffset? AcceptanceDate { get; set; }
    public string? TxId { get; set; }
    public string? Type { get; set; }
    public string? Amount { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public long? SttlId { get; set; }
    public string? Currency { get; set; }
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
    public string? Error { get; set; }
    public string? ErrorReason { get; set; }
}

public sealed class FailedTransactionRuleAssignment
{
    public long Id { get; set; }
    public long FailedTransactionId { get; set; }
    public List<string> RuleIds { get; set; } = [];
}

public sealed class FailedTransactionRuleDefinition : BaseEntity<long>
{
    public string RuleId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
}

public sealed class FailedTransactionConfiguration : IEntityTypeConfiguration<FailedTransaction>
{
    public void Configure(EntityTypeBuilder<FailedTransaction> builder)
    {
        builder.Ignore(e => e.DomainEvents);
        builder.Ignore(e => e.Active);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.BankFrom).HasMaxLength(20);
        builder.Property(e => e.BankTo).HasMaxLength(20);
        builder.Property(e => e.TxId).HasMaxLength(100);
        builder.Property(e => e.RequestId).HasMaxLength(100);
        builder.Property(e => e.Type).HasMaxLength(50);
        builder.Property(e => e.Status).HasMaxLength(50);
        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.ErrorReason).HasMaxLength(100);

        builder.HasIndex(e => e.TxId);
        builder.HasIndex(e => e.RequestId);
        builder.HasIndex(e => e.BankFrom);
        builder.HasIndex(e => e.BankTo);
        builder.HasIndex(e => e.CreatedDate);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Type);
    }
}

public sealed class FailedTransactionRuleAssignmentConfiguration : IEntityTypeConfiguration<FailedTransactionRuleAssignment>
{
    public void Configure(EntityTypeBuilder<FailedTransactionRuleAssignment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.FailedTransactionId).IsRequired();
        builder.Property(e => e.RuleIds).IsRequired();

        builder.HasIndex(e => e.FailedTransactionId).IsUnique();
        builder.HasOne<FailedTransaction>()
            .WithMany()
            .HasForeignKey(e => e.FailedTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FailedTransactionRuleDefinitionConfiguration : IEntityTypeConfiguration<FailedTransactionRuleDefinition>
{
    public void Configure(EntityTypeBuilder<FailedTransactionRuleDefinition> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.RuleId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Label).HasMaxLength(250).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.Active).IsRequired();

        builder.HasIndex(e => e.RuleId).IsUnique();
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.Active);
    }
}
