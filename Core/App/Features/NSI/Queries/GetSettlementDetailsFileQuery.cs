using SPSS.App.Common;
namespace SPSS.App.Features.NSI.Queries;

[Authorize(Roles = QueryNSI)]
[Authorize(Roles = QueryParticipantNSI)]
public sealed record GetSettlementDetailsFileQuery(long Id, string? Party) : IRequest<Response<(MemoryStream, string)>>;
public sealed class GetSettlementDetailsFileQueryValidator : AbstractValidator<GetSettlementDetailsFileQuery>
{
    public GetSettlementDetailsFileQueryValidator(IIdentityService identityService)
    {
        RuleFor(e => e.Id).NotNull();
        RuleFor(e => e.Party).NotEmpty().When(e => !identityService.IsInRole(QueryParticipantNSI))
            .WithMessage("Party is required when querying settlement details file");
    }
}
public sealed class GetSettlementDetailsFileQueryHandler(IStorageBroker storage, IMapper mapper, IIdentityService identity) : IRequestHandler<GetSettlementDetailsFileQuery, Response<(MemoryStream, string)>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly IIdentityService _identity = identity;

    public async ValueTask<Response<(MemoryStream, string)>> Handle(GetSettlementDetailsFileQuery request, CancellationToken cancellationToken)
    {
        var party = _identity.IsInRole(QueryParticipantNSI)
            ? _identity.TenantId
            : request.Party;

        if (string.IsNullOrWhiteSpace(party))
        {
            throw new BadRequestException("Party is required for this query, Contact SPS Team for assistance.");
        }

        var txIds = await _storage.NetSettlementDetails
            .Where(s => s.NetSettlementHeaderId == request.Id && (s.DebtorBic == party || s.CreditorBic == party))
            .Select(s => s.TxId)
            .Distinct().AsNoTracking()
            .ToListAsync(cancellationToken);

        var query = _storage.NetSettlementDetails
            .Where(s => s.NetSettlementHeaderId == request.Id && txIds.Contains(s.TxId))
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.TxId)
            .AsNoTracking();

        var result = await query.ProjectTo<NetSettlementDetailDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        if (result is null || result.Count == 0)
        {
            throw new NotFoundException($"No settlement details found for Id: {request.Id} and Party: {request.Party}");
        }

        var memoryStream = await CSVHelper.WriteRecordsToCsv(result);
        return Response<(MemoryStream, string)>.Success((memoryStream, $"Settled_Transactions_{request.Id}_{request.Party}.csv"), 0);
    }
}
