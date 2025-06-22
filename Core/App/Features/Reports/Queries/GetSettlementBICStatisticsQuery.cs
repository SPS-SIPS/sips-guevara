namespace SPSS.App.Features.Reports.Queries;

using static SPSS.Domain.Common.KnownClaims;

[Authorize(Roles = QueryReports)]
public sealed record GetSettlementBICStatisticsQuery(long Id) : IRequest<Response<List<BicStatisticsDto>>>;
public sealed class GetSettlementBICStatisticsQueryValidator : AbstractValidator<GetSettlementBICStatisticsQuery>
{
    public GetSettlementBICStatisticsQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}
public sealed class GetSettlementBICStatisticsQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetSettlementBICStatisticsQuery, Response<List<BicStatisticsDto>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    public async ValueTask<Response<List<BicStatisticsDto>>> Handle(GetSettlementBICStatisticsQuery request, CancellationToken cancellationToken)
    {
        var settlementDetails = await _storage.BicStatistics.Where(x => x.NetSettlementHeaderId == request.Id)
            .Include(x => x.TopReceivers)
            .Include(x => x.TopSenders)
            .ProjectTo<BicStatisticsDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new Response<List<BicStatisticsDto>>(settlementDetails);
    }
}
