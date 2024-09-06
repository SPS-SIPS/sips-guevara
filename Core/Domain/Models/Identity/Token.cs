namespace SPSS.Domain.Models.Identity;

public sealed class Token : BaseEntity<Guid>
{
    public Guid Jti { get; set; }
    public int Value { get; set; }  // Only For OTP Tokens
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
    public bool Used { get; set; } = false;
    public bool Invalidated { get; set; } = false;
    public TokenType TokenType { get; set; } = TokenType.Refresh;
    public User User { get; set; } = null!;
    public int UserId { get; set; }
    public string IPAddress { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
    public string OS { get; set; } = null!;
}

public sealed class TokenConfigurations : IEntityTypeConfiguration<Token>
{
    public void Configure(EntityTypeBuilder<Token> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Jti).IsRequired();
        builder.Property(e => e.CreatedOn).IsRequired();
        builder.Property(e => e.ExpiresOn).IsRequired();

        builder.Property(e => e.TokenType).HasConversion(x => x.Id, x => Enumeration<int>.FromId<TokenType>(x));

        builder.HasIndex(e => e.Used);
        builder.HasIndex(e => e.Invalidated);
        builder.HasIndex(e => e.ExpiresOn);
        builder.HasIndex(e => e.TokenType);


        builder.HasOne(e => e.User).WithMany(e => e.Tokens).HasForeignKey(e => e.UserId).IsRequired();
    }
}