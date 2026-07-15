namespace SPSS.Domain.Models.BL;

public sealed class OracleFailedTransactionAnalyticsRun : BaseEntity<long>
{
    public DateTimeOffset WindowStart { get; set; }
    public DateTimeOffset WindowEnd { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public int UniqueSendingInstitutions { get; set; }
    public int UniqueReceivingInstitutions { get; set; }
    public int UniqueAccounts { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class OracleFailedInstitutionAnalytic : BaseEntity<long>
{
    public long RunId { get; set; }
    public string SendingInstitution { get; set; } = string.Empty;
    public string ReceivingInstitution { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int DistinctDebtorAccounts { get; set; }
    public string TopFailureCode { get; set; } = string.Empty;
    public string TopFailureReason { get; set; } = string.Empty;
    public DateTimeOffset? FirstTransactionAt { get; set; }
    public DateTimeOffset? LastTransactionAt { get; set; }
}

public sealed class OracleFailedAccountAnalytic : BaseEntity<long>
{
    public long RunId { get; set; }
    public string InstitutionBic { get; set; } = string.Empty;
    public string ReceivingInstitution { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string TopFailureCode { get; set; } = string.Empty;
    public string TopFailureReason { get; set; } = string.Empty;
    public DateTimeOffset? FirstTransactionAt { get; set; }
    public DateTimeOffset? LastTransactionAt { get; set; }
}

public sealed class OracleFailedRejectionTypeAnalytic : BaseEntity<long>
{
    public long RunId { get; set; }
    public string SendingInstitution { get; set; } = string.Empty;
    public string ReceivingInstitution { get; set; } = string.Empty;
    public string RejectionCategory { get; set; } = string.Empty;
    public string FailureCode { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class OracleFailedTrendAnalytic : BaseEntity<long>
{
    public long RunId { get; set; }
    public DateTimeOffset BucketStart { get; set; }
    public string BucketType { get; set; } = "DAY";
    public string SendingInstitution { get; set; } = string.Empty;
    public string ReceivingInstitution { get; set; } = string.Empty;
    public string RejectionCategory { get; set; } = string.Empty;
    public string FailureCode { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class OracleFailedTransactionAnalyticsRunConfiguration : IEntityTypeConfiguration<OracleFailedTransactionAnalyticsRun>
{
    public void Configure(EntityTypeBuilder<OracleFailedTransactionAnalyticsRun> builder)
    {
        builder.Ignore(e => e.DomainEvents);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);
        builder.HasIndex(e => e.GeneratedAt);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.WindowStart, e.WindowEnd });
    }
}

public sealed class OracleFailedInstitutionAnalyticConfiguration : IEntityTypeConfiguration<OracleFailedInstitutionAnalytic>
{
    public void Configure(EntityTypeBuilder<OracleFailedInstitutionAnalytic> builder)
    {
        builder.Ignore(e => e.DomainEvents);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.SendingInstitution).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ReceivingInstitution).HasMaxLength(20).IsRequired();
        builder.Property(e => e.TopFailureCode).HasMaxLength(100);
        builder.Property(e => e.TopFailureReason).HasMaxLength(250);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(e => e.RunId);
        builder.HasIndex(e => e.SendingInstitution);
        builder.HasIndex(e => e.ReceivingInstitution);
    }
}

public sealed class OracleFailedAccountAnalyticConfiguration : IEntityTypeConfiguration<OracleFailedAccountAnalytic>
{
    public void Configure(EntityTypeBuilder<OracleFailedAccountAnalytic> builder)
    {
        builder.Ignore(e => e.DomainEvents);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.InstitutionBic).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ReceivingInstitution).HasMaxLength(20).IsRequired();
        builder.Property(e => e.TopFailureCode).HasMaxLength(100);
        builder.Property(e => e.TopFailureReason).HasMaxLength(250);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(e => e.RunId);
        builder.HasIndex(e => e.InstitutionBic);
        builder.HasIndex(e => e.ReceivingInstitution);
        builder.HasIndex(e => e.AccountId);
    }
}

public sealed class OracleFailedRejectionTypeAnalyticConfiguration : IEntityTypeConfiguration<OracleFailedRejectionTypeAnalytic>
{
    public void Configure(EntityTypeBuilder<OracleFailedRejectionTypeAnalytic> builder)
    {
        builder.Ignore(e => e.DomainEvents);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.SendingInstitution).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ReceivingInstitution).HasMaxLength(20).IsRequired();
        builder.Property(e => e.RejectionCategory).HasMaxLength(50).IsRequired();
        builder.Property(e => e.FailureCode).HasMaxLength(100);
        builder.Property(e => e.FailureReason).HasMaxLength(250);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(e => e.RunId);
        builder.HasIndex(e => e.SendingInstitution);
        builder.HasIndex(e => e.ReceivingInstitution);
        builder.HasIndex(e => e.RejectionCategory);
        builder.HasIndex(e => e.FailureCode);
    }
}

public sealed class OracleFailedTrendAnalyticConfiguration : IEntityTypeConfiguration<OracleFailedTrendAnalytic>
{
    public void Configure(EntityTypeBuilder<OracleFailedTrendAnalytic> builder)
    {
        builder.Ignore(e => e.DomainEvents);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.BucketType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.SendingInstitution).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ReceivingInstitution).HasMaxLength(20).IsRequired();
        builder.Property(e => e.RejectionCategory).HasMaxLength(50).IsRequired();
        builder.Property(e => e.FailureCode).HasMaxLength(100);
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(e => e.RunId);
        builder.HasIndex(e => e.BucketStart);
        builder.HasIndex(e => e.SendingInstitution);
        builder.HasIndex(e => e.ReceivingInstitution);
        builder.HasIndex(e => e.RejectionCategory);
    }
}
