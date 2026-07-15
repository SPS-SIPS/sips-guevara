using SPSS.App.Common;
using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Queries;

[Authorize(Roles = QueryTransactions)]
public sealed record GetOracleFailedAnalyticsRunsQuery(
    string? Status,
    DateTimeOffset? From,
    DateTimeOffset? To) : PaginationFilter, IRequest<PagedResponse<OracleFailedTransactionAnalyticsRunDto>>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetOracleFailedAnalyticsSummaryQuery(
    long? RunId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? SendingInstitution,
    string? ReceivingInstitution,
    string? RejectionCategory,
    string? FailureCode) : IRequest<Response<OracleFailedTransactionAnalyticsSummaryDto>>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetOracleFailedInstitutionAnalyticsQuery(
    long? RunId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? SendingInstitution,
    string? ReceivingInstitution) : PaginationFilter, IRequest<PagedResponse<OracleFailedInstitutionAnalyticDto>>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetOracleFailedAccountAnalyticsQuery(
    long? RunId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? InstitutionBic,
    string? ReceivingInstitution,
    string? AccountId) : PaginationFilter, IRequest<PagedResponse<OracleFailedAccountAnalyticDto>>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetOracleFailedRejectionTypeAnalyticsQuery(
    long? RunId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? SendingInstitution,
    string? ReceivingInstitution,
    string? RejectionCategory,
    string? FailureCode) : PaginationFilter, IRequest<PagedResponse<OracleFailedRejectionTypeAnalyticDto>>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetOracleFailedTrendAnalyticsQuery(
    long? RunId,
    string? SendingInstitution,
    string? ReceivingInstitution,
    string? RejectionCategory,
    string? FailureCode,
    DateTimeOffset? From,
    DateTimeOffset? To) : PaginationFilter, IRequest<PagedResponse<OracleFailedTrendAnalyticDto>>;

public sealed class GetOracleFailedAnalyticsRunsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetOracleFailedAnalyticsRunsQuery, PagedResponse<OracleFailedTransactionAnalyticsRunDto>>
{
    public async ValueTask<PagedResponse<OracleFailedTransactionAnalyticsRunDto>> Handle(
        GetOracleFailedAnalyticsRunsQuery request,
        CancellationToken cancellationToken)
    {
        var query = storage.OracleFailedTransactionAnalyticsRuns.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(x => x.Status == request.Status);
        }

        query = OracleFailedAnalyticsQueryHelpers.ApplyRunDateFilter(query, request.From, request.To);

        return await PaginationHelper<OracleFailedTransactionAnalyticsRunDto>.CreateAsync(
            query
                .OrderByDescending(x => x.GeneratedAt)
                .Select(x => new OracleFailedTransactionAnalyticsRunDto
                {
                    Id = x.Id,
                    WindowStart = x.WindowStart,
                    WindowEnd = x.WindowEnd,
                    GeneratedAt = x.GeneratedAt,
                    Status = x.Status,
                    TotalTransactions = x.TotalTransactions,
                    TotalAmount = x.TotalAmount,
                    UniqueSendingInstitutions = x.UniqueSendingInstitutions,
                    UniqueReceivingInstitutions = x.UniqueReceivingInstitutions,
                    UniqueAccounts = x.UniqueAccounts,
                    ErrorMessage = x.ErrorMessage
                }),
            request,
            cancellationToken);
    }
}

