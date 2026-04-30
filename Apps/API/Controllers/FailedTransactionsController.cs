using SPSS.App.Features.Balance.Commands;
using SPSS.App.Features.Balance.Queries;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class FailedTransactionsController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<FailedTransactionRecordDto>> Get([FromQuery] GetFailedTransactionsQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPut("rules")]
    public async Task<Response<long>> PutRules([FromBody] UpdateFailedTransactionRuleIdsCommand command, CancellationToken ct)
        => await Mediator.Send(command, ct);

    [HttpPost("rules/csv")]
    public async Task<Response<FailedTransactionRuleIdsCsvUploadResultDto>> UploadRulesCsv([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            return Response<FailedTransactionRuleIdsCsvUploadResultDto>.BadRequest("CSV file is empty");
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var csvContent = await reader.ReadToEndAsync(ct);

        return await Mediator.Send(new UploadFailedTransactionRuleIdsCsvCommand(csvContent), ct);
    }
}
