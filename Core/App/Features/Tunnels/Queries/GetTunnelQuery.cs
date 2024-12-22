namespace SPSS.App.Features.Tunnels.Queries;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryTunnels)]
public sealed record GetTunnelQuery(int Id) : IRequest<Response<TunnelDto>>;

public sealed class GetTunnelQueryValidator : AbstractValidator<GetTunnelQuery>
{
    public GetTunnelQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}

public sealed class GetTunnelQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetTunnelQuery, Response<TunnelDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<TunnelDto>> Handle(GetTunnelQuery request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Tunnels
        .Where(x => x.Id.Equals(request.Id))
        .AsNoTracking().ProjectTo<TunnelDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Can't find the selected Tunnel!");
        return new Response<TunnelDto>(entity);
    }
}
