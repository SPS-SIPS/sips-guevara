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

        var switchBic = _nsiConfig.SwitchBIC;

        // Collect all distinct BICs participating
        var allBICs = allDetails.Select(t => t.DebtorBic)
             .Concat(allDetails.Select(t => t.CreditorBic))
             .Where(b => !string.IsNullOrWhiteSpace(b))
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

                // Build reconciliation for this BIC (similar to GetSettlementDetailsFileQuery)
                var details = allDetails
                    .Where(s => s.DebtorBic == bic || s.CreditorBic == bic)
                    .OrderByDescending(s => s.Date)
                    .ThenBy(s => s.TxId)
                    .ToList();

                var reconciliation = details
                    .GroupBy(tx => tx.TxId)
                    .Select(group =>
                    {
                        var hops = group.ToList();
                        var first = hops.FirstOrDefault(h => h.DebtorBic != switchBic);
                        var last = hops.LastOrDefault(h => h.CreditorBic != switchBic);
                        var representativeHop = hops
                            .Where(h => !h.IsFee && ((h.DebtorBic == switchBic) ^ (h.CreditorBic == switchBic)))
                            .FirstOrDefault();
                        var totalAmount = representativeHop?.Amount
                            ?? hops.Where(h => !h.IsFee).Select(h => h.Amount).FirstOrDefault();
                        var totalFees = hops
                            .Where(h => h.IsFee && first != null && h.DebtorBic == first.DebtorBic && h.CreditorBic == switchBic)
                            .Sum(h => h.Amount);
                        return new ReconciliationRow
                        {
                            TxId = group.Key.ToString(),
                            EndToEndId = first?.EndToEndId ?? string.Empty,
                            Date = hops.Max(h => h.Date),
                            Amount = totalAmount,
                            SenderBic = first?.DebtorBic ?? "UNKNOWN",
                            ReceiverBic = last?.CreditorBic ?? "UNKNOWN",
                            TotalFees = totalFees
                        };
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
