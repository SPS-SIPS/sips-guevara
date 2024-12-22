namespace SPSS.App.Features.VPNs.Queries;
[Authorize]
public sealed record GetVPNTunnelsQuery(int VPNId) : IRequest<Response<List<TunnelDto>>>;

public sealed class GetVPNTunnelsQueryValidator : AbstractValidator<GetVPNTunnelsQuery>
{
    public GetVPNTunnelsQueryValidator()
    {
        RuleFor(e => e.VPNId).NotNull();
    }
}

public sealed class GetVPNTunnelsQueryHandler(IStorageBroker storage, IMapper mapper, AppSettings appSettings) : IRequestHandler<GetVPNTunnelsQuery, Response<List<TunnelDto>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly AppSettings _appSettings = appSettings;

    public async ValueTask<Response<List<TunnelDto>>> Handle(GetVPNTunnelsQuery request, CancellationToken cancellationToken)
    {
        var data = await _storage.Tunnels.AsQueryable().AsNoTracking().Where(x => x.VPNId.Equals(request.VPNId))
            .ProjectTo<TunnelDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken);

        return new Response<List<TunnelDto>>(data);
    }
}
