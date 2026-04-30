using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Commands;
[Authorize(Roles = UpdateTransactions)]
public sealed record UploadFailedTransactionRuleIdsCsvCommand(string CsvContent) : IRequest<Response<FailedTransactionRuleIdsCsvUploadResultDto>>;

public sealed class UploadFailedTransactionRuleIdsCsvCommandHandler(IStorageBroker storage)
    : IRequestHandler<UploadFailedTransactionRuleIdsCsvCommand, Response<FailedTransactionRuleIdsCsvUploadResultDto>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<FailedTransactionRuleIdsCsvUploadResultDto>> Handle(
        UploadFailedTransactionRuleIdsCsvCommand request,
        CancellationToken cancellationToken)
    {
        var result = new FailedTransactionRuleIdsCsvUploadResultDto();

        if (string.IsNullOrWhiteSpace(request.CsvContent))
        {
            return Response<FailedTransactionRuleIdsCsvUploadResultDto>.BadRequest("CSV file is empty", result);
        }

        var rows = ReadRows(request.CsvContent, result);
        if (rows.Count == 0)
        {
            return Response<FailedTransactionRuleIdsCsvUploadResultDto>.BadRequest("CSV file has no valid rows", result);
        }

        var ipsTransactionIds = rows.Select(x => x.IpsTransactionId).Distinct().ToList();
        var transactions = await _storage.FailedTransactions
            .Where(x => ipsTransactionIds.Contains(x.IpsTransactionId))
            .ToDictionaryAsync(x => x.IpsTransactionId, cancellationToken);

        foreach (var row in rows)
        {
            if (!transactions.TryGetValue(row.IpsTransactionId, out var transaction))
            {
                result.NotFoundCount++;
                result.MissingIpsTransactionIds.Add(row.IpsTransactionId);
                continue;
            }

            transaction.RuleIds = row.RuleIds;
            _storage.FailedTransactions.Update(transaction);
            result.UpdatedCount++;
            result.UpdatedIpsTransactionIds.Add(row.IpsTransactionId);
        }

        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<FailedTransactionRuleIdsCsvUploadResultDto>(result);
    }

    private static List<FailedTransactionRuleIdsCsvRow> ReadRows(
        string csvContent,
        FailedTransactionRuleIdsCsvUploadResultDto result)
    {
        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null
        });

        var rows = new List<FailedTransactionRuleIdsCsvRow>();

        try
        {
            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                result.TotalRows++;

                var ipsTransactionIdText = csv.GetField("IpsTransactionId");
                var ruleIdsText = GetRuleIdsField(csv);
                var rowNumber = csv.Context.Parser?.Row ?? result.TotalRows;

                if (!long.TryParse(ipsTransactionIdText, out var ipsTransactionId))
                {
                    result.InvalidRowCount++;
                    result.Errors.Add($"Row {rowNumber}: IpsTransactionId is missing or invalid");
                    continue;
                }

                var ruleIds = SplitRuleIds(ruleIdsText);
                if (ruleIds.Count == 0)
                {
                    result.InvalidRowCount++;
                    result.Errors.Add($"Row {rowNumber}: RuleIds is missing or empty");
                    continue;
                }

                rows.Add(new FailedTransactionRuleIdsCsvRow(ipsTransactionId, ruleIds));
            }
        }
        catch (Exception ex) when (ex is CsvHelperException or IOException)
        {
            result.InvalidRowCount++;
            result.Errors.Add($"CSV parsing failed: {ex.Message}");
        }

        return rows;
    }

    private static List<string> SplitRuleIds(string? ruleIds)
    {
        return (ruleIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
    }

    private static string? GetRuleIdsField(CsvReader csv)
    {
        var record = csv.Context.Parser?.Record;
        if (record is { Length: > 2 })
        {
            return string.Join(',', record.Skip(1));
        }

        return csv.GetField("RuleIds");
    }

    private sealed record FailedTransactionRuleIdsCsvRow(long IpsTransactionId, List<string> RuleIds);
}
