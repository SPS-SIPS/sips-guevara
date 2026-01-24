using SPSS.Domain.Models.BL;
using SPSS.Domain.Models.Enums;
using SPSS.Domain.Services;

namespace SPSS.Infrastructure.Services;

public sealed class BalanceThresholdService : IBalanceThresholdService
{
    public ThresholdZone EvaluateZone(decimal currentBalance, ParticipantThreshold threshold)
    {
        if (threshold == null)
            throw new ArgumentNullException(nameof(threshold));

        if (currentBalance < 0)
            throw new ArgumentException("Balance cannot be negative", nameof(currentBalance));

        if (threshold.GoldenAmount <= 0)
            throw new ArgumentException("GoldenAmount must be positive", nameof(threshold));

        if (threshold.AmberPercentage < 0 || threshold.AmberPercentage > 100)
            throw new ArgumentException("AmberPercentage must be between 0 and 100", nameof(threshold));

        if (threshold.RedPercentage < 0 || threshold.RedPercentage > 100)
            throw new ArgumentException("RedPercentage must be between 0 and 100", nameof(threshold));

        var amberThreshold = threshold.GoldenAmount * (threshold.AmberPercentage / 100m);
        var redThreshold = threshold.GoldenAmount * (threshold.RedPercentage / 100m);

        if (redThreshold > amberThreshold)
            throw new ArgumentException("RedPercentage must be less than or equal to AmberPercentage", nameof(threshold));

        if (currentBalance < redThreshold)
            return ThresholdZone.Red;

        if (currentBalance < amberThreshold)
            return ThresholdZone.Amber;

        return ThresholdZone.Green;
    }

    public bool ShouldSendAlert(ThresholdZone currentZone, ThresholdZone previousZone)
    {
        if (currentZone == previousZone)
        {
            return false;
        }

        if (currentZone == ThresholdZone.Green)
        {
            return false;
        }

        return true;
    }

    public ThresholdZone GetAlertZone(ThresholdZone zone)
    {
        return zone;
    }
}
