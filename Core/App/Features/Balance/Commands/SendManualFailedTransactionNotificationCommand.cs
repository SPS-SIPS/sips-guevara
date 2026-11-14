namespace SPSS.App.Features.Balance.Commands;

[Authorize(Roles = UpdateTransactions)]
public sealed record SendManualFailedTransactionNotificationCommand(string Bic, DateTimeOffset From, DateTimeOffset To)
    : IRequest<Response<OracleFailedTransactionNotificationResult>>;

public sealed class SendManualFailedTransactionNotificationValidator : AbstractValidator<SendManualFailedTransactionNotificationCommand>
{
    public SendManualFailedTransactionNotificationValidator()
    {
        RuleFor(x => x.Bic).NotEmpty().Matches("^[A-Za-z0-9]{8}([A-Za-z0-9]{3})?$");
        RuleFor(x => x.To).GreaterThan(x => x.From).LessThanOrEqualTo(_ => DateTimeOffset.UtcNow);
        RuleFor(x => x).Must(x => x.To - x.From <= TimeSpan.FromDays(7))
            .WithMessage("The report window cannot exceed seven days.");
    }
}

public sealed class SendManualFailedTransactionNotificationHandler(IOracleFailedTransactionNotificationService notifications)
    : IRequestHandler<SendManualFailedTransactionNotificationCommand, Response<OracleFailedTransactionNotificationResult>>
{
    public async ValueTask<Response<OracleFailedTransactionNotificationResult>> Handle(
        SendManualFailedTransactionNotificationCommand request, CancellationToken ct)
    {
        var result = await notifications.NotifyInstitutionAsync(request.Bic, request.From, request.To, ct);
        return new Response<OracleFailedTransactionNotificationResult>(result) { Message = result.Message };
    }
}
