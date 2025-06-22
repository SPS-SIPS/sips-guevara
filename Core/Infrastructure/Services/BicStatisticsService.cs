namespace SPSS.Infrastructure.Services;

public class BicStatisticsService : IBicStatisticsService
{
    public Task<BicStatisticsDto> CalculateBicStatisticsAsync(string bic, IEnumerable<NetSettlementDetailDto> transactions)
    {
        var statistics = new BicStatisticsDto { Bic = bic };
        const string switchBic = "ZYABSOSM";

        // Clean and flatten transactions: remove switch entries and reconstruct original flows
        var cleanedTransactions = new List<NetSettlementDetailDto>();
        foreach (var group in transactions.GroupBy(t => t.TxId))
        {
            var entries = group.ToList();
            // Carry over fee entries where creditor is not switch
            cleanedTransactions.AddRange(entries.Where(e => e.IsFee && e.CreditorBic != switchBic));
            // Process non-fee movements
            var movements = entries.Where(e => !e.IsFee).ToList();
            var initial = movements.FirstOrDefault(e => e.DebtorBic != switchBic);
            var final = movements.FirstOrDefault(e => e.CreditorBic != switchBic);
            if (initial != null && final != null)
            {
                cleanedTransactions.Add(new NetSettlementDetailDto
                {
                    EndToEndId = initial.EndToEndId,
                    TxId = group.Key,
                    Date = initial.Date,
                    Amount = initial.Amount,
                    DebtorBic = initial.DebtorBic,
                    CreditorBic = final.CreditorBic,
                    IsFee = false
                });
            }
            else
            {
                // No switch involvement: keep only entries not involving switch
                cleanedTransactions.AddRange(movements.Where(e => e.DebtorBic != switchBic && e.CreditorBic != switchBic));
            }
        }
        // Use cleaned list to compute stats
        var relevantTransactions = cleanedTransactions.Where(t => t.DebtorBic == bic || t.CreditorBic == bic).ToList();

        statistics.SentCount = relevantTransactions.Count(t => t.DebtorBic == bic);
        statistics.TotalAmountSent = relevantTransactions.Where(t => t.DebtorBic == bic).Sum(t => t.Amount);

        statistics.ReceivedCount = relevantTransactions.Count(t => t.CreditorBic == bic);
        statistics.TotalAmountReceived = relevantTransactions.Where(t => t.CreditorBic == bic).Sum(t => t.Amount);

        // Calculate total fees received by this BIC
        statistics.TotalFeesReceived = relevantTransactions.Where(t => t.CreditorBic == bic && t.IsFee).Sum(t => t.Amount);

        statistics.TopReceivers = relevantTransactions
            .Where(t => t.DebtorBic == bic)
            .GroupBy(t => t.CreditorBic)
            .Select(g => new BicInteractionDto
            {
                Bic = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.Amount)
            })
            .Where(i => !string.Equals(i.Bic, switchBic))
            .OrderByDescending(i => i.TransactionCount)
            .ThenByDescending(i => i.TotalAmount)
            .Take(3)
            .ToList();

        statistics.TopSenders = relevantTransactions
            .Where(t => t.CreditorBic == bic)
            .GroupBy(t => t.DebtorBic)
            .Select(g => new BicInteractionDto
            {
                Bic = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.Amount)
            })
            .Where(i => !string.Equals(i.Bic, switchBic))
            .OrderByDescending(i => i.TransactionCount)
            .ThenByDescending(i => i.TotalAmount)
            .Take(3)
            .ToList();

        return Task.FromResult(statistics);
    }
}
