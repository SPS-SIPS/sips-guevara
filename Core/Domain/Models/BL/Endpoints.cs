namespace SPSS.Domain.Models.BL;

public sealed class Endpoint : BaseEntity<int>, IAuditableEntity
{
    public Participant Participant { get; set; } = default!;
    public string ParticipantId { get; set; } = default!;
    public DateTimeOffset Date { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
}

public sealed class EndpointConfiguration : IEntityTypeConfiguration<Endpoint>
{
    public void Configure(EntityTypeBuilder<Endpoint> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasOne(e => e.Participant)
            .WithMany(e => e.Endpoints)
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}