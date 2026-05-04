using static SPSS.Domain.Common.RTGSHelpers;
namespace SPSS.App.Features.NSI.Queries;

[Authorize(Roles = QueryNSI)]
public sealed record GetSettlementSummaryFileQuery(long Id, bool Fees = false) : IRequest<Response<(MemoryStream, string)>>;
public sealed class GetSettlementSummaryFileQueryValidator : AbstractValidator<GetSettlementSummaryFileQuery>
{
    public GetSettlementSummaryFileQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}
public sealed class GetSettlementSummaryFileQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetSettlementSummaryFileQuery, Response<(MemoryStream, string)>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    public async ValueTask<Response<(MemoryStream, string)>> Handle(GetSettlementSummaryFileQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.NetSettlementDetails
            .Where(s => s.NetSettlementHeaderId == request.Id)
            .Include(s => s.NetSettlementHeader)
            .AsNoTracking();

        if (request.Fees)
        {
            query = query.Where(s => s.IsFee);
        }
        else
        {
            query = query.Where(s => !s.IsFee);
        }

        var result = await query
            .ToListAsync(cancellationToken);

        if (result is null || result.Count == 0)
        {
            throw new NotFoundException($"No settlement details found for Id: {request.Id}");
        }

        var header = result.First().NetSettlementHeader ?? throw new NotFoundException($"No settlement header found for Id: {request.Id}");
        var to = header.To;
        var fileName = header.OriginalName.Replace(".rtgs.xml", string.Empty);
        if (request.Fees)
        {
            fileName += "_netted_fees.csv";
        }
        else
        {
            fileName += "_netted_transactions.csv";
        }

        var netted = NetTransactions(result, "TTC", to);
        var nettedDto = _mapper.Map<List<NetSettlementSummaryDto>>(netted);
        var memoryStream = await CSVHelper.WriteRecordsToCsv(nettedDto);
        return Response<(MemoryStream, string)>.Success((memoryStream, fileName), 0);
    }
}
