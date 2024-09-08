using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace SPSS.Infrastructure.Services;
public sealed class LDAPCertificateFetcher : IDisposable, ICertificateRetriever
{
    private readonly string _baseDn;
    private readonly LdapConnection _ldapConnection;
    private readonly ILogger<LDAPCertificateFetcher> _logger;
    public LDAPCertificateFetcher(IConfiguration configuration, ILogger<LDAPCertificateFetcher> logger)
    {
        var ldapServer = configuration["LDAP:Server"] ?? throw new ArgumentNullException("LDAP:Server");
        var username = configuration["LDAP:Username"] ?? throw new ArgumentNullException("LDAP:Username");
        var password = configuration["LDAP:Password"] ?? throw new ArgumentNullException("LDAP:Password");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseDn = configuration["LDAP:BaseDN"] ?? throw new ArgumentNullException("LDAP:BaseDN");

        _ldapConnection = new LdapConnection(ldapServer)
        {
            Credential = new NetworkCredential(username, password)
        };
        _ldapConnection.Bind();
    }


    public X509Certificate? GetCertificateBySerialNumber(string serialNumber)
    {
        var searchFilter = $"(serialNumber={serialNumber})";
        var searchRequest = new SearchRequest(_baseDn, searchFilter, SearchScope.Subtree, "userCertificate");

        var searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);

        foreach (SearchResultEntry entry in searchResponse.Entries)
        {
            if (entry.Attributes["userCertificate"]?[0] is byte[] certData)
            {
                return ParseCertificate(certData);
            }
        }
        return null;
    }

    public void GetAllCertificates()
    {
        var searchFilter = $"(userCertificate=*)";
        var searchRequest = new SearchRequest(_baseDn, searchFilter, SearchScope.Subtree, "userCertificate");

        var searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);

        foreach (SearchResultEntry entry in searchResponse.Entries)
        {
            if (entry.Attributes["userCertificate"]?[0] is byte[] certData)
            {
                _logger.LogError("Certificate found {certData}", certData);
            }
        }
    }
    private static X509Certificate ParseCertificate(byte[] certData)
    {
        var parser = new X509CertificateParser();
        return parser.ReadCertificate(certData);
    }

    public string ConvertCertificateToPem(X509Certificate certificate)
    {
        StringBuilder pemStringBuilder = new();

        using (TextWriter textWriter = new StringWriter(pemStringBuilder))
        {
            PemWriter pemWriter = new(textWriter);
            pemWriter.WriteObject(certificate);
            pemWriter.Writer.Flush();
        }

        return pemStringBuilder.ToString();
    }

    public void Dispose()
    {
        _ldapConnection?.Dispose();
    }
}