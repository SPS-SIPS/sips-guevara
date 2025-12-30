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

    [HttpGet("{id}")]
    public async Task<Response<ParticipantThresholdDto>> Get([FromRoute] int id, CancellationToken ct)
        => await Mediator.Send(new GetParticipantThresholdQuery(id), ct);

    [HttpPost]
    public async Task<Response<int>> Post([FromBody] AddParticipantThresholdCommand command, CancellationToken ct)
        => await Mediator.Send(command, ct);

    [HttpPut]
    public async Task<Response<int>> Put([FromBody] UpdateParticipantThresholdCommand command, CancellationToken ct)
        => await Mediator.Send(command, ct);

    [HttpDelete("{id}")]
    public async Task<Response<int>> Delete([FromRoute] int id, CancellationToken ct)
        => await Mediator.Send(new DeleteParticipantThresholdCommand(id), ct);
}
