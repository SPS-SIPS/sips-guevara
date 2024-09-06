namespace SPSS.Domain.Models.BL;

public sealed class Download : BaseEntity<int>
{
    public Certificate Certificate { get; set; } = default!;
    public string CertificateId { get; set; } = default!;

    public Participant Participant { get; set; } = default!;
    public string ParticipantId { get; set; } = default!;
    public string Username { get; set; } = default!;
    public DateTimeOffset Date { get; set; }
}

public sealed class DownloadConfiguration : IEntityTypeConfiguration<Download>
{
    public void Configure(EntityTypeBuilder<Download> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasOne(e => e.Participant).WithMany(e => e.Downloads)
            .HasForeignKey(e => e.ParticipantId).IsRequired();

        builder.HasOne(e => e.Certificate).WithMany(e => e.Downloads)
            .HasForeignKey(e => e.CertificateId).IsRequired();
    }
}
