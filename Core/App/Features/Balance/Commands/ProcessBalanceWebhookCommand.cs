using SPSS.Domain.Models.DTOs;
using SPSS.Domain.Models.Enums;
using SPSS.Domain.Services;

namespace SPSS.App.Features.Balance.Commands;

public sealed record ProcessBalanceWebhookCommand(BalanceWebhookDto Data) : IRequest<Response<string>>;

public sealed class ProcessBalanceWebhookCommandHandler(
    IStorageBroker storage,
    IBalanceThresholdService thresholdService,
    IEmailSender emailSender,
    ILogger<ProcessBalanceWebhookCommandHandler> logger) : IRequestHandler<ProcessBalanceWebhookCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    private readonly IBalanceThresholdService _thresholdService = thresholdService ?? throw new ArgumentNullException(nameof(thresholdService));
    private readonly IEmailSender _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
    private readonly ILogger<ProcessBalanceWebhookCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<Response<string>> Handle(ProcessBalanceWebhookCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;

        if (data.Participants == null || data.Participants.Count == 0)
        {
            _logger.LogWarning("Webhook received with no participants at {Timestamp}", data.Timestamp);
            return new Response<string>("No participants to process");
        }

        _logger.LogInformation(
            "Balance webhook received at {Timestamp} with {TotalCount} participants",
            data.Timestamp,
            data.TotalCount);

        foreach (var participant in data.Participants)
        {
            _logger.LogInformation(
                "Received balance data: BIC={Bic}, Name={Name}, Balance={Balance:N2} {Currency}, Status={Status}, AccountNumber={AccountNumber}, CheckedAt={CheckedAt}",
                participant.Bic,
                participant.ParticipantName,
                participant.RemainingNetDebitCap,
                participant.Currency,
                participant.Status,
                participant.AccountNumber,
                participant.CheckedAt);
        }

        var processedCount = 0;
        var skippedCount = 0;
        var errorCount = 0;

        foreach (var participant in data.Participants)
        {
            try
            {
                await ProcessParticipantBalanceAsync(participant, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing balance for BIC {Bic}",
                    participant.Bic);
                errorCount++;
            }
        }

        var summary = $"Processed {processedCount} participants, skipped {skippedCount}, errors {errorCount}";
        _logger.LogInformation(summary);

        return new Response<string>(summary);
    }

    private async Task ProcessParticipantBalanceAsync(ParticipantBalanceDto participant, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(participant.Bic))
        {
            _logger.LogWarning("Participant with empty BIC skipped");
            return;
        }

        if (participant.RemainingNetDebitCap < 0)
        {
            _logger.LogWarning(
                "Participant {Bic} has negative balance {Balance}, skipping",
                participant.Bic,
                participant.RemainingNetDebitCap);
            return;
        }

        var threshold = await _storage.ParticipantThresholds
            .FirstOrDefaultAsync(x => x.Bic == participant.Bic, cancellationToken);

        if (threshold == null)
        {
            _logger.LogDebug(
                "No threshold configuration found for BIC {Bic}. Skipping balance evaluation.",
                participant.Bic);
            return;
        }

        var previousZone = threshold.LastKnownZone;
        var currentBalance = participant.RemainingNetDebitCap;

        ThresholdZone currentZone;
        try
        {
            currentZone = _thresholdService.EvaluateZone(currentBalance, threshold);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid threshold configuration for BIC {Bic}", participant.Bic);
            return;
        }

        threshold.LastKnownBalance = currentBalance;
        threshold.LastKnownZone = currentZone;

        var zoneChanged = currentZone != previousZone;

        if (zoneChanged)
        {
            threshold.LastZoneChangeAt = DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "Zone transition detected for BIC {Bic}: {PreviousZone} → {CurrentZone}. Balance: {Balance:N2} {Currency}",
                participant.Bic,
                previousZone,
                currentZone,
                currentBalance,
                participant.Currency);
        }
        else
        {
            _logger.LogDebug(
                "Balance updated for BIC {Bic}: Zone={Zone}, Balance={Balance:N2} {Currency}, GoldenAmount={GoldenAmount:N2}",
                participant.Bic,
                currentZone,
                currentBalance,
                participant.Currency,
                threshold.GoldenAmount);
        }

        var shouldSendAlert = _thresholdService.ShouldSendAlert(currentZone, previousZone);

        if (shouldSendAlert)
        {
            await SendAlertEmailAsync(threshold, currentZone, currentBalance, participant.Currency, cancellationToken);
            threshold.LastAlertSentAt = DateTimeOffset.UtcNow;
        }

        _storage.ParticipantThresholds.Update(threshold);
        await _storage.SaveChangesAsync(cancellationToken);
    }

    private async Task SendAlertEmailAsync(
        Domain.Models.BL.ParticipantThreshold threshold,
        ThresholdZone zone,
        decimal currentBalance,
        string currency,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(threshold.AlertRecipients))
        {
            _logger.LogWarning(
                "No alert recipients configured for BIC {Bic}. Skipping email notification.",
                threshold.Bic);
            return;
        }

        var recipients = threshold.AlertRecipients.Split(';', StringSplitOptions.RemoveEmptyEntries);

        if (recipients.Length == 0)
        {
            _logger.LogWarning(
                "Alert recipients list is empty for BIC {Bic}. Skipping email notification.",
                threshold.Bic);
            return;
        }

        var (subject, body) = zone switch
        {
            ThresholdZone.Amber => BuildAmberAlert(threshold, currentBalance, currency),
            ThresholdZone.Red => BuildRedAlert(threshold, currentBalance, currency),
            _ => (string.Empty, string.Empty)
        };

        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
        {
            return;
        }

        try
        {
            foreach (var recipient in recipients)
            {
                var trimmedRecipient = recipient.Trim();
                if (string.IsNullOrWhiteSpace(trimmedRecipient))
                {
                    continue;
                }
                await _emailSender.SendEmailAsync(
                    trimmedRecipient,
                    subject,
                    body,
                    null,
                    false);
            }

            _logger.LogInformation(
                "Alert email sent for BIC {Bic}, Zone {Zone} to {RecipientCount} recipient(s)",
                threshold.Bic,
                zone,
                recipients.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send alert email for BIC {Bic}, Zone {Zone}",
                threshold.Bic,
                zone);
            throw;
        }
    }

    private (string Subject, string Body) BuildAmberAlert(
        Domain.Models.BL.ParticipantThreshold threshold,
        decimal currentBalance,
        string currency)
    {
        var amberThreshold = threshold.GoldenAmount * (threshold.AmberPercentage / 100m);

        var subject = $"Settlement Account Balance Alert - {threshold.Bic}";

        var body = $@"Dear Settlement Operations Team,

This is an advisory notification regarding your settlement account balance.

Participant BIC: {threshold.Bic}
Current Balance: {currentBalance:N2} {currency}
Minimum Required Balance: {threshold.GoldenAmount:N2} {currency}
Alert Threshold: {amberThreshold:N2} {currency}

Your settlement account balance is approaching the minimum operational level. While your account remains functional, we recommend proactive replenishment to maintain optimal service continuity.

Please consider topping up your settlement account at your earliest convenience to avoid potential service impact.

If you have already initiated a recharge, please disregard this notice.

Best regards,
Payment Switch Operations
Automated Balance Monitoring System";

        return (subject, body);
    }

    private (string Subject, string Body) BuildRedAlert(
        Domain.Models.BL.ParticipantThreshold threshold,
        decimal currentBalance,
        string currency)
    {
        var redThreshold = threshold.GoldenAmount * (threshold.RedPercentage / 100m);

        var subject = $"URGENT: Settlement Account Balance Critical - {threshold.Bic}";

        var body = $@"Dear Settlement Operations Team,

This is a critical notification regarding your settlement account balance.

Participant BIC: {threshold.Bic}
Current Balance: {currentBalance:N2} {currency}
Minimum Required Balance: {threshold.GoldenAmount:N2} {currency}
Critical Threshold: {redThreshold:N2} {currency}

Your settlement account balance has fallen below the required operational threshold. Immediate action is required.

REQUIRED ACTION:
Please recharge your settlement account immediately to restore service continuity. Failure to maintain adequate balance may result in transaction processing disruptions.

If you have already initiated a recharge, please confirm receipt and expected settlement time.

For urgent assistance, please contact our operations team immediately.

Best regards,
Payment Switch Operations
Automated Balance Monitoring System";

        return (subject, body);
    }
}
