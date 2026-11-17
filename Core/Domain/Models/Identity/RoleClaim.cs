namespace SPSS.Domain.Models.Identity;

public sealed class RoleClaim
{
    public Role Role { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Claim Claim { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
}

public sealed class RoleClaimConfigurations : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.HasKey(e => new { e.Type, e.RoleId });

        builder.HasOne(e => e.Role).WithMany(e => e.RoleClaims).HasForeignKey(e => e.RoleId).IsRequired();
        // Preserve the deployed constraint name while updating the EF provider.
        builder.HasOne(e => e.Claim).WithMany(e => e.RoleClaims).HasForeignKey(e => e.Type).IsRequired()
            .Metadata.SetAnnotation("Relational:Name", "fk_roleclaims_claims_claimtempid");
    }
}