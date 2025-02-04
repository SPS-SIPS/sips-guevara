using SPSS.App.Features.Downloads.Queries;
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
    public async Task<Response<List<NSISheets>>> GetNSIFiles(CancellationToken ct)
    => await Mediator.Send(new GetNSIFilesQuery(), ct);

    [HttpGet("getNSIFile")]
    public async Task<IActionResult> GetNSIFile([FromQuery] GetNSIFileQuery query, CancellationToken ct)
    {
        var response = await Mediator.Send(query, ct);

        if (response is null)
        {
            return NotFound("File not found");
        }

        return File(response, "application/octet-stream", query.FileName);
    }
}
