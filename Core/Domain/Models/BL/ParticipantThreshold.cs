using SPSS.Domain.Models.Enums;

namespace SPSS.Domain.Models.BL;

public sealed class ParticipantThreshold : BaseEntity<int>
{
    public string Bic { get; set; } = default!;
    public decimal GoldenAmount { get; set; }
    public decimal AmberPercentage { get; set; }
    public decimal RedPercentage { get; set; }
    public ThresholdZone LastKnownZone { get; set; }
    public decimal? LastKnownBalance { get; set; }
    public DateTimeOffset? LastZoneChangeAt { get; set; }
    public DateTimeOffset? LastAlertSentAt { get; set; }
    public string? AlertRecipients { get; set; }
}

public sealed class ParticipantThresholdConfiguration : IEntityTypeConfiguration<ParticipantThreshold>
{
    public void Configure(EntityTypeBuilder<ParticipantThreshold> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Bic)
            .IsRequired()
            .HasMaxLength(11);

        builder.HasIndex(e => e.Bic)
            .IsUnique();

        builder.Property(e => e.GoldenAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.AmberPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(e => e.RedPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(e => e.LastKnownZone)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.LastKnownBalance)
            .HasPrecision(18, 2);

        builder.Property(e => e.AlertRecipients)
            .HasMaxLength(500);

        builder.HasIndex(e => e.LastKnownZone);
        builder.HasIndex(e => e.LastZoneChangeAt);
    }
}
