namespace SPSS.Domain.Models.BL;

public sealed class NetSettlementDetail : BaseEntity<long>
{
    public long NetSettlementHeaderId { get; set; }
    public NetSettlementHeader NetSettlementHeader { get; set; } = null!;
    // PMT.ID
    public string EndToEndId { get; set; } = string.Empty;
    public string TxId { get; set; } = string.Empty;
    public string UETR { get; set; } = string.Empty;
    // PMT.INFO
    public string InstructionPriority { get; set; } = "HIGH";
    public string SvcLvl { get; set; } = string.Empty;
    public string LocalInstrument { get; set; } = "RTGSNetSettlement";
    // Settlement Information
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
    public string DebtorBic { get; set; } = string.Empty;
    public string CreditorBic { get; set; } = string.Empty;
    public string CategoryPurpose { get; set; } = string.Empty;
    public bool IsFee { get; set; }
}

public sealed class NetSettlementDetailConfiguration : IEntityTypeConfiguration<NetSettlementDetail>
{
    public void Configure(EntityTypeBuilder<NetSettlementDetail> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasIndex(e => e.IsFee);
        builder.HasIndex(e => e.DebtorBic);
        builder.HasIndex(e => e.CreditorBic);
        builder.HasIndex(e => e.Date);


        builder.HasOne(e => e.NetSettlementHeader)
            .WithMany(e => e.NetSettlementDetails)
            .HasForeignKey(e => e.NetSettlementHeaderId);
    }
}
