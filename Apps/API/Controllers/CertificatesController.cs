using SPSS.App.Features.Certificates.Commands;
using SPSS.App.Features.Certificates.Queries;
namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class CertificatesController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<CertificateDto>> GetList([FromQuery] GetCertificatesListQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("{Id}")]
    public async Task<Response<CertificateDto>> Get([FromRoute] GetCertificateQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpPost("download")]
    public async Task<Response<CertificateDownloadDto>> Post([FromBody] DownloadCertificateCommand query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpPut]
    public async Task<Response<string>> Put([FromBody] EditCertificateCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpDelete("{Id}")]
    public async Task<Response<string>> Delete([FromRoute] DeleteCertificateCommand query, CancellationToken ct)
    => await Mediator.Send(query, ct);

}
