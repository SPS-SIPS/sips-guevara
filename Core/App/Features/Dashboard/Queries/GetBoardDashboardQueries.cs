using System.Globalization;
using SPSS.App.Features.RegulatorDashboard.Queries;
using SPSS.Domain.Models.Enums;

namespace SPSS.App.Features.Dashboard.Queries;

[Authorize(Roles = QueryTransactions)]
public sealed record GetBoardDashboardCardsQuery(string? Period, DateTimeOffset? From, DateTimeOffset? To)
    : IRequest<BoardDashboardCardsResponseDto>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetBoardTotalSystemValueQuery(int Months = 8)
    : IRequest<BoardTotalSystemValueResponseDto>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetBoardTransactionTypeMixQuery(string? Period, DateTimeOffset? From, DateTimeOffset? To)
    : IRequest<BoardTransactionTypeMixResponseDto>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetBoardFinancialInclusionQuery()
    : IRequest<BoardFinancialInclusionResponseDto>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetBoardNewAccountsOnboardingQuery()
    : IRequest<BoardNewAccountsOnboardingResponseDto>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetBoardRulebookComplianceScoreQuery()
    : IRequest<BoardRulebookComplianceScoreDto>;

public sealed class GetBoardDashboardCardsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetBoardDashboardCardsQuery, BoardDashboardCardsResponseDto>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<BoardDashboardCardsResponseDto> Handle(
        GetBoardDashboardCardsQuery request,
        CancellationToken cancellationToken)
    {
        var (from, to) = RegulatorDashboardQueryHelpers.ResolveWindow(request.Period, request.From, request.To);
        var availability = await RegulatorDashboardQueryHelpers.BuildParticipantAvailabilityAsync(_storage, from, to, cancellationToken);
        var latestRiskReport = await RegulatorDashboardQueryHelpers.GetLatestComplianceReportAsync(_storage, from, to, cancellationToken);
        var liquidityRiskCount = await _storage.ParticipantThresholds
            .AsNoTracking()
            .CountAsync(x => x.Active && (x.LastKnownZone == ThresholdZone.Red || !x.CanSend), cancellationToken);

        return new BoardDashboardCardsResponseDto
        {
            Data =
            [
                new BoardDashboardCardDto
                {
                    Id = 1,
                    Title = "System Availability",
                    Value = $"{availability.Summary.UptimePercent:0.##}%",
                    Description = "Uptime across switch, endpoints & components"
                },
                new BoardDashboardCardDto
                {
                    Id = 2,
                    Title = "Systemic Risk Score",
                    Value = BoardDashboardQueryHelpers.SystemicRiskScore(
                        latestRiskReport?.Summary.HighRiskFindings ?? 0,
                        availability.Summary.OfflineParticipants,
                        liquidityRiskCount),
                    Description = "Liquidity + Fraud + AML"
                },
                new BoardDashboardCardDto
                {
                    Id = 3,
                    Title = "Fee per Transaction",
                    Value = "$0.00",
                    Description = "No transaction fees currently applied"
                }
            ]
        };
    }
}

public sealed class GetBoardTotalSystemValueQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetBoardTotalSystemValueQuery, BoardTotalSystemValueResponseDto>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<BoardTotalSystemValueResponseDto> Handle(
        GetBoardTotalSystemValueQuery request,
        CancellationToken cancellationToken)
    {
        var months = Math.Clamp(request.Months, 1, 36);
        var now = DateTimeOffset.UtcNow;
        var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-(months - 1));

        var values = await _storage.NetSettlementDetails
            .AsNoTracking()
            .Where(x => !x.IsFee && x.Date >= start)
            .GroupBy(x => new
            {
                x.Date.Year,
                x.Date.Month
            })
            .Select(x => new
            {
                x.Key.Year,
                x.Key.Month,
                Value = x.Sum(d => d.Amount)
            })
            .ToListAsync(cancellationToken);

        var valueByMonth = values.ToDictionary(x => (x.Year, x.Month), x => x.Value);

        return new BoardTotalSystemValueResponseDto
        {
            Data = Enumerable.Range(0, months)
                .Select(offset => start.AddMonths(offset))
                .Select(month =>
                {
                    valueByMonth.TryGetValue((month.Year, month.Month), out var value);
                    return new BoardMonthlyValueDto
                    {
                        Month = month.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                        Value = value
                    };
                })
                .ToList()
        };
    }
}

public sealed class GetBoardTransactionTypeMixQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetBoardTransactionTypeMixQuery, BoardTransactionTypeMixResponseDto>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<BoardTransactionTypeMixResponseDto> Handle(
        GetBoardTransactionTypeMixQuery request,
        CancellationToken cancellationToken)
    {
        var (from, to) = RegulatorDashboardQueryHelpers.ResolveWindow(request.Period, request.From, request.To);
        var categories = await _storage.NetSettlementDetails
            .AsNoTracking()
            .Where(x => !x.IsFee && x.Date >= from && x.Date < to)
            .GroupBy(x => x.CategoryPurpose)
            .Select(x => new
            {
                CategoryPurpose = x.Key,
                Count = x.LongCount()
            })
            .ToListAsync(cancellationToken);

        var response = new BoardTransactionTypeMixResponseDto();
        foreach (var category in categories)
        {
            var transactionType = BoardDashboardQueryHelpers.MapTransactionType(category.CategoryPurpose);
            if (transactionType is null)
            {
                continue;
            }

            response.Data[transactionType] += category.Count;
        }

        return response;
    }
}

public sealed class GetBoardFinancialInclusionQueryHandler
    : IRequestHandler<GetBoardFinancialInclusionQuery, BoardFinancialInclusionResponseDto>
{
    public ValueTask<BoardFinancialInclusionResponseDto> Handle(
        GetBoardFinancialInclusionQuery request,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(new BoardFinancialInclusionResponseDto());
}

public sealed class GetBoardNewAccountsOnboardingQueryHandler
    : IRequestHandler<GetBoardNewAccountsOnboardingQuery, BoardNewAccountsOnboardingResponseDto>
{
    public ValueTask<BoardNewAccountsOnboardingResponseDto> Handle(
        GetBoardNewAccountsOnboardingQuery request,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(new BoardNewAccountsOnboardingResponseDto());
}

public sealed class GetBoardRulebookComplianceScoreQueryHandler
    : IRequestHandler<GetBoardRulebookComplianceScoreQuery, BoardRulebookComplianceScoreDto>
{
    public ValueTask<BoardRulebookComplianceScoreDto> Handle(
        GetBoardRulebookComplianceScoreQuery request,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(new BoardRulebookComplianceScoreDto());
}

internal static class BoardDashboardQueryHelpers
{
    public static string SystemicRiskScore(int highRiskFindings, int offlineParticipants, int liquidityRiskCount)
    {
        if (highRiskFindings > 0 || liquidityRiskCount > 0 || offlineParticipants > 2)
        {
            return "HIGH";
        }

        if (offlineParticipants > 0)
        {
            return "MEDIUM";
        }

        return "LOW";
    }

    public static string? MapTransactionType(string? categoryPurpose)
    {
        if (string.IsNullOrWhiteSpace(categoryPurpose))
        {
            return null;
        }

        var normalized = categoryPurpose.Trim().ToLowerInvariant();
        if (normalized.Contains("p2p", StringComparison.Ordinal) ||
            normalized.Contains("person", StringComparison.Ordinal))
        {
            return "p2p";
        }

        if (normalized.Contains("p2m", StringComparison.Ordinal) ||
            normalized.Contains("merchant", StringComparison.Ordinal))
        {
            return "p2m";
        }

        if (normalized.Contains("g2p", StringComparison.Ordinal) ||
            normalized.Contains("government", StringComparison.Ordinal))
        {
            return "g2p";
        }

        if (normalized.Contains("b2b", StringComparison.Ordinal) ||
            normalized.Contains("business", StringComparison.Ordinal))
        {
            return "b2b";
        }

        return null;
    }
}
