namespace SPSS.App.Features.Balance.Commands;

[Authorize(Roles = UpdateTransactions)]
public sealed record UpdateFailedTransactionRuleIdsCommand(long IpsTransactionId, List<string> RuleIds) : IRequest<Response<long>>;

public sealed class UpdateFailedTransactionRuleIdsCommandHandler(IStorageBroker storage)
    : IRequestHandler<UpdateFailedTransactionRuleIdsCommand, Response<long>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<long>> Handle(UpdateFailedTransactionRuleIdsCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _storage.FailedTransactions
            .FirstOrDefaultAsync(x => x.IpsTransactionId == request.IpsTransactionId, cancellationToken);

        if (transaction is null)
        {
            return Response<long>.NotFound($"Failed transaction with IPS transaction ID {request.IpsTransactionId} was not found");
        }

        transaction.RuleIds = request.RuleIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        _storage.FailedTransactions.Update(transaction);
        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<long>(transaction.IpsTransactionId);
    }
}
