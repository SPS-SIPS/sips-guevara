namespace SPSS.App.Features.Reports.Queries;

using static SPSS.Domain.Common.KnownClaims;

[Authorize(Roles = QueryReports)]
public sealed record FinancialReportsQuery(string FromDate, string ToDate) : IRequest<Response<CombinedReportDto>>;
public sealed class FinancialReportsQueryValidator : AbstractValidator<FinancialReportsQuery>
{
    public FinancialReportsQueryValidator()
    {
        RuleFor(e => e.FromDate)
            .NotEmpty()
            .WithMessage("From date is required.")
            .Must(BeAValidDate)
            .WithMessage("From date is not valid.")
            .LessThanOrEqualTo(e => e.ToDate)
            .WithMessage("From date must be less than or equal to To date.");
        RuleFor(e => e.ToDate)
            .NotEmpty()
            .WithMessage("To date is required.")
            .Must(BeAValidDate)
            .WithMessage("To date is not valid.")
            .GreaterThanOrEqualTo(e => e.FromDate)
            .WithMessage("To date must be greater than or equal to From date.");
    }

    private bool BeAValidDate(string dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return false;

        if (DateTimeOffset.TryParse(dateString, out _)) return true;

        string[] formats = ["MM-dd-yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy/MM/dd"];
        return DateTimeOffset.TryParseExact(dateString, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _);
    }
}
public sealed class FinancialReportsQueryHandler(IStorageBroker storage, IMapper mapper, IStatisticsReportService reportService) : IRequestHandler<FinancialReportsQuery, Response<CombinedReportDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly IStatisticsReportService _reportService = reportService;
    public async ValueTask<Response<CombinedReportDto>> Handle(FinancialReportsQuery request, CancellationToken cancellationToken)
    {
        string[] formats = ["MM-dd-yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy", "yyyy/MM/dd"];
        var fromDate = DateTimeOffset.ParseExact(request.FromDate, formats, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();
        var toDate = DateTimeOffset.ParseExact(request.ToDate, formats, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();

        var settlementDetails = await _storage.BicStatistics
        .Where(x => x.Bic != "ZYABSOSM" && x.Date >= fromDate && x.Date <= toDate)
            .Include(x => x.TopReceivers)
            .Include(x => x.TopSenders)
            .ProjectTo<BicStatisticsDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        if (settlementDetails == null || settlementDetails.Count == 0)
            throw new NotFoundException("No settlement statistics found for the specified date range.");

        // Generate reports based on the fetched statistics
        var systemOverview = await _reportService.GetSystemOverviewAsync(settlementDetails);
        var participantSummaries = await _reportService.GetParticipantSummariesAsync(settlementDetails);
        var netPositionTable = await _reportService.GetNetPositionTableAsync(settlementDetails);
        var trendPack = await _reportService.GetTrendPackAsync(settlementDetails, fromDate.Date, toDate.Date);
        var directionalFlow = await _reportService.GetDirectionalFlowAsync(settlementDetails);
        var feeDistribution = await _reportService.GetFeeDistributionAsync(settlementDetails);
        var counterpartyLists = await _reportService.GetCounterpartyListsAsync(settlementDetails);
        var concentrationAnalysis = await _reportService.GetConcentrationAnalysisAsync(settlementDetails);
        var leagueTable = await _reportService.GetLeagueTableAsync(settlementDetails);
        var matrix = await _reportService.GetMatrixAsync(settlementDetails);
        var topSenders = counterpartyLists.TopSenders;
        var topReceivers = counterpartyLists.TopReceivers;
        var feeDistributionItems = feeDistribution.Items;
        var leagueTableItems = leagueTable.Items;
        var matrixRows = matrix.Rows;


        // Combine all reports into a single response
        var combinedResponse = new CombinedReportDto
        {
            SystemOverview = systemOverview,
            ParticipantSummaries = participantSummaries,
            NetPositionTable = netPositionTable,
            TrendPack = trendPack,
            DirectionalFlow = directionalFlow,
            CounterpartyList = counterpartyLists,
            FeeDistribution = feeDistribution,
            ConcentrationAnalysis = concentrationAnalysis,
            LeagueTable = leagueTable,
            Matrix = matrix,
            TopSenders = topSenders,
            TopReceivers = topReceivers,
            FeeDistributionItems = feeDistributionItems,
            LeagueTableItems = leagueTableItems,
        };

        return new Response<CombinedReportDto>(combinedResponse);
    }
}