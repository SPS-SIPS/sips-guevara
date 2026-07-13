using SPSS.App.Features.Balance.Queries;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class TransactionsController : BaseController
{
    private readonly IOracleFailedTransactionNotificationService _oracleFailedTransactionNotificationService;

    public TransactionsController(IOracleFailedTransactionNotificationService oracleFailedTransactionNotificationService)
    {
        _oracleFailedTransactionNotificationService = oracleFailedTransactionNotificationService;
    }

    [HttpGet]
    public async Task<PagedResponse<TransactionRecordDto>> Get(
        [FromQuery] GetTransactionsQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("summary")]
    public async Task<Response<TransactionSummaryDto>> GetSummary(
        [FromQuery] GetTransactionsSummaryQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("ingestion-batches")]
    public async Task<PagedResponse<TransactionIngestionBatchDto>> GetIngestionBatches(
        [FromQuery] GetOracleTransactionIngestionBatchesQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPost("failed-notifications/manual")]
    public async Task<Response<OracleFailedTransactionNotificationResult>> SendManualFailedTransactionNotification(
        [FromBody] ManualOracleFailedTransactionNotificationRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Bic))
        {
            return Response<OracleFailedTransactionNotificationResult>.BadRequest("Institution BIC is required");
        }

        if (request.From >= request.To)
        {
            return Response<OracleFailedTransactionNotificationResult>.BadRequest("Window start must be before window end");
        }

        var result = await _oracleFailedTransactionNotificationService.NotifyInstitutionAsync(
            request.Bic,
            request.From,
            request.To,
            ct);

        return new Response<OracleFailedTransactionNotificationResult>(result)
        {
            Message = result.Message
        };
    }
}
