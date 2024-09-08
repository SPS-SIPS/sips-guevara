using Org.BouncyCastle.X509;

namespace SPSS.Domain.Interfaces;
public interface ICertificateRetriever
{
    X509Certificate? GetCertificateBySerialNumber(string serialNumber);
    string ConvertCertificateToPem(X509Certificate certificate);
    void GetAllCertificates();
}
