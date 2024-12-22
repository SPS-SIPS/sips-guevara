namespace SPSS.App.Features.Endpoints.Queries;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryEndpoints)]
public sealed record GetEndpointQuery(int Id) : IRequest<Response<EndpointDto>>;

public sealed class GetEndpointQueryValidator : AbstractValidator<GetEndpointQuery>
{
    public GetEndpointQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}

public sealed class GetEndpointQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetEndpointQuery, Response<EndpointDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<EndpointDto>> Handle(GetEndpointQuery request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Endpoints.AsNoTracking().ProjectTo<EndpointDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken) ?? throw new NotFoundException("Can't find the selected Endpoint!");
        return new Response<EndpointDto>(entity);
    }
}
