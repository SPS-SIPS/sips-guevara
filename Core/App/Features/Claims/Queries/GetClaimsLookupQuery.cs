namespace SPSS.App.Features.Claims.Queries;
[Authorize]
public sealed record GetClaimsLookupQuery() : IRequest<Response<List<Lookup<string>>>>;

public sealed class GetClaimsLookupQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetClaimsLookupQuery, Response<List<Lookup<string>>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<List<Lookup<string>>>> Handle(GetClaimsLookupQuery request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Claims.AsNoTracking().ProjectTo<Lookup<string>>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new Response<List<Lookup<string>>>(entity);
    }
}
