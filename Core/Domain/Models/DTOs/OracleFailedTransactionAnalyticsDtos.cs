namespace SPSS.Domain.Models.DTOs;

public sealed class OracleFailedTransactionAnalyticsRunDto
{
    public long Id { get; set; }
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

public sealed class OracleFailedInstitutionAnalyticDto
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

public sealed class OracleFailedAccountAnalyticDto
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

public sealed class OracleFailedRejectionTypeAnalyticDto
{
    public long RunId { get; set; }
    public string SendingInstitution { get; set; } = string.Empty;
    public string ReceivingInstitution { get; set; } = string.Empty;
    public string FailureCode { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class OracleFailedTrendAnalyticDto
{
    public long RunId { get; set; }
    public DateTimeOffset BucketStart { get; set; }
    public string BucketType { get; set; } = "DAY";
    public string SendingInstitution { get; set; } = string.Empty;
    public string ReceivingInstitution { get; set; } = string.Empty;
    public string FailureCode { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}
