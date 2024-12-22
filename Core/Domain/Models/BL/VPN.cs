namespace SPSS.Domain.Models.BL;

public sealed class VPN : BaseEntity<int>, IAuditableEntity
{
    public Participant Participant { get; set; } = default!;
    public string ParticipantId { get; set; } = default!;
    public DateTimeOffset Date { get; set; }
    public string Gateway { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; }

    public ICollection<Tunnel> Tunnels { get; set; } = [];
}

public sealed class VPNConfiguration : IEntityTypeConfiguration<VPN>
{
    public void Configure(EntityTypeBuilder<VPN> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasOne(e => e.Participant)
            .WithMany(e => e.VPNs)
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}