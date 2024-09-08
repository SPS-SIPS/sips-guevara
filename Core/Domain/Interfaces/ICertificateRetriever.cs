using Org.BouncyCastle.X509;

namespace SPSS.Domain.Interfaces;
public interface ICertificateRetriever
{
    string ConvertCertificateToPem(X509Certificate certificate);
    Task GetAllCertificates();
}
