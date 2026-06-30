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
    public DbSet<MonitHistory> MonitHistories { get; set; } = null!;
    public DbSet<ParticipantStatus> ParticipantStatuses { get; set; } = null!;
    public DbSet<ParticipantThreshold> ParticipantThresholds { get; set; } = null!;
    public DbSet<FailedTransaction> FailedTransactions { get; set; } = null!;
    public DbSet<FailedTransactionRuleAssignment> FailedTransactionRuleAssignments { get; set; } = null!;
    public DbSet<FailedTransactionComplianceReport> FailedTransactionComplianceReports { get; set; } = null!;
    public DbSet<TransactionIngestionBatch> TransactionIngestionBatches { get; set; } = null!;
    public DbSet<ProcessedTransaction> ProcessedTransactions { get; set; } = null!;
    public DbSet<OracleTransactionIngestionBatch> OracleTransactionIngestionBatches { get; set; } = null!;
    public DbSet<OracleTransaction> OracleTransactions { get; set; } = null!;
    public DbSet<NetSettlementHeader> NetSettlementHeaders { get; set; } = null!;
    public DbSet<NetSettlementDetail> NetSettlementDetails { get; set; } = null!;
    public DbSet<BicStatistic> BicStatistics { get; set; } = null!;

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
