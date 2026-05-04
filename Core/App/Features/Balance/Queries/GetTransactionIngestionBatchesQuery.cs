using SPSS.App.Common;
using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Queries;

[Authorize(Roles = QueryTransactions)]
public sealed record GetTransactionIngestionBatchesQuery(
    string? BatchType,
    string? Status,
    DateTimeOffset? From,
    DateTimeOffset? To) : PaginationFilter, IRequest<PagedResponse<TransactionIngestionBatchDto>>;

public sealed class GetTransactionIngestionBatchesQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetTransactionIngestionBatchesQuery, PagedResponse<TransactionIngestionBatchDto>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<PagedResponse<TransactionIngestionBatchDto>> Handle(
        GetTransactionIngestionBatchesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _storage.TransactionIngestionBatches.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.BatchType))
        {
            query = query.Where(x => x.BatchType == request.BatchType);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(x => x.Status == request.Status);
        }

        if (request.From.HasValue)
        {
            query = query.Where(x => x.ToDate > request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(x => x.FromDate < request.To.Value);
        }

        var result = query
            .OrderByDescending(x => x.ReceivedAt)
            .Select(x => new TransactionIngestionBatchDto
            {
                Id = x.Id,
                BatchType = x.BatchType,
                Status = x.Status,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
                ReceivedAt = x.ReceivedAt,
                ProcessedAt = x.ProcessedAt,
                SnapshotType = x.SnapshotType,
                SnapshotStatus = x.SnapshotStatus,
                TotalCount = x.TotalCount,
                ReceivedCount = x.ReceivedCount,
                UniqueCount = x.UniqueCount,
                InsertedCount = x.InsertedCount,
                UpdatedCount = x.UpdatedCount,
                DuplicateCount = x.DuplicateCount,
                RejectedCount = x.RejectedCount,
                ErrorMessage = x.ErrorMessage,
                SampleErrors = x.SampleErrors
            });

        return await PaginationHelper<TransactionIngestionBatchDto>.CreateAsync(result, request, cancellationToken);
    }
}
