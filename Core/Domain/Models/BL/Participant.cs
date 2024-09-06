
namespace SPSS.Domain.Models.BL;

public sealed class Participant : BaseEntity<string>, IAuditableEntity, ISoftDelete
{
    public string Name { get; set; } = default!;
    public string NormalizedName { get; set; } = default!;

    public User User { get; set; } = default!;
    public int UserId { get; set; }

    public ICollection<Certificate> Certificates { get; set; } = [];
    public ICollection<Download> Downloads { get; set; } = [];
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
}

public sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();

        builder.HasOne(e => e.User).WithOne(e => e.Participant).HasForeignKey<Participant>(e => e.UserId).IsRequired();
    }
}
