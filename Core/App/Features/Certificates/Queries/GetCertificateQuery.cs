namespace SPSS.App.Features.Certificates.Queries;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryCertificates)]
public sealed record GetCertificateQuery(int Id) : IRequest<Response<CertificateDto>>;

public sealed class GetCertificateQueryValidator : AbstractValidator<GetCertificateQuery>
{
    public GetCertificateQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}

public sealed class GetCertificateQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetCertificateQuery, Response<CertificateDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<CertificateDto>> Handle(GetCertificateQuery request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Certificates.AsNoTracking().ProjectTo<CertificateDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken) ?? throw new NotFoundException("Can't find the selected Certificate!");
        return new Response<CertificateDto>(entity);
    }
}
