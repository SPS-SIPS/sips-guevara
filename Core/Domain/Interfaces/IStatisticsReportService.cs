using SPSS.Domain.Models.BL;
using SPSS.Domain.Models.DTOs.Reports;

namespace SPSS.Domain.Interfaces;

public interface IStatisticsReportService
{
    Task<SystemOverviewReportDto> GetSystemOverviewAsync(IEnumerable<BicStatisticsDto> stats);
    Task<List<ParticipantSummaryReportDto>> GetParticipantSummariesAsync(IEnumerable<BicStatisticsDto> stats);
    Task<List<NetPositionReportDto>> GetNetPositionTableAsync(IEnumerable<BicStatisticsDto> stats);
    Task<TrendPackReportDto> GetTrendPackAsync(IEnumerable<BicStatisticsDto> stats, DateTime from, DateTime to);
    Task<DirectionalFlowReportDto> GetDirectionalFlowAsync(IEnumerable<BicStatisticsDto> stats);
    Task<FeeDistributionReportDto> GetFeeDistributionAsync(IEnumerable<BicStatisticsDto> stats);
    Task<CounterpartyListReportDto> GetCounterpartyListsAsync(IEnumerable<BicStatisticsDto> stats);
    Task<ConcentrationAnalysisReportDto> GetConcentrationAnalysisAsync(IEnumerable<BicStatisticsDto> stats);
    Task<LeagueTableReportDto> GetLeagueTableAsync(IEnumerable<BicStatisticsDto> stats);
    Task<MatrixReportDto> GetMatrixAsync(IEnumerable<BicStatisticsDto> stats);
    Task<List<InstitutionUptimeDto>> GetInstitutionUptimeReportAsync(IEnumerable<MonitHistory> histories, DateTime? from = null, DateTime? to = null);
}
