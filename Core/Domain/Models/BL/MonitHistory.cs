namespace SPSS.Domain.Models.BL;

public sealed class MonitHistory : BaseEntity<int>
{
    public string InstitutionName { get; set; } = default!;
    public string InstitutionBic { get; set; } = default!;
    public bool Success { get; set; }
    public int HttpStatus { get; set; }
    public string? Response { get; set; }
    public string? Error { get; set; }
    public string? ErrorType { get; set; }
    public DateTimeOffset CheckedAt { get; set; }
}

public sealed class MonitHistoryConfiguration : IEntityTypeConfiguration<MonitHistory>
{
    public void Configure(EntityTypeBuilder<MonitHistory> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasIndex(x => x.InstitutionBic);
        builder.HasIndex(x => x.InstitutionName);
        builder.HasIndex(x => x.CheckedAt);
        builder.HasIndex(x => x.Success);
    }
}