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
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{response.Data.Item2.Replace(".xml", ".zip")}\"");
        Response.Headers.Append("Content-Type", "application/octet-stream");
        Response.Headers.Append("Content-Length", response.Data.Item1.Length.ToString());
        Response.Headers.Append("Cache-Control", "public, max-age=3600");
        return File(response.Data.Item1, "application/octet-stream", response.Data.Item2.Replace(".xml", ".zip"));
    }
}
