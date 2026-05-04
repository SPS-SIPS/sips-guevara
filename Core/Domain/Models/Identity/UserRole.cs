namespace SPSS.Domain.Models.Identity;

public sealed class UserRole
{
    public User User { get; set; } = null!;
    public int UserId { get; set; }

    public Role Role { get; set; } = null!;
    public Guid RoleId { get; set; }
}

public sealed class UserRoleConfigurations : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(e => new { e.UserId, e.RoleId });

        builder.HasOne(e => e.User).WithMany(e => e.Roles).HasForeignKey(e => e.UserId).IsRequired();
    }
}
