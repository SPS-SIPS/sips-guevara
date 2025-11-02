using SPSS.App.Common;
namespace SPSS.App.Features.Participants.Queries;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryParticipants)]
public sealed record GetParticipantsListQuery(string? Name, bool? Active, bool? Deleted, int? UserId, string? Email) : PaginationFilter, IRequest<PagedResponse<ParticipantDto>>;

public sealed class GetParticipantsListQueryHandler(IStorageBroker storage, IMapper mapper, IIdentityService identityService) : IRequestHandler<GetParticipantsListQuery, PagedResponse<ParticipantDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly IIdentityService _identityService = identityService;

    public async ValueTask<PagedResponse<ParticipantDto>> Handle(GetParticipantsListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Participants.AsQueryable().AsNoTracking();

        if (!string.IsNullOrEmpty(request.Name))
        {
            query = query.Where(x => x.Name.Contains(request.Name));
        }


        if (!string.IsNullOrEmpty(request.Email))
        {
            query = query.Where(x => x.Email!.Contains(request.Email));
        }

        if (request.Active.HasValue)
        {
            query = query.Where(x => x.Active.Equals(request.Active.Value));
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(x => x.UserId.Equals(request.UserId.Value));
        }

        if (request.Deleted.HasValue)
        {
            query = query.Where(x => x.DeletedOn.HasValue == request.Deleted.Value);
        }

        query = request.Sort switch
        {
            "name_ascend" => query.OrderBy(x => x.Name),
            "name_descend" => query.OrderByDescending(x => x.Name),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        return await PaginationHelper<ParticipantDto>.CreateAsync(query.ProjectTo<ParticipantDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
