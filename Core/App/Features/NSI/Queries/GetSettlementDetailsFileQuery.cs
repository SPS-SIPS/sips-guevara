using SPSS.App.Common;
namespace SPSS.App.Features.NSI.Queries;

[Authorize(Roles = QueryNSI)]
public sealed record GetSettlementDetailsFileQuery(long Id, string Party) : IRequest<Response<(MemoryStream, string)>>;
public sealed class GetSettlementDetailsFileQueryValidator : AbstractValidator<GetSettlementDetailsFileQuery>
{
    public GetSettlementDetailsFileQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
        RuleFor(e => e.Party).NotNull();
    }
}
public sealed class GetSettlementDetailsFileQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetSettlementDetailsFileQuery, Response<(MemoryStream, string)>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<(MemoryStream, string)>> Handle(GetSettlementDetailsFileQuery request, CancellationToken cancellationToken)
    {
        var txIds = await _storage.NetSettlementDetails
            .Where(s => s.NetSettlementHeaderId == request.Id && (s.DebtorBic == request.Party || s.CreditorBic == request.Party))
            .Select(s => s.TxId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = _storage.NetSettlementDetails
            .Where(s => s.NetSettlementHeaderId == request.Id && txIds.Contains(s.TxId))
            .OrderByDescending(s => s.Date);

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