public sealed class GetOracleFailedAnalyticsSummaryQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetOracleFailedAnalyticsSummaryQuery, Response<OracleFailedTransactionAnalyticsSummaryDto>>
{
    public async ValueTask<Response<OracleFailedTransactionAnalyticsSummaryDto>> Handle(
        GetOracleFailedAnalyticsSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var runIds = await OracleFailedAnalyticsQueryHelpers.ResolveRunIdsAsync(storage, request.RunId, request.From, request.To, cancellationToken);
        var rejectionQuery = storage.OracleFailedRejectionTypeAnalytics.AsNoTracking().Where(x => runIds.Contains(x.RunId));
        var accountQuery = storage.OracleFailedAccountAnalytics.AsNoTracking().Where(x => runIds.Contains(x.RunId));

        if (!string.IsNullOrWhiteSpace(request.SendingInstitution))
        {
            rejectionQuery = rejectionQuery.Where(x => x.SendingInstitution == request.SendingInstitution);
            accountQuery = accountQuery.Where(x => x.InstitutionBic == request.SendingInstitution);
        }

        if (!string.IsNullOrWhiteSpace(request.ReceivingInstitution))
        {
            rejectionQuery = rejectionQuery.Where(x => x.ReceivingInstitution == request.ReceivingInstitution);
            accountQuery = accountQuery.Where(x => x.ReceivingInstitution == request.ReceivingInstitution);
        }

        if (!string.IsNullOrWhiteSpace(request.RejectionCategory))
        {
            rejectionQuery = rejectionQuery.Where(x => x.RejectionCategory == request.RejectionCategory);
        }

        if (!string.IsNullOrWhiteSpace(request.FailureCode))
        {
            rejectionQuery = rejectionQuery.Where(x => x.FailureCode == request.FailureCode);
        }

        var summary = new OracleFailedTransactionAnalyticsSummaryDto
        {
            TotalTransactions = await rejectionQuery.SumAsync(x => (int?)x.TransactionCount, cancellationToken) ?? 0,
            TotalAmount = await rejectionQuery.SumAsync(x => (decimal?)x.TotalAmount, cancellationToken) ?? 0,
            UniqueSendingInstitutions = await rejectionQuery.Select(x => x.SendingInstitution).Where(x => x != string.Empty).Distinct().CountAsync(cancellationToken),
            UniqueReceivingInstitutions = await rejectionQuery.Select(x => x.ReceivingInstitution).Where(x => x != string.Empty).Distinct().CountAsync(cancellationToken),
            UniqueAccounts = await accountQuery.Select(x => x.AccountId).Where(x => x != string.Empty).Distinct().CountAsync(cancellationToken)
        };

        return new Response<OracleFailedTransactionAnalyticsSummaryDto>(summary);
    }
}

public sealed class GetOracleFailedInstitutionAnalyticsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetOracleFailedInstitutionAnalyticsQuery, PagedResponse<OracleFailedInstitutionAnalyticDto>>
{
    public async ValueTask<PagedResponse<OracleFailedInstitutionAnalyticDto>> Handle(
        GetOracleFailedInstitutionAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var runIds = await OracleFailedAnalyticsQueryHelpers.ResolveRunIdsAsync(storage, request.RunId, request.From, request.To, cancellationToken);
        var query = storage.OracleFailedInstitutionAnalytics.AsNoTracking().Where(x => runIds.Contains(x.RunId));

        if (!string.IsNullOrWhiteSpace(request.SendingInstitution))
        {
            query = query.Where(x => x.SendingInstitution == request.SendingInstitution);
        }

        if (!string.IsNullOrWhiteSpace(request.ReceivingInstitution))
        {
            query = query.Where(x => x.ReceivingInstitution == request.ReceivingInstitution);
        }

        return await PaginationHelper<OracleFailedInstitutionAnalyticDto>.CreateAsync(
            query
                .GroupBy(x => new { x.SendingInstitution, x.ReceivingInstitution })
                .Select(group => new OracleFailedInstitutionAnalyticDto
                {
                    RunId = 0,
                    SendingInstitution = group.Key.SendingInstitution,
                    ReceivingInstitution = group.Key.ReceivingInstitution,
                    TransactionCount = group.Sum(x => x.TransactionCount),
                    TotalAmount = group.Sum(x => x.TotalAmount),
                    DistinctDebtorAccounts = group.Sum(x => x.DistinctDebtorAccounts),
                    TopFailureCode = group.OrderByDescending(x => x.TransactionCount).Select(x => x.TopFailureCode).FirstOrDefault() ?? string.Empty,
                    TopFailureReason = group.OrderByDescending(x => x.TransactionCount).Select(x => x.TopFailureReason).FirstOrDefault() ?? string.Empty,
                    FirstTransactionAt = group.Min(x => x.FirstTransactionAt),
                    LastTransactionAt = group.Max(x => x.LastTransactionAt)
                })
                .OrderByDescending(x => x.TransactionCount)
                .ThenByDescending(x => x.TotalAmount),
            request,
            cancellationToken);
    }
}

