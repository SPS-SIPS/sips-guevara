using Microsoft.EntityFrameworkCore.Storage;
using SPSS.Domain.Models.BL;

namespace SPSS.Infrastructure.Persistence;

public sealed class StorageBroker(DbContextOptions<StorageBroker> options) : DbContext(options), IStorageBroker
{
    // Identity
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Token> Tokens { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<Domain.Models.Identity.Claim> Claims { get; set; } = null!;
    public DbSet<RoleClaim> RoleClaims { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Participant> Participants { get; set; } = null!;
    public DbSet<Certificate> Certificates { get; set; } = null!;
    public DbSet<Download> Downloads { get; set; } = null!;
    public DbSet<VPN> VPNs { get; set; } = null!;
    public DbSet<Tunnel> Tunnels { get; set; } = null!;
    public DbSet<Domain.Models.BL.Endpoint> Endpoints { get; set; } = null!;
    public DbSet<MonitHistory> MonitHistories { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        return base.SaveChanges();
    }

    public Task<int> ExecuteSqlRawAsync(string query, CancellationToken cancellationToken)
    {
        return Database.ExecuteSqlRawAsync(query, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IStorageBroker).Assembly);
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
    }
    public bool IsRelational() => Database.IsRelational();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }
}