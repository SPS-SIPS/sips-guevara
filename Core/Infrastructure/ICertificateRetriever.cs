using Org.BouncyCastle.X509;

namespace SPSS.Infrastructure;
public interface ICertificateRetriever
{
    string ConvertCertificateToPem(X509Certificate certificate);
    Task GetAllCertificates();
}
