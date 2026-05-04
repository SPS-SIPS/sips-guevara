using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace SPSS.Infrastructure.Services;
public sealed class CertificateValidator(IConfiguration configuration, ILogger<CertificateValidator> logger, IStorageBroker storage)
{
    private readonly string _clrAddress = configuration["LDAP:CLRAddress"] ?? throw new ArgumentNullException("LDAP:CLRAddress");
    private readonly ILogger<CertificateValidator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IStorageBroker _storage = storage;

    public async Task ValidateCertificates(CancellationToken ct)
    {
        var certificates = await _storage.Certificates.Where(x => x.IsRevoked == false).ToListAsync(ct);
        var crl = await GetCrlFromUrlAsync(_clrAddress);

        if (crl == null)
        {
            _logger.LogError("CRL is not valid.");
            return;
        }

        foreach (var item in certificates)
        {
            var cert = CertificateFromPem(item.Name);

            if (cert == null)
            {
                _logger.LogError("Certificate with SL: {Serial No.#} is not valid.", item.Id);

                item.Active = false;
                continue;
            }


            if (item.ExpireDate < DateTime.Now)
            {
                _logger.LogInformation("Certificate with SL: {Serial No.#} is expired.", item.Id);
                item.Active = false;
                continue;
            }

            // Check if the certificate is revoked
            if (IsCertificateRevoked(crl, cert))
            {
                _logger.LogInformation("Certificate with SL: {Serial No.#} is revoked.", item.Id);
                item.IsRevoked = true;
                item.Active = false;
            }


        }
        await _storage.SaveChangesAsync(ct);
    }

    // Check if a certificate is revoked by the CRL
    static bool IsCertificateRevoked(X509Crl crl, X509Certificate certificate)
    {
        // Get the revocation entry for the certificate's serial number
        var revokedEntry = crl.GetRevokedCertificate(certificate.SerialNumber);

        // If the revocation entry exists, the certificate is revoked
        return revokedEntry != null;
    }

    public static X509Certificate CertificateFromPem(string certPem)
    {
        using StringReader reader = new(certPem);
        PemReader pemReader = new(reader);

        List<X509Certificate> chain = [];

        while (pemReader.ReadObject() is X509Certificate cert)
        {
            chain.Add(cert);
        }

        if (chain.Count == 0)
        {
            throw new Exception("No certificates found in the PEM file.");
        }

        return chain[0];
    }
    private async Task<X509Crl?> GetCrlFromUrlAsync(string crlUrl)
    {
        try
        {
            using HttpClient client = new();
            byte[] crlData = await client.GetByteArrayAsync(crlUrl);

            X509CrlParser crlParser = new();
            return crlParser.ReadCrl(crlData);
        }
        catch (System.Exception ex)
        {
            _logger.LogError("CLR File Download Failed exception message: {message}", ex.Message);
            return null;
        }
    }
}