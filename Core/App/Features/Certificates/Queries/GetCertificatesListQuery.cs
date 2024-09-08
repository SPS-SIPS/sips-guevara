using SPSS.App.Common;
namespace SPSS.App.Features.Certificates.Queries;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryCertificates)]
public sealed record GetCertificatesListQuery(bool? Active, bool? IsRevoked, int? UserId, int? ParticipantId) : PaginationFilter, IRequest<PagedResponse<CertificateDto>>;

public sealed class GetCertificatesListQueryHandler(IStorageBroker storage, IMapper mapper, IIdentityService identityService) : IRequestHandler<GetCertificatesListQuery, PagedResponse<CertificateDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly IIdentityService _identityService = identityService;

    public async ValueTask<PagedResponse<CertificateDto>> Handle(GetCertificatesListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Certificates.AsQueryable().AsNoTracking();

        if (request.Active.HasValue)
        {
            query = query.Where(x => x.Active.Equals(request.Active.Value));
        }

        if (request.IsRevoked.HasValue)
        {
            query = query.Where(x => x.IsRevoked.Equals(request.IsRevoked.Value));
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(x => x.Participant.UserId.Equals(request.UserId.Value));
        }

        if (request.ParticipantId.HasValue)
        {
            query = query.Where(x => x.ParticipantId.Equals(request.ParticipantId.Value));
        }


        query = request.Sort switch
        {
            "name_ascend" => query.OrderBy(x => x.Name),
            "name_descend" => query.OrderByDescending(x => x.Name),
            _ => query.OrderByDescending(x => x.CachedOn)
        };

        return await PaginationHelper<CertificateDto>.CreateAsync(query.ProjectTo<CertificateDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
