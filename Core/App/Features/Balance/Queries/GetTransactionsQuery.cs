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

        var oracleQuery = ProjectOracle(ApplyOracleFilters(_storage.OracleTransactions.AsNoTracking(), request));
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

    private static IQueryable<TransactionRecordDto> ProjectOracle(IQueryable<OracleTransaction> query) =>
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
            ReceivedAt = x.ReceivedAt
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
        if (HasOracleOnlyFilters(request))
        {
            return query.Where(_ => false);
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

    private static IQueryable<ProcessedTransaction> ApplyProcessedFilters(
        IQueryable<ProcessedTransaction> query,
        GetTransactionsQuery request)
    {
        if (HasOracleOnlyFilters(request) ||
            !string.IsNullOrWhiteSpace(request.Error) ||
            !string.IsNullOrWhiteSpace(request.FailureReason))
        {
            return query.Where(_ => false);
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

    private static bool HasOracleOnlyFilters(GetTransactionsQuery request) =>
        !string.IsNullOrWhiteSpace(request.LocalInstrument) ||
        !string.IsNullOrWhiteSpace(request.CategoryPurpose) ||
        !string.IsNullOrWhiteSpace(request.TxSts);
}
