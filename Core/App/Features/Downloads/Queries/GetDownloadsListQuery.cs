using SPSS.App.Common;
namespace SPSS.App.Features.Downloads.Queries;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryDownloads)]
public sealed record GetDownloadsListQuery(int? UserId, string ParticipantId, string CertificateId) : PaginationFilter, IRequest<PagedResponse<DownloadDto>>;

public sealed class GetDownloadsListQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetDownloadsListQuery, PagedResponse<DownloadDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<PagedResponse<DownloadDto>> Handle(GetDownloadsListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Downloads.AsQueryable().AsNoTracking();

        if (!string.IsNullOrEmpty(request.CertificateId))
        {
            query = query.Where(x => x.CertificateId.Contains(request.CertificateId));
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(x => x.Participant.UserId.Equals(request.UserId.Value));
        }

        if (!string.IsNullOrEmpty(request.ParticipantId))
        {
            query = query.Where(x => x.ParticipantId.Contains(request.ParticipantId));
        }

        query = query.OrderByDescending(x => x.Date);

        return await PaginationHelper<DownloadDto>.CreateAsync(query.ProjectTo<DownloadDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
