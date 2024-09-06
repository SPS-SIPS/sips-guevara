namespace SPSS.App.Features.Participants.Queries;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryParticipants)]
public sealed record GetParticipantQuery(string Id) : IRequest<Response<ParticipantDto>>;

public sealed class GetParticipantQueryValidator : AbstractValidator<GetParticipantQuery>
{
    public GetParticipantQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}

public sealed class GetParticipantQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetParticipantQuery, Response<ParticipantDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<ParticipantDto>> Handle(GetParticipantQuery request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Participants.AsNoTracking().ProjectTo<ParticipantDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken) ?? throw new NotFoundException("Can't find the selected Participant!");
        return new Response<ParticipantDto>(entity);
    }
}
