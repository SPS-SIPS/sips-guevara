namespace SPSS.Domain.Models.BL;

public sealed class NetSettlementHeader : BaseEntity<long>
{
    public string MsgId { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public DateTimeOffset CredDtTm { get; set; }
    public int NumberOfTrans { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalTransactions { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string OriginalUserReference { get; set; } = string.Empty;
    public string BizMsgId { get; set; } = string.Empty;
    public DateTimeOffset InterBankSettlementDate { get; set; }
    public string OriginalFile { get; set; } = string.Empty;
    public ICollection<NetSettlementDetail> NetSettlementDetails { get; set; } = [];

    public bool Processed { get; set; } = false;
    public DateTimeOffset? ProcessedOn { get; set; }
    public int Count { get; set; } = 0;
    public string? Error { get; set; }
    public string? FilePath { get; set; }
}


public sealed class NetSettlementHeaderConfiguration : IEntityTypeConfiguration<NetSettlementHeader>
{
    public void Configure(EntityTypeBuilder<NetSettlementHeader> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasIndex(e => e.MsgId).IsUnique();
        builder.HasIndex(e => e.CredDtTm);

        builder.Property(e => e.OriginalFile)
            .IsRequired(true);
    }
}