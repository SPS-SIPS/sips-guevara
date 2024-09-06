using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.Identity;
public sealed class User : BaseEntity<int>, IAuditableEntity, ISoftDelete, IVersionedEntity
{
    public string Username { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Photo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string SecurityStamp { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; }

    // Soft Delete
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
    public long Version { get; set; }

    public ICollection<UserRole> Roles { get; set; } = [];
    public ICollection<Token> Tokens { get; set; } = [];
    public Participant? Participant { get; set; }
}

public sealed class UserConfigurations : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Username).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Phone).IsRequired();

        builder.HasIndex(e => e.Version).IsUnique();

        builder.HasIndex(e => e.Phone).IsUnique();
        builder.HasIndex(e => e.Username).IsUnique();
        builder.HasMany(e => e.Roles).WithOne(e => e.User).HasForeignKey(e => e.UserId).IsRequired();
    }
}
