namespace SPSS.Domain.Common;

public static class KnownClaims
{

    // User
    public const string CreateUser = "CUA";
    public const string UpdateUser = "UUA";
    public const string DeleteUser = "DUA";
    public const string QueryUser = "QUA";

    // Roles
    public const string CreateRole = "CR";
    public const string QueryRoles = "QR";
    public const string RemoveRole = "RR";
    public const string UpdateRole = "UR";


    // Participants
    public const string CreateParticipant = "CP";
    public const string QueryParticipants = "QP";
    public const string RemoveParticipant = "RP";
    public const string UpdateParticipant = "UP";

    // Certificates
    public const string RemoveCertificate = "RC";
    public const string UpdateCertificate = "UC";
    public const string QueryCertificates = "QC";
    public const string DownloadCertificates = "DC";

    // Downloads
    public const string QueryDownloads = "QD";

    // VPNs
    public const string CreateVPN = "CVPN";
    public const string QueryVPNs = "QVPN";
    public const string RemoveVPN = "RVPN";
    public const string UpdateVPN = "UVPN";

    // Tunnels
    public const string CreateTunnel = "CTs";
    public const string QueryTunnels = "QTs";
    public const string RemoveTunnel = "RTs";
    public const string UpdateTunnel = "UTs";

    // Endpoints
    public const string CreateEndpoint = "CEP";
    public const string QueryEndpoints = "QEP";
    public const string RemoveEndpoint = "REP";
    public const string UpdateEndpoint = "UEP";

    // NSI
    public const string QueryNSI = "QNSI";
    public const string DownloadNSI = "DNSI";
    public const string QueryParticipantNSI = "QPNSI";
}