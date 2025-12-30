using SPSS.App.Features.Thresholds.Commands;
using SPSS.App.Features.Thresholds.Queries;
using SPSS.Domain.Models.DTOs;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class ParticipantThresholdsController : BaseController
{
    [HttpGet]
    public async Task<Response<List<ParticipantThresholdDto>>> GetList(CancellationToken ct)
        => await Mediator.Send(new GetParticipantThresholdsQuery(), ct);

    [HttpGet("{Id}")]
    public async Task<Response<ParticipantThresholdDto>> Get([FromRoute] GetParticipantThresholdQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPost]
    public async Task<Response<int>> Post([FromBody] CreateParticipantThresholdDto data, CancellationToken ct)
        => await Mediator.Send(new AddParticipantThresholdCommand(data), ct);

    [HttpPut]
    public async Task<Response<int>> Put([FromBody] UpdateParticipantThresholdDto data, CancellationToken ct)
        => await Mediator.Send(new UpdateParticipantThresholdCommand(data), ct);

    [HttpDelete("{Id}")]
    public async Task<Response<int>> Delete([FromRoute] DeleteParticipantThresholdCommand command, CancellationToken ct)
        => await Mediator.Send(command, ct);
}