public sealed class GetOracleFailedAccountAnalyticsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetOracleFailedAccountAnalyticsQuery, PagedResponse<OracleFailedAccountAnalyticDto>>
{
    public async ValueTask<PagedResponse<OracleFailedAccountAnalyticDto>> Handle(
        GetOracleFailedAccountAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var runIds = await OracleFailedAnalyticsQueryHelpers.ResolveRunIdsAsync(storage, request.RunId, request.From, request.To, cancellationToken);
        var query = storage.OracleFailedAccountAnalytics.AsNoTracking().Where(x => runIds.Contains(x.RunId));

        if (!string.IsNullOrWhiteSpace(request.InstitutionBic))
        {
            query = query.Where(x => x.InstitutionBic == request.InstitutionBic);
        }

        if (!string.IsNullOrWhiteSpace(request.ReceivingInstitution))
        {
            query = query.Where(x => x.ReceivingInstitution == request.ReceivingInstitution);
        }

        if (!string.IsNullOrWhiteSpace(request.AccountId))
        {
            query = query.Where(x => x.AccountId == request.AccountId);
        }

        return await PaginationHelper<OracleFailedAccountAnalyticDto>.CreateAsync(
            query
                .GroupBy(x => new { x.InstitutionBic, x.ReceivingInstitution, x.AccountId })
                .Select(group => new OracleFailedAccountAnalyticDto
                {
                    RunId = 0,
                    InstitutionBic = group.Key.InstitutionBic,
                    ReceivingInstitution = group.Key.ReceivingInstitution,
                    AccountId = group.Key.AccountId,
                    TransactionCount = group.Sum(x => x.TransactionCount),
                    TotalAmount = group.Sum(x => x.TotalAmount),
                    TopFailureCode = group.OrderByDescending(x => x.TransactionCount).Select(x => x.TopFailureCode).FirstOrDefault() ?? string.Empty,
                    TopFailureReason = group.OrderByDescending(x => x.TransactionCount).Select(x => x.TopFailureReason).FirstOrDefault() ?? string.Empty,
                    FirstTransactionAt = group.Min(x => x.FirstTransactionAt),
                    LastTransactionAt = group.Max(x => x.LastTransactionAt)
                })
                .OrderByDescending(x => x.TransactionCount)
                .ThenByDescending(x => x.TotalAmount),
            request,
            cancellationToken);
    }
}

