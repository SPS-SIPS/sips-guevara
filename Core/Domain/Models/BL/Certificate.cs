namespace SPSS.Domain.Models.BL;

public sealed class Certificate : BaseEntity<string>
{
    public string Name { get; set; } = default!;
    public string Subject { get; set; } = default!;

    public Participant Participant { get; set; } = default!;
    public string ParticipantId { get; set; } = default!;

    public DateTimeOffset ExpireDate { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset CachedOn { get; set; }
    public ICollection<Download> Downloads { get; set; } = [];
}

public sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).IsRequired();
        builder.Property(e => e.ExpireDate).IsRequired();

        builder.HasIndex(e => e.ExpireDate);
        builder.HasIndex(e => e.CachedOn);


        builder.HasOne(e => e.Participant).WithMany(e => e.Certificates)
            .HasForeignKey(e => e.ParticipantId).IsRequired();

    }
}
