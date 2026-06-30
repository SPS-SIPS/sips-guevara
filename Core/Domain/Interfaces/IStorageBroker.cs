using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Interfaces;

public interface IStorageBroker
{
    DbSet<Role> Roles { get; set; }
    DbSet<Token> Tokens { get; set; }
    DbSet<UserRole> UserRoles { get; set; }
    DbSet<User> Users { get; set; }
    DbSet<Claim> Claims { get; set; }
    DbSet<RoleClaim> RoleClaims { get; set; }

    DbSet<Participant> Participants { get; set; }
    DbSet<Certificate> Certificates { get; set; }
    DbSet<Download> Downloads { get; set; }
    DbSet<MonitHistory> MonitHistories { get; set; }
    DbSet<ParticipantStatus> ParticipantStatuses { get; set; }
    DbSet<ParticipantThreshold> ParticipantThresholds { get; set; }
    DbSet<FailedTransaction> FailedTransactions { get; set; }
    DbSet<FailedTransactionRuleAssignment> FailedTransactionRuleAssignments { get; set; }
    DbSet<FailedTransactionRuleDefinition> FailedTransactionRuleDefinitions { get; set; }
    DbSet<FailedTransactionComplianceReport> FailedTransactionComplianceReports { get; set; }
    DbSet<TransactionIngestionBatch> TransactionIngestionBatches { get; set; }
    DbSet<ProcessedTransaction> ProcessedTransactions { get; set; }
    DbSet<OracleTransactionIngestionBatch> OracleTransactionIngestionBatches { get; set; }
    DbSet<OracleTransaction> OracleTransactions { get; set; }
    DbSet<NetSettlementHeader> NetSettlementHeaders { get; set; }
    DbSet<NetSettlementDetail> NetSettlementDetails { get; set; }
    DbSet<BicStatistic> BicStatistics { get; set; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    int SaveChanges();
    Task<int> ExecuteSqlRawAsync(string query, CancellationToken cancellationToken);
    bool IsRelational();
    void Dispose();

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
