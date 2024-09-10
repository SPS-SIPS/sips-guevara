namespace SPSS.App.Features.Certificates.Commands;
[Authorize(Roles = DownloadCertificates)]
public sealed record DownloadCertificateCommand(string SerialNumber, string IssuerDN) : IRequest<Response<CertificateDownloadDto>>;
public sealed class DownloadCertificateCommandValidator : AbstractValidator<DownloadCertificateCommand>
{
    public DownloadCertificateCommandValidator()
    {
        RuleFor(x => x.SerialNumber).NotNull().NotEqual(default(string));
        // IssuerDN must be equal to CN=sps issuing CA, DC=sps, DC=local always, but not case sensitive can be sps issuing ca or sps issuing CA
        RuleFor(x => x.IssuerDN).NotNull().NotEqual(default(string)).Must(BeValidIssuerDN).WithMessage("IssuerDN must be equal to CN=sps issuing CA, DC=sps, DC=local (case insensitive)");
    }
    private bool BeValidIssuerDN(string issuerDN)
    {
        var validIssuerDNs = new List<string>
        {
            "CN=sps issuing CA, DC=sps, DC=local",
            "cn=sps issuing CA,dc=sps,dc=local"
        };

        return validIssuerDNs.Any(validIssuerDN => string.Equals(issuerDN.Trim(), validIssuerDN.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class DownloadCertificateCommandHandler(IStorageBroker storage, IIdentityService identity, AppSettings settings) : IRequestHandler<DownloadCertificateCommand, Response<CertificateDownloadDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IIdentityService _identity = identity;
    private readonly AppSettings _settings = settings;


    public async ValueTask<Response<CertificateDownloadDto>> Handle(DownloadCertificateCommand request, CancellationToken cancellationToken)
    {
        var cacheInMins = _settings.CacheInMins ?? 20;
        var certResponse = new CertificateDownloadDto { Content = string.Empty };

        var certificate = await _storage.Certificates
        .Where(x => x.Id!.Equals(request.SerialNumber))
        .SingleOrDefaultAsync(cancellationToken);

        if (certificate is null)
        {
            return Response<CertificateDownloadDto>.NotFound("Certificate not found", null);
        }

        if (certificate.Active == false)
        {
            return Response<CertificateDownloadDto>.NotFound("Certificate not found", null);
        }

        if (certificate.IsRevoked)
        {
            certificate.Active = false;
            await _storage.SaveChangesAsync(cancellationToken);
            return Response<CertificateDownloadDto>.NotFound("Certificate not found", null);
        }

        if (certificate.ExpireDate < DateTimeOffset.UtcNow)
        {
            certificate.Active = false;
            await _storage.SaveChangesAsync(cancellationToken);
            return Response<CertificateDownloadDto>.NotFound("Certificate not found", null);
        }
        certResponse.Content = certificate.Name;

        if (_identity.Username != "admin")
        {
            certificate.Downloads.Add(new Download
            {
                Date = DateTime.UtcNow.ToUniversalTime(),
                ParticipantId = _identity.TenantId,
                Username = _identity.Username,
            });

            await _storage.SaveChangesAsync(cancellationToken);
        }
        return Response<CertificateDownloadDto>.Success(certResponse, cacheInMins);
    }
}