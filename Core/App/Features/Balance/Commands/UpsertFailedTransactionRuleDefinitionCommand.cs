namespace SPSS.App.Features.Balance.Commands;

[Authorize(Roles = UpdateTransactions)]
public sealed record UpsertFailedTransactionRuleDefinitionCommand(
    string RuleId,
    string Label,
    string? Description,
    string? Category,
    bool Active = true) : IRequest<Response<FailedTransactionRuleDefinitionDto>>;

public sealed class UpsertFailedTransactionRuleDefinitionCommandHandler(IStorageBroker storage)
    : IRequestHandler<UpsertFailedTransactionRuleDefinitionCommand, Response<FailedTransactionRuleDefinitionDto>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<FailedTransactionRuleDefinitionDto>> Handle(
        UpsertFailedTransactionRuleDefinitionCommand request,
        CancellationToken cancellationToken)
    {
        var ruleId = request.RuleId?.Trim();
        var label = request.Label?.Trim();

        if (string.IsNullOrWhiteSpace(ruleId))
        {
            return Response<FailedTransactionRuleDefinitionDto>.BadRequest("RuleId is required");
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return Response<FailedTransactionRuleDefinitionDto>.BadRequest("Label is required");
        }

        var entity = await _storage.FailedTransactionRuleDefinitions
            .FirstOrDefaultAsync(x => x.RuleId == ruleId, cancellationToken);

        if (entity is null)
        {
            entity = new FailedTransactionRuleDefinition
            {
                RuleId = ruleId
            };

            await _storage.FailedTransactionRuleDefinitions.AddAsync(entity, cancellationToken);
        }

        entity.Label = label;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        entity.Active = request.Active;

        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<FailedTransactionRuleDefinitionDto>(
            new FailedTransactionRuleDefinitionDto
            {
                Id = entity.Id,
                RuleId = entity.RuleId,
                Label = entity.Label,
                Description = entity.Description,
                Category = entity.Category,
                Active = entity.Active
            });
    }
}
