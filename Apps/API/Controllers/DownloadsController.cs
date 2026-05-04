using SPSS.App.Features.Downloads.Queries;
using SPSS.App.Features.NSI.Queries;
using Microsoft.AspNetCore.Authorization;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class DownloadsController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<DownloadDto>> GetList([FromQuery] GetDownloadsListQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [Authorize]
    [HttpPost("manual-nsi")]
    public async Task<Response<string>> CreateManualNSI([FromBody] App.Features.Downloads.Commands.CreateManualNSICommand command, CancellationToken ct)
    => await Mediator.Send(command, ct);

    [HttpGet("getNSIFiles")]
    public async Task<PagedResponse<NetSettlementHeaderDto>> GetNSIFiles([FromQuery] GetSettlementFileQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("getNSIFile")]
    public async Task<IActionResult> GetNSIFile([FromQuery] DownloadSettlementFileQuery query, CancellationToken ct)
    {
        var response = await Mediator.Send(query, ct);
        var fileName = response.Data.Item2;
        var fileBytes = response.Data.Item1;
        string contentType;
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "text/csv";
        }
        else if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "application/xml";
        }
        else if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "application/zip";
        }
        else
        {
            contentType = "application/octet-stream";
        }
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        Response.Headers.Append("Content-Type", contentType);
        Response.Headers.Append("Content-Length", fileBytes.Length.ToString());
        Response.Headers.Append("Cache-Control", "public, max-age=3600");
        return File(fileBytes, contentType, fileName);
    }

    [HttpGet("GetDaySettlementInfo")]
    public async Task<Response<List<NetSettlementHeaderSimpleDto>>> GetSettlementInfo([FromQuery] GetSettlementInfoQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("getNSIFile4party")]
    public async Task<IActionResult> GetNSIFile4Party([FromQuery] GetSettlementDetailsFileQuery query, CancellationToken ct)
    {
        var response = await Mediator.Send(query, ct);
        var fileName = response.Data.Item2;
        var fileBytes = response.Data.Item1;
        string contentType;
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "text/csv";
        }
        else if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "application/xml";
        }
        else if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "application/zip";
        }
        else
        {
            contentType = "application/octet-stream";
        }
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
        Response.Headers.Append("Pragma", "no-cache");
        Response.Headers.Append("Expires", "0");
        return File(fileBytes, contentType, fileName);
    }

    [HttpGet("SettlementSummaryFile")]
    public async Task<IActionResult> GetSettlementSummaryFile([FromQuery] GetSettlementSummaryFileQuery query, CancellationToken ct)
    {
        var response = await Mediator.Send(query, ct);
        var fileName = response.Data.Item2;
        var fileBytes = response.Data.Item1;
        string contentType;
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "text/csv";
        }
        else if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "application/xml";
        }
        else if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "application/zip";
        }
        else
        {
            contentType = "application/octet-stream";
        }
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        Response.Headers.Append("Content-Type", contentType);
        Response.Headers.Append("Content-Length", fileBytes.Length.ToString());
        Response.Headers.Append("Cache-Control", "public, max-age=3600");
        return File(fileBytes, contentType, fileName);
    }
}
