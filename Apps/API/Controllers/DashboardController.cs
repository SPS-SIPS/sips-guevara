using SPSS.App.Features.Dashboard.Queries;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/dashboard")]
public class DashboardController : BaseController
{
    [HttpGet("cards")]
    public async Task<BoardDashboardCardsResponseDto> GetCards(
        [FromQuery] GetBoardDashboardCardsQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("total-system-value")]
    public async Task<BoardTotalSystemValueResponseDto> GetTotalSystemValue(
        [FromQuery] GetBoardTotalSystemValueQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("transaction-type-mix")]
    public async Task<BoardTransactionTypeMixResponseDto> GetTransactionTypeMix(
        [FromQuery] GetBoardTransactionTypeMixQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("financial-inclusion")]
    public async Task<BoardFinancialInclusionResponseDto> GetFinancialInclusion(CancellationToken ct)
        => await Mediator.Send(new GetBoardFinancialInclusionQuery(), ct);

    [HttpGet("new-accounts-onboarding")]
    public async Task<BoardNewAccountsOnboardingResponseDto> GetNewAccountsOnboarding(CancellationToken ct)
        => await Mediator.Send(new GetBoardNewAccountsOnboardingQuery(), ct);

    [HttpGet("rulebook-compliance-score")]
    public async Task<BoardRulebookComplianceScoreDto> GetRulebookComplianceScore(CancellationToken ct)
        => await Mediator.Send(new GetBoardRulebookComplianceScoreQuery(), ct);
}
