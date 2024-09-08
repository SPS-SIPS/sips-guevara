namespace SPSS.App.Features.Certificates.Commands;

using Org.BouncyCastle.Asn1.Crmf;
using Org.BouncyCastle.X509;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = DownloadCertificates)]
public sealed record DownloadCertificateCommand(string SerialNumber, string IssuerDN) : IRequest<Response<CertificateDownloadDto>>;
public sealed class DownloadCertificateCommandValidator : AbstractValidator<DownloadCertificateCommand>
{
    public DownloadCertificateCommandValidator()
    {
        RuleFor(x => x.SerialNumber).NotNull().NotEqual(default(string));
        RuleFor(x => x.IssuerDN).NotNull().NotEqual(default(string));
    }
}

public sealed class DownloadCertificateCommandHandler(IStorageBroker storage, IIdentityService identity, AppSettings settings) : IRequestHandler<DownloadCertificateCommand, Response<CertificateDownloadDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IIdentityService _identity = identity;
    private readonly AppSettings _settings = settings;


    public async ValueTask<Response<CertificateDownloadDto>> Handle(DownloadCertificateCommand request, CancellationToken cancellationToken)
    {
        var formateIssuer = request.IssuerDN.ToLower();
        var cacheInMins = _settings.CacheInMins ?? 20;
        var certResponse = new CertificateDownloadDto { Content = string.Empty };
        var certificate = await _storage.Certificates
        .Where(x => x.Id!.Equals(request.SerialNumber) && x.Issuer.ToLower().Contains(formateIssuer))
        .SingleOrDefaultAsync(cancellationToken);

        if (certificate is not null)
        {
            if (certificate.Active == false)
            {
                return Response<CertificateDownloadDto>.NotFound("Certificate not found", certResponse);
            }

            if (certificate.IsRevoked)
            {
                certificate.Active = false;
                await _storage.SaveChangesAsync(cancellationToken);
                return Response<CertificateDownloadDto>.NotFound("Certificate not found", certResponse);
            }

            if (certificate.ExpireDate < DateTimeOffset.UtcNow)
            {
                certificate.Active = false;
                await _storage.SaveChangesAsync(cancellationToken);
                return Response<CertificateDownloadDto>.NotFound("Certificate not found", certResponse);
            }

            certResponse.Content = certificate.Name;
            return Response<CertificateDownloadDto>.Success(certResponse, cacheInMins);
        }
        return Response<CertificateDownloadDto>.NotFound("Certificate not found", certResponse);
    }
}