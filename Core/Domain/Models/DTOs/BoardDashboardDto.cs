namespace SPSS.Domain.Models.DTOs;

public sealed class BoardDashboardCardsResponseDto
{
    public List<BoardDashboardCardDto> Data { get; set; } = [];
}

public sealed class BoardDashboardCardDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class BoardTotalSystemValueResponseDto
{
    public List<BoardMonthlyValueDto> Data { get; set; } = [];
}

public sealed class BoardMonthlyValueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public sealed class BoardTransactionTypeMixResponseDto
{
    public Dictionary<string, long> Data { get; set; } = new()
    {
        ["p2p"] = 0,
        ["p2m"] = 0,
        ["g2p"] = 0,
        ["b2b"] = 0
    };
}

public sealed class BoardFinancialInclusionResponseDto
{
    public BoardFinancialInclusionNationalDataDto NationalData { get; set; } = new();
}

public sealed class BoardFinancialInclusionNationalDataDto
{
    public string Name { get; set; } = "NATIONAL TOTAL";
    public long AdultPopulation { get; set; }
    public long ActiveUsers { get; set; }
    public decimal Coverage { get; set; }
    public decimal TargetGap { get; set; }
}

public sealed class BoardNewAccountsOnboardingResponseDto
{
    public Dictionary<string, long> MonthlyData { get; set; } = [];
}

public sealed class BoardRulebookComplianceScoreDto
{
    public decimal Score { get; set; }
    public decimal PreviousScore { get; set; }
    public int TotalReviews { get; set; }
    public int PassedReviews { get; set; }
    public int FailedReviews { get; set; }
    public string? LastReviewDate { get; set; }
}
