namespace SPSS.Domain.Models.BL;

public sealed class ParticipantStatus : BaseEntity<int>
{
    public string InstitutionBic { get; set; } = default!;
    public string InstitutionName { get; set; } = default!;
    public bool IsLive { get; set; }
    public DateTimeOffset LastCheckedAt { get; set; }
    public DateTimeOffset LastStatusChangeAt { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int ConsecutiveSuccesses { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ParticipantStatusConfiguration : IEntityTypeConfiguration<ParticipantStatus>
{
    public void Configure(EntityTypeBuilder<ParticipantStatus> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasIndex(e => e.InstitutionBic).IsUnique();
        builder.HasIndex(e => e.IsLive);
        builder.HasIndex(e => e.LastCheckedAt);
        builder.HasIndex(e => e.UpdatedAt);
    }
}
