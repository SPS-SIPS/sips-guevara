using SPSS.Domain.Models.DTOs;
using SPSS.Domain.Models.Enums;
using SPSS.Domain.Services;

namespace SPSS.App.Features.Balance.Commands;

public sealed record ProcessBalanceWebhookCommand(BalanceWebhookDto Data) : IRequest<Response<string>>;

public sealed class ProcessBalanceWebhookCommandHandler(
    IStorageBroker storage,
    IBalanceThresholdService thresholdService,
    IEmailSender emailSender,
    ITemplateRenderer templateRenderer,
    ILogger<ProcessBalanceWebhookCommandHandler> logger) : IRequestHandler<ProcessBalanceWebhookCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    private readonly IBalanceThresholdService _thresholdService = thresholdService ?? throw new ArgumentNullException(nameof(thresholdService));
    private readonly IEmailSender _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
    private readonly ITemplateRenderer _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
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

        // Evaluate send capability based on configuration
        var previousCanSend = threshold.CanSend;
        if (threshold.EnforceSendRestriction)
        {
            threshold.CanSend = currentBalance >= threshold.MinimumSendBalance;
        }
        else
        {
            threshold.CanSend = true;
        }

        var canSendChanged = threshold.CanSend != previousCanSend;
        var zoneChanged = currentZone != previousZone;

        if (canSendChanged)
        {
            _logger.LogWarning(
                "Send capability changed for BIC {Bic}: {PreviousCanSend} → {CurrentCanSend}. Balance: {Balance:N2}, MinimumSendBalance: {MinimumSendBalance:N2} {Currency}",
                participant.Bic,
                previousCanSend ? "ALLOWED" : "BLOCKED",
                threshold.CanSend ? "ALLOWED" : "BLOCKED",
                currentBalance,
                threshold.MinimumSendBalance,
                participant.Currency);
        }

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
                "Balance updated for BIC {Bic}: Zone={Zone}, Balance={Balance:N2} {Currency}, CanSend={CanSend}, AvailableBalance={AvailableBalance:N2}",
                participant.Bic,
                currentZone,
                currentBalance,
                participant.Currency,
                threshold.CanSend,
                currentBalance - threshold.MinimumSendBalance);
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
            ThresholdZone.Amber => await BuildAmberAlertAsync(threshold, currentBalance, currency, cancellationToken),
            ThresholdZone.Red => await BuildRedAlertAsync(threshold, currentBalance, currency, cancellationToken),
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
                    true);
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

    private async Task<(string Subject, string Body)> BuildAmberAlertAsync(
        Domain.Models.BL.ParticipantThreshold threshold,
        decimal currentBalance,
        string currency,
        CancellationToken cancellationToken)
    {
        var currentPercentage = (currentBalance / threshold.GoldenAmount) * 100m;
        var subject = $"URGENT: Settlement Account Balance Alert - {threshold.Bic} ({currentPercentage:N2}%)";

        var values = new Dictionary<string, string>
        {
            { "CurrentBalance", currentBalance.ToString("N2") },
            { "Currency", currency },
            { "LimitPercentage", currentPercentage.ToString("N2") }
        };

        var body = await _templateRenderer.RenderAsync("Templates", "balance-alert-amber.html", values, cancellationToken);
        return (subject, body);
    }

    private async Task<(string Subject, string Body)> BuildRedAlertAsync(
        Domain.Models.BL.ParticipantThreshold threshold,
        decimal currentBalance,
        string currency,
        CancellationToken cancellationToken)
    {
        var currentPercentage = (currentBalance / threshold.GoldenAmount) * 100m;
        var subject = $"CRITICAL: Settlement Account Balance - {threshold.Bic} ({currentPercentage:N2}%)";

        var values = new Dictionary<string, string>
        {
            { "CurrentBalance", currentBalance.ToString("N2") },
            { "Currency", currency },
            { "LimitPercentage", currentPercentage.ToString("N2") }
        };

        var body = await _templateRenderer.RenderAsync("Templates", "balance-alert-red.html", values, cancellationToken);
        return (subject, body);
    }
}
