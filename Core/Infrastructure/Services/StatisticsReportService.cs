using SPSS.Domain.Models.BL;
using SPSS.Domain.Models.DTOs.Reports;

namespace SPSS.Infrastructure.Services;

public class StatisticsReportService : IStatisticsReportService
{
    public Task<SystemOverviewReportDto> GetSystemOverviewAsync(IEnumerable<BicStatisticsDto> stats)
    {
        var all = stats.ToList();
        return Task.FromResult(new SystemOverviewReportDto
        {
            TotalParticipants = all.Select(x => x.Bic).Distinct().Count(),
            TotalTransactions = all.Sum(x => x.SentCount + x.ReceivedCount),
            TotalValue = all.Sum(x => x.TotalAmountSent + x.TotalAmountReceived),
            TotalFees = all.Sum(x => x.TotalFeesReceived),
            From = all.Min(x => x.Date.Date),
            To = all.Max(x => x.Date.Date),
        });
    }

    public Task<List<ParticipantSummaryReportDto>> GetParticipantSummariesAsync(IEnumerable<BicStatisticsDto> stats)
    {
        var result = stats
            .GroupBy(x => x.Bic)
            .Select(g => new ParticipantSummaryReportDto
            {
                Bic = g.Key,
                SentCount = g.Sum(x => x.SentCount),
                TotalAmountSent = g.Sum(x => x.TotalAmountSent),
                ReceivedCount = g.Sum(x => x.ReceivedCount),
                TotalAmountReceived = g.Sum(x => x.TotalAmountReceived),
                TotalFeesReceived = g.Sum(x => x.TotalFeesReceived)
            })
            .ToList();
        return Task.FromResult(result);
    }

    public Task<List<NetPositionReportDto>> GetNetPositionTableAsync(IEnumerable<BicStatisticsDto> stats)
    {
        var result = stats
            .GroupBy(x => x.Bic)
            .Select(g => new NetPositionReportDto
            {
                Bic = g.Key,
                NetPosition = g.Sum(x => x.TotalAmountReceived) - g.Sum(x => x.TotalAmountSent)
            })
            .ToList();
        return Task.FromResult(result);
    }

    public Task<TrendPackReportDto> GetTrendPackAsync(IEnumerable<BicStatisticsDto> stats, DateTime from, DateTime to)
    {
        // Filter out records with invalid or default dates
        var validStats = stats
            .Where(x => x.Date >= new DateTimeOffset(new DateTime(2000, 1, 1)) && x.Date <= new DateTimeOffset(new DateTime(9999, 12, 31)))
            .Where(x => x.Date >= from && x.Date <= to);
        var points = validStats
            .GroupBy(x => x.Date.Date)
            .Select(g => new TrendPointDto
            {
                Date = g.Key,
                TotalValue = g.Sum(x => x.TotalAmountSent + x.TotalAmountReceived),
                TotalTransactions = g.Sum(x => x.SentCount + x.ReceivedCount)
            })
            .OrderBy(x => x.Date)
            .ToList();
        return Task.FromResult(new TrendPackReportDto { Points = points });
    }

    public Task<DirectionalFlowReportDto> GetDirectionalFlowAsync(IEnumerable<BicStatisticsDto> stats)
    {
        // For each pair (sender, receiver), sum transactions and amounts
        var flows = stats
            .SelectMany(bic => bic.TopReceivers.Select(r => new DirectionalFlowItemDto
            {
                FromBic = bic.Bic,
                ToBic = r.Bic,
                TransactionCount = r.TransactionCount,
                TotalAmount = r.TotalAmount
            }))
            .GroupBy(x => new { x.FromBic, x.ToBic })
            .Select(g => new DirectionalFlowItemDto
            {
                FromBic = g.Key.FromBic,
                ToBic = g.Key.ToBic,
                TransactionCount = g.Sum(x => x.TransactionCount),
                TotalAmount = g.Sum(x => x.TotalAmount)
            })
            .ToList();
        return Task.FromResult(new DirectionalFlowReportDto { Flows = flows });
    }

    public Task<FeeDistributionReportDto> GetFeeDistributionAsync(IEnumerable<BicStatisticsDto> stats)
    {
        var items = stats
            .GroupBy(x => x.Bic)
            .Select(g => new FeeDistributionItemDto
            {
                Bic = g.Key,
                TotalFeesReceived = g.Sum(x => x.TotalFeesReceived)
            })
            .ToList();
        return Task.FromResult(new FeeDistributionReportDto { Items = items });
    }

