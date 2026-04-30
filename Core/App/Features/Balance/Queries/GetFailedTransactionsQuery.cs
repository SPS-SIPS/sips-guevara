using SPSS.App.Common;
using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Queries;

[Authorize(Roles = QueryTransactions)]
public sealed record GetFailedTransactionsQuery(
    string? BankFrom,
    string? BankTo,
    DateTimeOffset? CreatedDate,
    string? TxId,
    string? Status,
    long? SttlId,
    string? DbtrAcctId,
    string? CdtrAcctId) : PaginationFilter, IRequest<PagedResponse<FailedTransactionRecordDto>>;

public sealed class GetFailedTransactionsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetFailedTransactionsQuery, PagedResponse<FailedTransactionRecordDto>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<PagedResponse<FailedTransactionRecordDto>> Handle(GetFailedTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.FailedTransactions.AsNoTracking();

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

        if (!string.IsNullOrWhiteSpace(request.TxId))
        {
            query = query.Where(x => x.TxId == request.TxId);
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

        query = query.OrderByDescending(x => x.CreatedDate);

        var result = query.Select(x => new FailedTransactionRecordDto
        {
            Id = x.Id,
            IpsTransactionId = x.IpsTransactionId,
            SnapshotTimestamp = x.SnapshotTimestamp,
            SnapshotFromDate = x.SnapshotFromDate,
            SnapshotToDate = x.SnapshotToDate,
            ReceivedAt = x.ReceivedAt,
            RuleIds = x.RuleIds,
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
            ErrorReason = x.ErrorReason
        });

        return await PaginationHelper<FailedTransactionRecordDto>.CreateAsync(result, request, cancellationToken);
    }
}