public sealed class GetOracleFailedRejectionTypeAnalyticsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetOracleFailedRejectionTypeAnalyticsQuery, PagedResponse<OracleFailedRejectionTypeAnalyticDto>>
{
    public async ValueTask<PagedResponse<OracleFailedRejectionTypeAnalyticDto>> Handle(
        GetOracleFailedRejectionTypeAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var runIds = await OracleFailedAnalyticsQueryHelpers.ResolveRunIdsAsync(storage, request.RunId, request.From, request.To, cancellationToken);
        var query = storage.OracleFailedRejectionTypeAnalytics.AsNoTracking().Where(x => runIds.Contains(x.RunId));

        if (!string.IsNullOrWhiteSpace(request.SendingInstitution))
        {
            query = query.Where(x => x.SendingInstitution == request.SendingInstitution);
        }

        if (!string.IsNullOrWhiteSpace(request.ReceivingInstitution))
        {
            query = query.Where(x => x.ReceivingInstitution == request.ReceivingInstitution);
        }

        if (!string.IsNullOrWhiteSpace(request.RejectionCategory))
        {
            query = query.Where(x => x.RejectionCategory == request.RejectionCategory);
        }

        if (!string.IsNullOrWhiteSpace(request.FailureCode))
        {
            query = query.Where(x => x.FailureCode == request.FailureCode);
        }

        return await PaginationHelper<OracleFailedRejectionTypeAnalyticDto>.CreateAsync(
            query
                .GroupBy(x => new { x.SendingInstitution, x.ReceivingInstitution, x.RejectionCategory, x.FailureCode, x.FailureReason })
                .Select(group => new OracleFailedRejectionTypeAnalyticDto
                {
                    RunId = 0,
                    SendingInstitution = group.Key.SendingInstitution,
                    ReceivingInstitution = group.Key.ReceivingInstitution,
                    RejectionCategory = group.Key.RejectionCategory,
                    FailureCode = group.Key.FailureCode,
                    FailureReason = group.Key.FailureReason,
                    TransactionCount = group.Sum(x => x.TransactionCount),
                    TotalAmount = group.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.TransactionCount)
                .ThenByDescending(x => x.TotalAmount),
            request,
            cancellationToken);
    }
}

public sealed class GetOracleFailedTrendAnalyticsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetOracleFailedTrendAnalyticsQuery, PagedResponse<OracleFailedTrendAnalyticDto>>
{
    public async ValueTask<PagedResponse<OracleFailedTrendAnalyticDto>> Handle(
        GetOracleFailedTrendAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var runIds = await OracleFailedAnalyticsQueryHelpers.ResolveRunIdsAsync(storage, request.RunId, request.From, request.To, cancellationToken);
        var query = storage.OracleFailedTrendAnalytics.AsNoTracking().Where(x => runIds.Contains(x.RunId));

        if (!string.IsNullOrWhiteSpace(request.SendingInstitution))
        {
            query = query.Where(x => x.SendingInstitution == request.SendingInstitution);
        }

        if (!string.IsNullOrWhiteSpace(request.ReceivingInstitution))
        {
            query = query.Where(x => x.ReceivingInstitution == request.ReceivingInstitution);
        }

        if (!string.IsNullOrWhiteSpace(request.RejectionCategory))
        {
            query = query.Where(x => x.RejectionCategory == request.RejectionCategory);
        }

        if (!string.IsNullOrWhiteSpace(request.FailureCode))
        {
            query = query.Where(x => x.FailureCode == request.FailureCode);
        }

        if (request.From.HasValue)
        {
            query = query.Where(x => x.BucketStart >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(x => x.BucketStart < request.To.Value);
        }

        return await PaginationHelper<OracleFailedTrendAnalyticDto>.CreateAsync(
            query
                .GroupBy(x => new { x.BucketStart, x.BucketType, x.SendingInstitution, x.ReceivingInstitution, x.RejectionCategory, x.FailureCode })
                .Select(group => new OracleFailedTrendAnalyticDto
                {
                    RunId = 0,
                    BucketStart = group.Key.BucketStart,
                    BucketType = group.Key.BucketType,
                    SendingInstitution = group.Key.SendingInstitution,
                    ReceivingInstitution = group.Key.ReceivingInstitution,
                    RejectionCategory = group.Key.RejectionCategory,
                    FailureCode = group.Key.FailureCode,
                    TransactionCount = group.Sum(x => x.TransactionCount),
                    TotalAmount = group.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.BucketStart)
                .ThenBy(x => x.SendingInstitution),
            request,
            cancellationToken);
    }
}

file static class OracleFailedAnalyticsQueryHelpers
{
    public static IQueryable<OracleFailedTransactionAnalyticsRun> ApplyRunDateFilter(
        IQueryable<OracleFailedTransactionAnalyticsRun> query,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (from.HasValue)
        {
            query = query.Where(x => x.WindowEnd > from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.WindowStart < to.Value);
        }

        return query;
    }

    public static async Task<List<long>> ResolveRunIdsAsync(
        IStorageBroker storage,
        long? requestedRunId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (requestedRunId.HasValue)
        {
            return [requestedRunId.Value];
        }

        var query = ApplyRunDateFilter(
            storage.OracleFailedTransactionAnalyticsRuns.Where(x => x.Status == "SUCCESS"),
            from,
            to);

        if (!from.HasValue && !to.HasValue)
        {
            var latestRunId = await query
                .OrderByDescending(x => x.GeneratedAt)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return latestRunId == 0 ? [] : [latestRunId];
        }

        return await query
            .OrderBy(x => x.WindowStart)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
