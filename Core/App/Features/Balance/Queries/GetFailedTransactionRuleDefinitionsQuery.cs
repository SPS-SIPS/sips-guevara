using SPSS.App.Common;

namespace SPSS.App.Features.Balance.Queries;

[Authorize(Roles = QueryTransactions)]
public sealed record GetFailedTransactionRuleDefinitionsQuery(
    string? RuleId,
    string? Label,
    string? Category,
    bool? Active) : PaginationFilter, IRequest<PagedResponse<FailedTransactionRuleDefinitionDto>>;

public sealed class GetFailedTransactionRuleDefinitionsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetFailedTransactionRuleDefinitionsQuery, PagedResponse<FailedTransactionRuleDefinitionDto>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<PagedResponse<FailedTransactionRuleDefinitionDto>> Handle(
        GetFailedTransactionRuleDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _storage.FailedTransactionRuleDefinitions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.RuleId))
        {
            query = query.Where(x => x.RuleId == request.RuleId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Label))
        {
            var label = request.Label.Trim().ToLower();
            query = query.Where(x => x.Label.ToLower().Contains(label));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(x => x.Category == request.Category.Trim());
        }

        if (request.Active.HasValue)
        {
            query = query.Where(x => x.Active == request.Active.Value);
        }

        var result = query
            .OrderBy(x => x.RuleId)
            .Select(x => new FailedTransactionRuleDefinitionDto
            {
                Id = x.Id,
                RuleId = x.RuleId,
                Label = x.Label,
                Description = x.Description,
                Category = x.Category,
                Active = x.Active
            });

        return await PaginationHelper<FailedTransactionRuleDefinitionDto>.CreateAsync(result, request, cancellationToken);
    }
}
