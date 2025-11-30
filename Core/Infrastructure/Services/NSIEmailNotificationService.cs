using AutoMapper;
using SPSS.Domain.Interfaces;
using SPSS.Domain.Models.BL;
using SPSS.Domain.Models.DTOs;
using SPSS.Infrastructure.Helpers;
using static SPSS.Domain.Common.RTGSHelpers;

namespace SPSS.Infrastructure.Services;

public sealed class NSIEmailNotificationService(IStorageBroker storage,
                                                IEmailSender emailSender,
                                                NSI nsiConfig,
                                                EmailConfig emailConfig,
                                                ILogger<NSIEmailNotificationService> logger,
                                                IMapper mapper) : INSIEmailNotificationService
{
    private readonly IStorageBroker _storage = storage;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly NSI _nsiConfig = nsiConfig;
    private readonly EmailConfig _emailConfig = emailConfig;
    private readonly ILogger<NSIEmailNotificationService> _logger = logger;
    private readonly IMapper _mapper = mapper;

    public async Task NotifyAsync(long netSettlementHeaderId, CancellationToken cancellationToken = default)
    {
        var header = await _storage.NetSettlementHeaders
            .Where(h => h.Id == netSettlementHeaderId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ArgumentException($"No NetSettlementHeader found with Id {netSettlementHeaderId}");

        var allDetails = await _storage.NetSettlementDetails
            .Where(d => d.NetSettlementHeaderId == netSettlementHeaderId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (allDetails.Count == 0)
        {
            _logger.LogWarning("No NetSettlementDetails to notify for header {HeaderId}", netSettlementHeaderId);
            return;
        }

        var switchBic = _nsiConfig.CoreBankBIC ?? "";

        // Collect all distinct BICs participating (excluding switch and fees)
        var allBICs = allDetails
             .Where(t => !t.IsFee)
             .SelectMany(t => new[] { t.DebtorBic, t.CreditorBic })
             .Where(b => !string.IsNullOrWhiteSpace(b) && b != switchBic)
             .Distinct()
             .ToList();

        int sentCount = 0;
        var skipped = new List<string>();
        foreach (var bic in allBICs)
        {
            try
            {
                // Find participant email by BIC
                var bic8 = string.IsNullOrWhiteSpace(bic) ? bic : (bic.Length >= 8 ? bic.Substring(0, 8) : bic);
                var participant = await _storage.Participants
                    .Where(p => p.Active && p.DeletedOn == null &&
                           (p.BicNo == bic || p.BicNo.StartsWith(bic8)))
                    .Select(p => new { p.Name, p.Email, p.BicNo })
                    .FirstOrDefaultAsync(cancellationToken);

                if (participant == null || string.IsNullOrWhiteSpace(participant.Email))
                {
                    skipped.Add(bic);
                    _logger.LogInformation("Skipping notification for BIC {BIC} due to missing participant/email (lookup by '{Lookup}')", bic, bic8);
                    continue;
                }

                // Step 1: Collect TxIds where this BIC is involved in any hop
                var txIds = allDetails
                    .Where(s => s.DebtorBic == bic || s.CreditorBic == bic)
                    .Select(s => s.TxId)
                    .Distinct()
                    .ToList();

                if (txIds.Count == 0)
                {
                    _logger.LogInformation("No transactions found for BIC {BIC}", bic);
                    continue;
                }

                // Step 2: Retrieve all hops for those TxIds to analyze full transaction flow
                var details = allDetails
                    .Where(s => txIds.Contains(s.TxId))
                    .OrderByDescending(s => s.Date)
                    .ThenBy(s => s.TxId)
                    .ToList();

                // Step 3: Build reconciliation - identify real sender and receiver
                var reconciliation = details
                    .GroupBy(tx => tx.TxId)
                    .Select(group =>
                    {
                        var hops = group.ToList();
                        // Identify real sender (first non-switch debtor)
                        var first = hops.FirstOrDefault(h => !h.IsFee && h.DebtorBic != switchBic);
                        var last = hops.LastOrDefault(h => !h.IsFee && h.CreditorBic != switchBic);
                        var representativeHop = hops
                            .Where(h => !h.IsFee && ((h.DebtorBic == switchBic) ^ (h.CreditorBic == switchBic)))
                            .FirstOrDefault();
                        var totalAmount = representativeHop?.Amount
                            ?? hops.Where(h => !h.IsFee).Select(h => h.Amount).FirstOrDefault();
                        
                        return new
                        {
                            TxId = group.Key.ToString(),
                            EndToEndId = first?.EndToEndId ?? string.Empty,
                            Date = hops.Max(h => h.Date),
                            Amount = totalAmount,
                            SenderBic = first?.DebtorBic ?? "UNKNOWN",
                            ReceiverBic = last?.CreditorBic ?? "UNKNOWN",
                            Hops = hops
                        };
                    })
                    // Filter to only include transactions where BIC is the real sender or receiver
                    .Where(r => r.SenderBic == bic || r.ReceiverBic == bic)
                    .Select(r => new ReconciliationRow
                    {
                        TxId = r.TxId,
                        EndToEndId = r.EndToEndId,
                        Date = r.Date,
                        Amount = r.Amount,
                        SenderBic = r.SenderBic,
                        ReceiverBic = r.ReceiverBic,
                        // Calculate fees received by the BIC from the switch
                        TotalFees = r.Hops
                            .Where(h => h.IsFee && h.DebtorBic == switchBic && h.CreditorBic == bic)
                            .Sum(h => h.Amount)
                    })
                    .ToList();

                if (reconciliation.Count == 0)
                {
                    _logger.LogInformation("No reconciliation rows for BIC {BIC}", bic);
                    continue;
                }

                var memoryStream = await SPSS.Domain.Common.CSVHelper.WriteRecordsToCsv(reconciliation);
                var fileName = $"Reconciliation_{netSettlementHeaderId}_{bic}.csv";
                var tempPath = Path.Combine(Path.GetTempPath(), fileName);
                await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(fs, cancellationToken);
                }

                var subject = $"NSI Reconciliation Report for {bic} - {header.CredDtTm:yyyy-MM-dd}";

                var htmlBody = await TemplateRenderer.RenderAsync(_emailConfig.TemplatesDirectory, "nsi-reconciliation.html", new Dictionary<string, string>
                {
                    ["ParticipantName"] = participant.Name,
                    ["BIC"] = bic,
                    ["MsgId"] = header.MsgId,
                    ["Date"] = header.CredDtTm.ToString("yyyy-MM-dd"),
                    ["AttachmentName"] = fileName,
                }, cancellationToken);

                await _emailSender.SendEmailAsync(participant.Email!, subject, htmlBody, tempPath, isHTML: true);

                TryDelete(tempPath);

                _logger.LogInformation("Sent NSI reconciliation email to {Email} for BIC {BIC}", participant.Email, bic);
                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send NSI email for BIC {BIC} (Header {HeaderId})", bic, netSettlementHeaderId);
                skipped.Add(bic);
            }
        }

        _logger.LogInformation("NSI email notification summary for header {HeaderId}: sent={Sent}, skipped={SkippedCount}, skippedBICs=[{Skipped}]",
            netSettlementHeaderId, sentCount, skipped.Count, string.Join(",", skipped));
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }
}

file sealed class ReconciliationRow { public string EndToEndId { get; set; } = string.Empty; public string TxId { get; set; } = string.Empty; public DateTimeOffset Date { get; set; } public decimal Amount { get; set; } public string SenderBic { get; set; } = string.Empty; public string ReceiverBic { get; set; } = string.Empty; public decimal TotalFees { get; set; } }