    public Task<CounterpartyListReportDto> GetCounterpartyListsAsync(IEnumerable<BicStatisticsDto> stats)
    {
        var topSenders = stats
            .SelectMany(bic => bic.TopSenders.Select(s => new CounterpartyListItemDto
            {
                Bic = s.Bic,
                TransactionCount = s.TransactionCount,
                TotalAmount = s.TotalAmount
            }))
            .GroupBy(x => x.Bic)
            .Select(g => new CounterpartyListItemDto
            {
                Bic = g.Key,
                TransactionCount = g.Sum(x => x.TransactionCount),
                TotalAmount = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(x => x.TransactionCount)
            .Take(10)
            .ToList();
        var topReceivers = stats
            .SelectMany(bic => bic.TopReceivers.Select(r => new CounterpartyListItemDto
            {
                Bic = r.Bic,
                TransactionCount = r.TransactionCount,
                TotalAmount = r.TotalAmount
            }))
            .GroupBy(x => x.Bic)
            .Select(g => new CounterpartyListItemDto
            {
                Bic = g.Key,
                TransactionCount = g.Sum(x => x.TransactionCount),
                TotalAmount = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(x => x.TransactionCount)
            .Take(10)
            .ToList();
        return Task.FromResult(new CounterpartyListReportDto { TopSenders = topSenders, TopReceivers = topReceivers });
    }

    public Task<ConcentrationAnalysisReportDto> GetConcentrationAnalysisAsync(IEnumerable<BicStatisticsDto> stats)
    {
        var grouped = stats
            .GroupBy(x => x.Bic)
            .Select(g => new ConcentrationItemDto
            {
                Bic = g.Key,
                TotalValue = g.Sum(x => x.TotalAmountSent + x.TotalAmountReceived),
                TotalTransactions = g.Sum(x => x.SentCount + x.ReceivedCount)
            })
            .OrderByDescending(x => x.TotalValue)
            .ToList();
        var totalValue = grouped.Sum(x => x.TotalValue);
        var top5 = grouped.Take(5).Sum(x => x.TotalValue);
        var top10 = grouped.Take(10).Sum(x => x.TotalValue);
        return Task.FromResult(new ConcentrationAnalysisReportDto
        {
            TopConcentrations = grouped,
            Top5SharePercent = totalValue == 0 ? 0 : (top5 / totalValue) * 100,
            Top10SharePercent = totalValue == 0 ? 0 : (top10 / totalValue) * 100
        });
    }

    public Task<LeagueTableReportDto> GetLeagueTableAsync(IEnumerable<BicStatisticsDto> stats)
    {
        var items = stats
            .GroupBy(x => x.Bic)
            .Select(g => new LeagueTableItemDto
            {
                Bic = g.Key,
                NetPosition = g.Sum(x => x.TotalAmountReceived) - g.Sum(x => x.TotalAmountSent),
                TotalTransactions = g.Sum(x => x.SentCount + x.ReceivedCount),
                TotalValue = g.Sum(x => x.TotalAmountSent + x.TotalAmountReceived)
            })
            .OrderByDescending(x => x.NetPosition)
            .ToList();
        return Task.FromResult(new LeagueTableReportDto { Items = items });
    }

    public Task<MatrixReportDto> GetMatrixAsync(IEnumerable<BicStatisticsDto> stats)
    {
        var matrix = new Dictionary<string, MatrixRowDto>();
        foreach (var bic in stats)
        {
            foreach (var receiver in bic.TopReceivers)
            {
                if (!matrix.TryGetValue(bic.Bic, out var row))
                {
                    row = new MatrixRowDto { FromBic = bic.Bic };
                    matrix[bic.Bic] = row;
                }
                if (!row.ToBics.ContainsKey(receiver.Bic))
                {
                    row.ToBics[receiver.Bic] = new MatrixCellDto();
                }
                row.ToBics[receiver.Bic].TransactionCount += receiver.TransactionCount;
                row.ToBics[receiver.Bic].TotalAmount += receiver.TotalAmount;
            }
        }
        return Task.FromResult(new MatrixReportDto { Rows = matrix.Values.ToList() });
    }

    public Task<List<InstitutionUptimeDto>> GetInstitutionUptimeReportAsync(IEnumerable<MonitHistory> histories, DateTime? from = null, DateTime? to = null)
    {
        var filtered = histories;
        if (from.HasValue && to.HasValue)
        {
            filtered = filtered.Where(x => x.CheckedAt >= from.Value && x.CheckedAt <= to.Value);
        }

        var result = filtered
            .GroupBy(x => x.InstitutionBic)
            .Select(g => new InstitutionUptimeDto
            {
                InstitutionBic = g.Key,
                TotalChecks = g.Count(),
                SuccessChecks = g.Count(x => x.Success),
                Uptime = !g.Any() ? 0 : g.Count(x => x.Success) * 100.0 / g.Count()
            })
            .ToList();

        return Task.FromResult(result);
    }
}
