namespace SPSS.Domain.Models.BL;

public sealed class Tunnel : BaseEntity<int>, IAuditableEntity
{
    public VPN VPN { get; set; } = default!;
    public int VPNId { get; set; } = default!;
    public DateTimeOffset Date { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; }

    public string Endpoint { get; set; } = default!;
    public string Name { get; set; } = default!;
}

public sealed class TunnelConfiguration : IEntityTypeConfiguration<Tunnel>
{
    public void Configure(EntityTypeBuilder<Tunnel> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasOne(e => e.VPN)
            .WithMany(e => e.Tunnels)
            .HasForeignKey(e => e.VPNId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}