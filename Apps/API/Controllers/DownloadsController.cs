using SPSS.App.Features.Downloads.Queries;
using SPSS.App.Features.NSI.Queries;
namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class DownloadsController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<DownloadDto>> GetList([FromQuery] GetDownloadsListQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("getNSIFiles")]
    public async Task<PagedResponse<NetSettlementHeaderDto>> GetNSIFiles([FromQuery] GetSettlementFileQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("getNSIFile")]
    public async Task<IActionResult> GetNSIFile([FromQuery] DownloadSettlementFileQuery query, CancellationToken ct)
    {
        var response = await Mediator.Send(query, ct);
        var fileName = query.Id + ".zip" ?? "settlement_file.zip";
        return File(response.Data, "application/octet-stream", fileName);
    }
}
