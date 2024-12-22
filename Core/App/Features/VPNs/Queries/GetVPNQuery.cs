namespace SPSS.App.Features.VPNs.Queries;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryVPNs)]
public sealed record GetVPNQuery(int Id) : IRequest<Response<VPNDto>>;

public sealed class GetVPNQueryValidator : AbstractValidator<GetVPNQuery>
{
    public GetVPNQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}

public sealed class GetVPNQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetVPNQuery, Response<VPNDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<VPNDto>> Handle(GetVPNQuery request, CancellationToken cancellationToken)
    {
        var entity = await _storage.VPNs.AsNoTracking().ProjectTo<VPNDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken) ?? throw new NotFoundException("Can't find the selected VPN!");
        return new Response<VPNDto>(entity);
    }
}
