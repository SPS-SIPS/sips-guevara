namespace SPSS.App.Features.Balance.Commands;

[Authorize(Roles = UpdateTransactions)]
public sealed record UpdateFailedTransactionRuleIdsCommand(long IpsTransactionId, List<string> RuleIds) : IRequest<Response<long>>;

public sealed class UpdateFailedTransactionRuleIdsCommandHandler(
    IStorageBroker storage,
    SPSS.App.Features.Balance.Services.IFailedTransactionComplianceClassificationRefresher classificationRefresher)
    : IRequestHandler<UpdateFailedTransactionRuleIdsCommand, Response<long>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly SPSS.App.Features.Balance.Services.IFailedTransactionComplianceClassificationRefresher _classificationRefresher = classificationRefresher;

    public async ValueTask<Response<long>> Handle(UpdateFailedTransactionRuleIdsCommand request, CancellationToken cancellationToken)
    {
        var transactionExists = await _storage.FailedTransactions
            .AnyAsync(x => x.Id == request.IpsTransactionId, cancellationToken);

        if (!transactionExists)
        {
            return Response<long>.NotFound($"Failed transaction with IPS transaction ID {request.IpsTransactionId} was not found");
        }

        var ruleIds = request.RuleIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        var assignment = await _storage.FailedTransactionRuleAssignments
            .FirstOrDefaultAsync(x => x.FailedTransactionId == request.IpsTransactionId, cancellationToken);

        if (assignment is null)
        {
            await _storage.FailedTransactionRuleAssignments.AddAsync(
                new FailedTransactionRuleAssignment
                {
                    FailedTransactionId = request.IpsTransactionId,
                    RuleIds = ruleIds
                },
                cancellationToken);
        }
        else
        {
            assignment.RuleIds = ruleIds;
            _storage.FailedTransactionRuleAssignments.Update(assignment);
        }

        await _storage.SaveChangesAsync(cancellationToken);
        await _classificationRefresher.RefreshByFailedTransactionIdsAsync([request.IpsTransactionId], cancellationToken);

        return new Response<long>(request.IpsTransactionId);
    }
}
