using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace SPSS.Infrastructure.Services;
public sealed class CertificateValidator(IConfiguration configuration, ILogger<CertificateValidator> logger, IStorageBroker storage)
{
    private readonly string _clrAddress = configuration["LDAP:CLRAddress"] ?? throw new ArgumentNullException("LDAP:CLRAddress");
    private readonly ILogger<CertificateValidator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IStorageBroker _storage = storage;

    public async Task ValidateCertificates(CancellationToken ct)
    {
        var certificates = await _storage.Certificates.Where(x => !x.IsRevoked).ToListAsync(ct);
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
            item.IsRevoked = IsCertificateRevoked(crl, cert);

        }
        await _storage.SaveChangesAsync(ct);
    }

    // Check if a certificate is revoked by the CRL
    static bool IsCertificateRevoked(X509Crl crl, X509Certificate certificate)
    {
        // Get the revocation entry for the certificate's serial number
        X509CrlEntry revokedEntry = crl.GetRevokedCertificate(certificate.SerialNumber);

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

    // download the CRL from the CLR address
    public async Task DownloadCRL()
    {
        try
        {
            using WebClient client = new();
            byte[] data = await client.DownloadDataTaskAsync(_clrAddress);

            X509CrlParser crlParser = new();
            X509Crl crl = crlParser.ReadCrl(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading CRL");
        }
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