namespace SPSS.Domain.Models.BL;

public sealed class OracleFraudTransaction : BaseEntity<long>
{
    public string Source { get; set; } = string.Empty;
    public long Utrnno { get; set; }
    public string RespCode { get; set; } = string.Empty;
    public string FraudZone { get; set; } = string.Empty;
    public string? RulesList { get; set; }
    public List<string> Rules { get; set; } = [];
    public string Refnum { get; set; } = string.Empty;
    public string? RiskValue { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OracleFraudTransactionConfiguration : IEntityTypeConfiguration<OracleFraudTransaction>
{
    public void Configure(EntityTypeBuilder<OracleFraudTransaction> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Source).HasMaxLength(50).IsRequired();
        builder.Property(e => e.RespCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.FraudZone).HasMaxLength(50).IsRequired();
        builder.Property(e => e.RulesList).HasMaxLength(1000);
        builder.Property(e => e.Rules).IsRequired();
        builder.Property(e => e.Refnum).HasMaxLength(100).IsRequired();
        builder.Property(e => e.RiskValue).HasMaxLength(100);

        builder.HasIndex(e => new { e.Source, e.Utrnno }).IsUnique();
        builder.HasIndex(e => e.Utrnno);
        builder.HasIndex(e => e.Refnum);
        builder.HasIndex(e => e.RespCode);
        builder.HasIndex(e => e.FraudZone);
        builder.HasIndex(e => e.ReceivedAt);
    }
}
