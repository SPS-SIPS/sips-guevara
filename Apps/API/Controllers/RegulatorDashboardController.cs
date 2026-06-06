using SPSS.App.Features.RegulatorDashboard.Queries;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/regulator-dashboard")]
public class RegulatorDashboardController : BaseController
{
    [HttpGet("realtime-monitoring-cards")]
    public async Task<Response<List<RegulatorMonitoringCardDto>>> GetRealtimeMonitoringCards(
        [FromQuery] GetRegulatorMonitoringCardsQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("transaction-volume-hourly")]
    public async Task<Response<Dictionary<string, int>>> GetTransactionVolumeHourly(
        [FromQuery] GetRegulatorTransactionVolumeHourlyQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("velocity-alerts")]
    public async Task<Response<List<RegulatorVelocityAlertDto>>> GetVelocityAlerts(
        [FromQuery] GetRegulatorVelocityAlertsQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("velocity-alert-heatmap")]
    public async Task<Response<RegulatorVelocityAlertHeatmapDto>> GetVelocityAlertHeatmap(
        [FromQuery] GetRegulatorVelocityAlertHeatmapQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);
}
