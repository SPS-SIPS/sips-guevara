namespace SPSS.Domain.Common;

public class ApiClient
{
    public string ClientId { get; set; } = default!;
    public string ClientName { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public List<string> Scopes { get; set; } = [];
}