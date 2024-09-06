namespace SPSS.Domain.Models.Identity;

public sealed class Claim
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<RoleClaim> RoleClaims { get; set; } = [];
}

public sealed class ClaimConfigurations : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {

        builder.HasKey(e => e.Type);
        builder.Property(e => e.Type).ValueGeneratedNever();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
    }
}
