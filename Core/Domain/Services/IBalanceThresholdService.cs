using SPSS.Domain.Models.BL;
using SPSS.Domain.Models.Enums;

namespace SPSS.Domain.Services;

public interface IBalanceThresholdService
{
    ThresholdZone EvaluateZone(decimal currentBalance, ParticipantThreshold threshold);
    bool ShouldSendAlert(ThresholdZone currentZone, ThresholdZone previousZone);
    ThresholdZone GetAlertZone(ThresholdZone zone);
}
