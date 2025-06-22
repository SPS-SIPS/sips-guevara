namespace SPSS.App.Features.Reports.Queries;

using SPSS.Domain.Models.DTOs.Reports;

public sealed class CombinedReportDto
{
    public SystemOverviewReportDto? SystemOverview { get; set; }
    public List<ParticipantSummaryReportDto>? ParticipantSummaries { get; set; }
    public List<NetPositionReportDto>? NetPositionTable { get; set; }
    public TrendPackReportDto? TrendPack { get; set; }
    public DirectionalFlowReportDto? DirectionalFlow { get; set; }
    public CounterpartyListReportDto? CounterpartyList { get; set; }
    public FeeDistributionReportDto? FeeDistribution { get; set; }
    public ConcentrationAnalysisReportDto? ConcentrationAnalysis { get; set; }
    public LeagueTableReportDto? LeagueTable { get; set; }
    public MatrixReportDto? Matrix { get; set; }
    public List<CounterpartyListItemDto>? TopSenders { get; set; }
    public List<CounterpartyListItemDto>? TopReceivers { get; set; }
    public List<FeeDistributionItemDto>? FeeDistributionItems { get; set; }
    public List<LeagueTableItemDto>? LeagueTableItems { get; set; }
    public List<DirectionalFlowItemDto>? DirectionalFlowItems { get; set; }
}
