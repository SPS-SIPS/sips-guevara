using SPSS.App.Common;
using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Queries;

[Authorize(Roles = QueryTransactions)]
public sealed record GetTransactionsQuery(
    string? Kind,
    string? BankFrom,
    string? BankTo,
    DateTimeOffset? CreatedDate,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? TxId,
    string? Type,
    string? Status,
    long? SttlId,
    string? DbtrAcctId,
    string? CdtrAcctId,
    string? LocalInstrument,
    string? CategoryPurpose,
    string? TxSts,
    string? Error,
    string? FailureReason) : PaginationFilter, IRequest<PagedResponse<TransactionRecordDto>>;

public sealed class GetTransactionsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetTransactionsQuery, PagedResponse<TransactionRecordDto>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<PagedResponse<TransactionRecordDto>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var kind = NormalizeKind(request.Kind);

        IQueryable<TransactionRecordDto>? query = null;

        if (kind is "oracle" or "all")
        {
            query = ApplyOracleFilters(_storage.OracleTransactions.AsNoTracking(), request)
                .Select(x => new TransactionRecordDto
                {
                    Source = x.Source,
                    Id = x.OracleId,
                    ParentId = x.ParentId,
                    BankFrom = x.BankFrom,
                    BankTo = x.BankTo,
                    CreatedDate = x.CreatedAt,
                    ModifiedDate = x.ModifiedAt,
                    LastModifiedDate = x.LastModifiedAt,
                    AcceptanceDate = x.AcceptanceAt,
                    CreatedDateRaw = x.CreatedDate,
                    ModifiedDateRaw = x.ModifiedDate,
                    LastModifiedDateRaw = x.LastModifiedDate,
                    AcceptanceDateRaw = x.AcceptanceDate,
                    TxId = x.TxId,
                    Type = x.Type,
                    TypeDescription = x.TypeDescription,
                    Amount = x.Amount,
                    AmountValue = x.AmountValue,
                    Status = x.Status,
                    TxSts = x.TxSts,
                    Description = x.Description,
                    SttlId = x.SttlId,
                    Currency = x.Currency,
                    IntrBkSttlmAtm = x.IntrBkSttlmAtm,
                    IntrBkSttlmAtmValue = x.IntrBkSttlmAtmValue,
                    IntrBkSttlmCurr = x.IntrBkSttlmCurr,
                    DbtrAcctId = x.DbtrAcctId,
                    DbtrAcctType = x.DbtrAcctType,
                    DbtrName = x.DbtrName,
                    DbtrAddress = x.DbtrAddress,
                    CdtrAcctId = x.CdtrAcctId,
                    CdtrAcctType = x.CdtrAcctType,
                    CdtrName = x.CdtrName,
                    CdtrAddress = x.CdtrAddress,
                    RequestId = x.RequestId,
                    TransCode = x.TransCode,
                    Error = x.Error,
                    ErrDesc = x.ErrDesc,
                    ErrorReason = x.ErrorReason,
                    FailureCode = x.Error,
                    FailureDescription = x.ErrDesc,
                    FailureReason = x.ErrorReason ?? x.StsRsnInfAddtlInf ?? x.ErrDesc ?? x.Error,
                    LocalInstrument = x.LclInstrm,
                    CategoryPurpose = x.CtgyPurp,
                    LclInstrm = x.LclInstrm,
                    CtgyPurp = x.CtgyPurp,
                    RtrId = x.RtrId,
                    OldBalance = x.OldBalance,
                    NewBalance = x.NewBalance,
                    BankFromSaPosition = x.BankFromSaPosition,
                    BankToSaPosition = x.BankToSaPosition,
                    BankFromWaPosition = x.BankFromWaPosition,
                    BankToWaPosition = x.BankToWaPosition,
                    EndToEndId = x.EndToEndId,
                    Iban = x.Iban,
                    RtpId = x.RtpId,
                    AttributesRaw = x.AttributesRaw,
                    OrgTrxId = x.OrgTrxId,
                    StsRsnInfRsn = x.StsRsnInfRsn,
                    StsRsnInfAddtlInf = x.StsRsnInfAddtlInf,
                    MandateReqId = x.MandateReqId,
                    RawPayload = x.RawPayload,
                    ReceivedAt = x.ReceivedAt
                });
        }

        if (kind is "all" or "legacy" or "failed")
        {
            var failedQuery = ApplyFailedFilters(_storage.FailedTransactions.AsNoTracking(), request)
                .Select(x => new TransactionRecordDto
                {
                    Source = "failed",
                    Id = x.Id,
                    BankFrom = x.BankFrom,
                    BankFromName = x.BankFromName,
                    BankTo = x.BankTo,
                    BankToName = x.BankToName,
                    CreatedDate = x.CreatedDate,
                    ModifiedDate = x.ModifiedDate,
                    AcceptanceDate = x.AcceptanceDate,
                    TxId = x.TxId,
                    Type = x.Type,
                    Amount = x.Amount,
                    Status = x.Status,
                    Description = x.Description,
                    SttlId = x.SttlId,
                    Currency = x.Currency,
                    IntrBkSttlmAtm = x.IntrBkSttlmAtm,
                    DbtrAcctId = x.DbtrAcctId,
                    DbtrAcctType = x.DbtrAcctType,
                    DbtrName = x.DbtrName,
                    DbtrAddress = x.DbtrAddress,
                    CdtrAcctId = x.CdtrAcctId,
                    CdtrName = x.CdtrName,
                    CdtrAddress = x.CdtrAddress,
                    RequestId = x.RequestId,
                    TransCode = x.TransCode,
                    Error = x.Error,
                    ErrorReason = x.ErrorReason,
                    FailureCode = x.Error,
                    FailureReason = x.ErrorReason ?? x.Error,
                    ReceivedAt = null
                });

            query = query is null ? failedQuery : query.Concat(failedQuery);
        }

        if (kind is "all" or "legacy" or "processed")
        {
            var processedQuery = ApplyProcessedFilters(_storage.ProcessedTransactions.AsNoTracking(), request)
                .Select(x => new TransactionRecordDto
                {
                    Source = "processed",
                    Id = x.Id,
                    BankFrom = x.BankFrom,
                    BankFromName = x.BankFromName,
                    BankTo = x.BankTo,
                    BankToName = x.BankToName,
                    CreatedDate = x.CreatedDate,
                    ModifiedDate = x.ModifiedDate,
                    AcceptanceDate = x.AcceptanceDate,
                    TxId = x.TxId,
                    Type = x.Type,
                    Amount = x.Amount,
                    Status = x.Status,
                    Description = x.Description,
                    SttlId = x.SttlId,
                    Currency = x.Currency,
                    IntrBkSttlmAtm = x.IntrBkSttlmAtm,
                    DbtrAcctId = x.DbtrAcctId,
                    DbtrAcctType = x.DbtrAcctType,
                    DbtrName = x.DbtrName,
                    DbtrAddress = x.DbtrAddress,
                    CdtrAcctId = x.CdtrAcctId,
                    CdtrName = x.CdtrName,
                    CdtrAddress = x.CdtrAddress,
                    RequestId = x.RequestId,
                    TransCode = x.TransCode,
                    Error = null,
                    ErrorReason = null,
                    FailureCode = null,
                    FailureReason = null,
                    ReceivedAt = x.ReceivedAt
                });

            query = query is null ? processedQuery : query.Concat(processedQuery);
        }

        query ??= Enumerable.Empty<TransactionRecordDto>().AsQueryable();
        query = query.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id);

        return await PaginationHelper<TransactionRecordDto>.CreateAsync(query, request, cancellationToken);
    }

    private static string NormalizeKind(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return "oracle";
        }

        return kind.Trim().ToLowerInvariant() switch
        {
            "all" or "both" => "all",
            "oracle" or "new" => "oracle",
            "legacy" => "legacy",
            "failed" or "declined" => "failed",
            "processed" or "succeeded" or "successful" => "processed",
            _ => throw new BadRequestException("Kind must be one of: oracle, all, legacy, failed, processed")
        };
    }

    private static IQueryable<OracleTransaction> ApplyOracleFilters(
        IQueryable<OracleTransaction> query,
        GetTransactionsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.BankFrom))
        {
            query = query.Where(x => x.BankFrom == request.BankFrom);
        }

        if (!string.IsNullOrWhiteSpace(request.BankTo))
        {
            query = query.Where(x => x.BankTo == request.BankTo);
        }

        if (request.CreatedDate.HasValue)
        {
            var from = new DateTimeOffset(request.CreatedDate.Value.Date, request.CreatedDate.Value.Offset);
            var to = from.AddDays(1);
            query = query.Where(x => x.CreatedAt >= from && x.CreatedAt < to);
        }

        if (request.From.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(x => x.CreatedAt < request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TxId))
        {
            query = query.Where(x => x.TxId == request.TxId);
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(x => x.Type == request.Type);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(x => x.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.TxSts))
        {
            query = query.Where(x => x.TxSts == request.TxSts);
        }

        if (!string.IsNullOrWhiteSpace(request.LocalInstrument))
        {
            query = query.Where(x => x.LclInstrm == request.LocalInstrument);
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryPurpose))
        {
            query = query.Where(x => x.CtgyPurp == request.CategoryPurpose);
        }

        if (!string.IsNullOrWhiteSpace(request.Error))
        {
            query = query.Where(x => x.Error == request.Error);
        }

        if (!string.IsNullOrWhiteSpace(request.FailureReason))
        {
            query = query.Where(x =>
                x.ErrorReason == request.FailureReason ||
                x.StsRsnInfAddtlInf == request.FailureReason ||
                x.ErrDesc == request.FailureReason ||
                x.Error == request.FailureReason);
        }

        if (request.SttlId.HasValue)
        {
            query = query.Where(x => x.SttlId == request.SttlId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.DbtrAcctId))
        {
            query = query.Where(x => x.DbtrAcctId == request.DbtrAcctId);
        }

        if (!string.IsNullOrWhiteSpace(request.CdtrAcctId))
        {
            query = query.Where(x => x.CdtrAcctId == request.CdtrAcctId);
        }

        return query;
    }

    private static IQueryable<FailedTransaction> ApplyFailedFilters(
        IQueryable<FailedTransaction> query,
        GetTransactionsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.BankFrom))
        {
            query = query.Where(x => x.BankFrom == request.BankFrom);
        }

        if (!string.IsNullOrWhiteSpace(request.BankTo))
        {
            query = query.Where(x => x.BankTo == request.BankTo);
        }

        if (request.CreatedDate.HasValue)
        {
            var from = new DateTimeOffset(request.CreatedDate.Value.Date, request.CreatedDate.Value.Offset);
            var to = from.AddDays(1);
            query = query.Where(x => x.CreatedDate >= from && x.CreatedDate < to);
        }

        if (request.From.HasValue)
        {
            query = query.Where(x => x.CreatedDate >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(x => x.CreatedDate < request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TxId))
        {
            query = query.Where(x => x.TxId == request.TxId);
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(x => x.Type == request.Type);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(x => x.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.Error))
        {
            query = query.Where(x => x.Error == request.Error);
        }

        if (!string.IsNullOrWhiteSpace(request.FailureReason))
        {
            query = query.Where(x => x.ErrorReason == request.FailureReason || x.Error == request.FailureReason);
        }

        if (request.SttlId.HasValue)
        {
            query = query.Where(x => x.SttlId == request.SttlId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.DbtrAcctId))
        {
            query = query.Where(x => x.DbtrAcctId == request.DbtrAcctId);
        }

        if (!string.IsNullOrWhiteSpace(request.CdtrAcctId))
        {
            query = query.Where(x => x.CdtrAcctId == request.CdtrAcctId);
        }

        return query;
    }

    private static IQueryable<ProcessedTransaction> ApplyProcessedFilters(
        IQueryable<ProcessedTransaction> query,
        GetTransactionsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.BankFrom))
        {
            query = query.Where(x => x.BankFrom == request.BankFrom);
        }

        if (!string.IsNullOrWhiteSpace(request.BankTo))
        {
            query = query.Where(x => x.BankTo == request.BankTo);
        }

        if (request.CreatedDate.HasValue)
        {
            var from = new DateTimeOffset(request.CreatedDate.Value.Date, request.CreatedDate.Value.Offset);
            var to = from.AddDays(1);
            query = query.Where(x => x.CreatedDate >= from && x.CreatedDate < to);
        }

        if (request.From.HasValue)
        {
            query = query.Where(x => x.CreatedDate >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(x => x.CreatedDate < request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.TxId))
        {
            query = query.Where(x => x.TxId == request.TxId);
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(x => x.Type == request.Type);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(x => x.Status == request.Status);
        }

        if (request.SttlId.HasValue)
        {
            query = query.Where(x => x.SttlId == request.SttlId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.DbtrAcctId))
        {
            query = query.Where(x => x.DbtrAcctId == request.DbtrAcctId);
        }

        if (!string.IsNullOrWhiteSpace(request.CdtrAcctId))
        {
            query = query.Where(x => x.CdtrAcctId == request.CdtrAcctId);
        }

        return query;
    }
}
