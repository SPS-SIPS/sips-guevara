namespace SPSS.Domain.Models.BL;

public sealed class OracleTransaction : BaseEntity<long>
{
    public string Source { get; set; } = string.Empty;
    public long OracleId { get; set; }
    public long? ParentId { get; set; }
    public string BankFrom { get; set; } = string.Empty;
    public string BankTo { get; set; } = string.Empty;
    public string? CreatedDate { get; set; }
    public string? ModifiedDate { get; set; }
    public string? LastModifiedDate { get; set; }
    public string? AcceptanceDate { get; set; }
    public string TxId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? TypeDescription { get; set; }
    public string? Amount { get; set; }
    public decimal? AmountValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TxSts { get; set; }
    public string? Description { get; set; }
    public string? Currency { get; set; }
    public string? IntrBkSttlmAtm { get; set; }
    public decimal? IntrBkSttlmAtmValue { get; set; }
    public string? IntrBkSttlmCurr { get; set; }
    public string? DbtrAcctId { get; set; }
    public string? DbtrAcctType { get; set; }
    public string? DbtrName { get; set; }
    public string? DbtrAddress { get; set; }
    public string? CdtrName { get; set; }
    public string? CdtrAddress { get; set; }
    public string? CdtrAcctId { get; set; }
    public string? CdtrAcctType { get; set; }
    public long? SttlId { get; set; }
    public string? Error { get; set; }
    public string? ErrDesc { get; set; }
    public string? ErrorReason { get; set; }
    public string? RequestId { get; set; }
    public string? TransCode { get; set; }
    public string? LclInstrm { get; set; }
    public string? CtgyPurp { get; set; }
    public string? RtrId { get; set; }
    public string? OldBalance { get; set; }
    public string? NewBalance { get; set; }
    public string? BankFromSaPosition { get; set; }
    public string? BankToSaPosition { get; set; }
    public string? BankFromWaPosition { get; set; }
    public string? BankToWaPosition { get; set; }
    public string? EndToEndId { get; set; }
    public string? Iban { get; set; }
    public string? RtpId { get; set; }
    public string? AttributesRaw { get; set; }
    public string? OrgTrxId { get; set; }
    public string? StsRsnInfRsn { get; set; }
    public string? StsRsnInfAddtlInf { get; set; }
    public string? MandateReqId { get; set; }
    public string? TransactionRaw { get; set; }
    public string? TransactionsRaw { get; set; }
    public string? MessagesRaw { get; set; }
    public string? RawPayload { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OracleTransactionConfiguration : IEntityTypeConfiguration<OracleTransaction>
{
    public void Configure(EntityTypeBuilder<OracleTransaction> builder)
    {
        builder.Ignore(e => e.DomainEvents);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Source).HasMaxLength(50).IsRequired();
        builder.Property(e => e.BankFrom).HasMaxLength(20).IsRequired();
        builder.Property(e => e.BankTo).HasMaxLength(20).IsRequired();
        builder.Property(e => e.CreatedDate).HasMaxLength(40);
        builder.Property(e => e.ModifiedDate).HasMaxLength(40);
        builder.Property(e => e.LastModifiedDate).HasMaxLength(40);
        builder.Property(e => e.AcceptanceDate).HasMaxLength(40);
        builder.Property(e => e.TxId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TypeDescription).HasMaxLength(100);
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TxSts).HasMaxLength(50);
        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.IntrBkSttlmCurr).HasMaxLength(10);
        builder.Property(e => e.RequestId).HasMaxLength(100);
        builder.Property(e => e.TransCode).HasMaxLength(50);
        builder.Property(e => e.Error).HasMaxLength(100);
        builder.Property(e => e.ErrDesc).HasMaxLength(250);
        builder.Property(e => e.ErrorReason).HasMaxLength(250);
        builder.Property(e => e.AmountValue).HasPrecision(18, 2);
        builder.Property(e => e.IntrBkSttlmAtmValue).HasPrecision(18, 2);

        builder.HasIndex(e => new { e.Source, e.OracleId }).IsUnique();
        builder.HasIndex(e => e.TxId);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.TxSts);
        builder.HasIndex(e => e.BankFrom);
        builder.HasIndex(e => e.BankTo);
        builder.HasIndex(e => e.RequestId);
        builder.HasIndex(e => e.ReceivedAt);
    }
}
