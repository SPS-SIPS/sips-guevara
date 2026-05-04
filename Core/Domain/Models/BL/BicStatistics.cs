namespace SPSS.Domain.Models.BL;

public sealed class BicStatistic : BaseEntity<long>
{
    public long NetSettlementHeaderId { get; set; }
    public NetSettlementHeader NetSettlementHeader { get; set; } = null!;
    public DateTimeOffset Date { get; set; }
    public string Bic { get; set; } = string.Empty;
    public long SentCount { get; set; }
    public decimal TotalAmountSent { get; set; }
    public long ReceivedCount { get; set; }
    public decimal TotalAmountReceived { get; set; }
    public decimal TotalFeesReceived { get; set; }
    public IList<BicInteraction> TopReceivers { get; set; } = [];
    public IList<BicInteraction> TopSenders { get; set; } = [];
}

public sealed class BicStatisticConfiguration : IEntityTypeConfiguration<BicStatistic>
{
    public void Configure(EntityTypeBuilder<BicStatistic> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();


        builder.HasOne(e => e.NetSettlementHeader)
            .WithMany(e => e.BicStatistics)
            .HasForeignKey(e => e.NetSettlementHeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(e => e.TopReceivers, b =>
        {
            b.WithOwner().HasForeignKey("BicStatisticId");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
            b.Property(e => e.Bic).IsRequired(true);
            b.Property(e => e.TransactionCount).IsRequired();
            b.Property(e => e.TotalAmount).IsRequired();
        });
        builder.OwnsMany(e => e.TopSenders, b =>
        {
            b.WithOwner().HasForeignKey("BicStatisticId");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
            b.Property(e => e.Bic).IsRequired(true);
            b.Property(e => e.TransactionCount).IsRequired();
            b.Property(e => e.TotalAmount).IsRequired();
        });

        builder.Property(e => e.Bic)
            .IsRequired(true);

        builder.HasIndex(e => e.Bic);
    }
}