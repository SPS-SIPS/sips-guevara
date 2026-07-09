using SPSS.App.Common;
using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Queries;

[Authorize(Roles = QueryTransactions)]
public sealed record GetTransactionsQuery(
    string? Kind,
    string? Bank,
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
    string? FailureReason,
    string? FraudZone,
    string? FraudRule) : PaginationFilter, IRequest<PagedResponse<TransactionRecordDto>>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetTransactionsSummaryQuery(
    string? Kind,
    string? Bank,
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
    string? FailureReason,
    string? FraudZone,
    string? FraudRule) : IRequest<Response<TransactionSummaryDto>>;

public sealed class GetTransactionsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetTransactionsQuery, PagedResponse<TransactionRecordDto>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<PagedResponse<TransactionRecordDto>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var kind = NormalizeKind(request.Kind);

        var fraudRows = _storage.OracleFraudTransactions.AsNoTracking();
        var oracleQuery = ProjectOracle(
            ApplyOracleFraudFilters(
                ApplyOracleFilters(_storage.OracleTransactions.AsNoTracking(), request),
                fraudRows,
                request),
            fraudRows);
        var failedQuery = ProjectFailed(ApplyFailedFilters(_storage.FailedTransactions.AsNoTracking(), request));
        var processedQuery = ProjectProcessed(ApplyProcessedFilters(_storage.ProcessedTransactions.AsNoTracking(), request));

        if (kind is "oracle")
        {
            return await PaginationHelper<TransactionRecordDto>.CreateAsync(
                oracleQuery.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id),
                request,
                cancellationToken);
        }

        if (kind is "failed")
        {
            return await PaginationHelper<TransactionRecordDto>.CreateAsync(
                failedQuery.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id),
                request,
                cancellationToken);
        }

        if (kind is "processed")
        {
            return await PaginationHelper<TransactionRecordDto>.CreateAsync(
                processedQuery.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id),
                request,
                cancellationToken);
        }

        var queries = kind is "legacy"
            ? new[] { failedQuery, processedQuery }
            : new[] { oracleQuery, failedQuery, processedQuery };

        return await CreateCombinedPageAsync(queries, request, cancellationToken);
    }

    private static async ValueTask<PagedResponse<TransactionRecordDto>> CreateCombinedPageAsync(
        IEnumerable<IQueryable<TransactionRecordDto>> queries,
        PaginationFilter filter,
        CancellationToken cancellationToken)
    {
        var index = filter.Index < 1 ? 1 : filter.Index;
        var size = filter.Size is > 500 or < 1 ? 10 : filter.Size;
        var pageWindow = index * size;
        var count = 0;
        var candidates = new List<TransactionRecordDto>();

        foreach (var query in queries)
        {
            count += await query.CountAsync(cancellationToken);

            var pageCandidates = await query
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.Id)
                .Take(pageWindow)
                .ToListAsync(cancellationToken);

            candidates.AddRange(pageCandidates);
        }

        var items = candidates
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.Id)
            .Skip((index - 1) * size)
            .Take(size)
            .ToList();

        return new PagedResponse<TransactionRecordDto>
        {
            Data = items,
            Index = index,
            Size = size,
            Count = count
        };
    }

    private static IQueryable<TransactionRecordDto> ProjectOracle(
        IQueryable<OracleTransaction> query,
        IQueryable<OracleFraudTransaction> fraudQuery) =>
        query.Select(x => new TransactionRecordDto
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
            EndToEndId = x.EndToEndId,
            Iban = x.Iban,
            RtpId = x.RtpId,
            OrgTrxId = x.OrgTrxId,
            StsRsnInfRsn = x.StsRsnInfRsn,
            StsRsnInfAddtlInf = x.StsRsnInfAddtlInf,
            MandateReqId = x.MandateReqId,
            ReceivedAt = x.ReceivedAt,
            Fraud = fraudQuery
                .Where(f => f.Refnum == x.TxId)
                .OrderByDescending(f => f.Utrnno)
                .ThenByDescending(f => f.ReceivedAt)
                .Select(f => new TransactionFraudContextDto
                {
                    Utrnno = f.Utrnno,
                    RespCode = f.RespCode,
                    FraudZone = f.FraudZone,
                    RulesList = f.RulesList,
                    Rules = f.Rules,
                    Refnum = f.Refnum,
                    RiskValue = f.RiskValue,
                    ReceivedAt = f.ReceivedAt
                })
                .FirstOrDefault()
        });

    private static IQueryable<TransactionRecordDto> ProjectFailed(IQueryable<FailedTransaction> query) =>
        query.Select(x => new TransactionRecordDto
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
            FailureReason = x.ErrorReason ?? x.Error
        });

    private static IQueryable<TransactionRecordDto> ProjectProcessed(IQueryable<ProcessedTransaction> query) =>
        query.Select(x => new TransactionRecordDto
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
            ReceivedAt = x.ReceivedAt
        });

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

    internal static IQueryable<OracleTransaction> ApplyOracleFilters(
        IQueryable<OracleTransaction> query,
        GetTransactionsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Bank))
        {
            query = query.Where(x => x.BankFrom == request.Bank || x.BankTo == request.Bank);
        }

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

    internal static IQueryable<OracleTransaction> ApplyOracleFraudFilters(
        IQueryable<OracleTransaction> query,
        IQueryable<OracleFraudTransaction> fraudQuery,
        GetTransactionsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.FraudZone))
        {
            query = query.Where(x => fraudQuery.Any(f => f.Refnum == x.TxId && f.FraudZone == request.FraudZone));
        }

        if (!string.IsNullOrWhiteSpace(request.FraudRule))
        {
            query = query.Where(x => fraudQuery.Any(f => f.Refnum == x.TxId && f.Rules.Contains(request.FraudRule)));
        }

        return query;
    }

    internal static IQueryable<FailedTransaction> ApplyFailedFilters(
        IQueryable<FailedTransaction> query,
        GetTransactionsQuery request)
    {
        if (HasOracleOnlyFilters(request))
        {
            return query.Where(_ => false);
        }

        if (!string.IsNullOrWhiteSpace(request.Bank))
        {
            query = query.Where(x => x.BankFrom == request.Bank || x.BankTo == request.Bank);
        }

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

    internal static IQueryable<ProcessedTransaction> ApplyProcessedFilters(
        IQueryable<ProcessedTransaction> query,
        GetTransactionsQuery request)
    {
        if (HasOracleOnlyFilters(request) ||
            !string.IsNullOrWhiteSpace(request.Error) ||
            !string.IsNullOrWhiteSpace(request.FailureReason))
        {
            return query.Where(_ => false);
        }

        if (!string.IsNullOrWhiteSpace(request.Bank))
        {
            query = query.Where(x => x.BankFrom == request.Bank || x.BankTo == request.Bank);
        }

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

    internal static bool HasOracleOnlyFilters(GetTransactionsQuery request) =>
        !string.IsNullOrWhiteSpace(request.LocalInstrument) ||
        !string.IsNullOrWhiteSpace(request.CategoryPurpose) ||
        !string.IsNullOrWhiteSpace(request.TxSts) ||
        !string.IsNullOrWhiteSpace(request.FraudZone) ||
        !string.IsNullOrWhiteSpace(request.FraudRule);
}

public sealed class GetTransactionsSummaryQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetTransactionsSummaryQuery, Response<TransactionSummaryDto>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<TransactionSummaryDto>> Handle(
        GetTransactionsSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var filter = ToTransactionsQuery(request);
        var kind = NormalizeKind(request.Kind);

        var fraudRows = _storage.OracleFraudTransactions.AsNoTracking();
        var oracleQuery = ProjectOracleSummary(
            GetTransactionsQueryHandler.ApplyOracleFraudFilters(
                GetTransactionsQueryHandler.ApplyOracleFilters(_storage.OracleTransactions.AsNoTracking(), filter),
                fraudRows,
                filter),
            fraudRows);
        var failedQuery = ProjectFailedSummary(GetTransactionsQueryHandler.ApplyFailedFilters(_storage.FailedTransactions.AsNoTracking(), filter));
        var processedQuery = ProjectProcessedSummary(GetTransactionsQueryHandler.ApplyProcessedFilters(_storage.ProcessedTransactions.AsNoTracking(), filter));

        var queries = kind switch
        {
            "oracle" => new[] { oracleQuery },
            "failed" => new[] { failedQuery },
            "processed" => new[] { processedQuery },
            "legacy" => new[] { failedQuery, processedQuery },
            _ => new[] { oracleQuery, failedQuery, processedQuery }
        };

        var summary = new TransactionSummaryDto();
        var rejectedByCode = new Dictionary<string, TransactionRejectionCodeSummaryDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var query in queries)
        {
            var aggregate = await query
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalTransactions = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount),
                    RejectedTransactions = g.Count(x => x.IsRejected),
                    RejectedAmount = g.Where(x => x.IsRejected).Sum(x => x.Amount),
                    FraudMatchedTransactions = g.Count(x => x.HasFraud)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (aggregate is not null)
            {
                summary.TotalTransactions += aggregate.TotalTransactions;
                summary.TotalAmount += aggregate.TotalAmount;
                summary.RejectedTransactions += aggregate.RejectedTransactions;
                summary.RejectedAmount += aggregate.RejectedAmount;
                summary.FraudMatchedTransactions += aggregate.FraudMatchedTransactions;
            }

            var groups = await query
                .Where(x => x.IsRejected)
                .GroupBy(x => x.RejectionCode)
                .Select(g => new TransactionRejectionCodeSummaryDto
                {
                    Code = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(x => x.Amount)
                })
                .ToListAsync(cancellationToken);

            foreach (var group in groups)
            {
                var code = string.IsNullOrWhiteSpace(group.Code) ? "UNKNOWN" : group.Code;
                if (!rejectedByCode.TryGetValue(code, out var existing))
                {
                    rejectedByCode[code] = new TransactionRejectionCodeSummaryDto
                    {
                        Code = code,
                        Count = group.Count,
                        Amount = group.Amount
                    };
                    continue;
                }

                existing.Count += group.Count;
                existing.Amount += group.Amount;
            }
        }

        summary.RejectedByCode = rejectedByCode.Values
            .OrderByDescending(x => x.Amount)
            .ThenByDescending(x => x.Count)
            .ThenBy(x => x.Code)
            .ToList();

        return new Response<TransactionSummaryDto>(summary);
    }

    private static GetTransactionsQuery ToTransactionsQuery(GetTransactionsSummaryQuery request) =>
        new(
            request.Kind,
            request.Bank,
            request.BankFrom,
            request.BankTo,
            request.CreatedDate,
            request.From,
            request.To,
            request.TxId,
            request.Type,
            request.Status,
            request.SttlId,
            request.DbtrAcctId,
            request.CdtrAcctId,
            request.LocalInstrument,
            request.CategoryPurpose,
            request.TxSts,
            request.Error,
            request.FailureReason,
            request.FraudZone,
            request.FraudRule);

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

    private static IQueryable<TransactionSummaryProjection> ProjectOracleSummary(
        IQueryable<OracleTransaction> query,
        IQueryable<OracleFraudTransaction> fraudQuery) =>
        query.Select(x => new TransactionSummaryProjection
        {
            Amount = x.AmountValue ?? x.IntrBkSttlmAtmValue ?? 0,
            IsRejected = x.Status == "2" || x.TxSts == "RJCT" || x.Status == "REJECTED" || x.Status == "DECLINED" || x.Status == "FAILED",
            RejectionCode = x.Error ?? x.StsRsnInfRsn ?? x.TxSts ?? x.Status ?? "UNKNOWN",
            HasFraud = fraudQuery.Any(f => f.Refnum == x.TxId)
        });

    private static IQueryable<TransactionSummaryProjection> ProjectFailedSummary(IQueryable<FailedTransaction> query) =>
        query.Select(x => new TransactionSummaryProjection
        {
            Amount = 0,
            IsRejected = true,
            RejectionCode = x.Error ?? x.ErrorReason ?? x.Status ?? "UNKNOWN",
            HasFraud = false
        });

    private static IQueryable<TransactionSummaryProjection> ProjectProcessedSummary(IQueryable<ProcessedTransaction> query) =>
        query.Select(x => new TransactionSummaryProjection
        {
            Amount = 0,
            IsRejected = false,
            RejectionCode = "NONE",
            HasFraud = false
        });

    private sealed class TransactionSummaryProjection
    {
        public decimal Amount { get; set; }
        public bool IsRejected { get; set; }
        public string RejectionCode { get; set; } = string.Empty;
        public bool HasFraud { get; set; }
    }
}
