namespace SPSS.Domain.Interfaces;

public interface IBicStatisticsService
{
    Task<BicStatisticsDto> CalculateBicStatisticsAsync(string bic, IEnumerable<NetSettlementDetailDto> transactions);
}
