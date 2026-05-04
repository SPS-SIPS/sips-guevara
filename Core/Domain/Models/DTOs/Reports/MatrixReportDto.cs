using System.Collections.Generic;

namespace SPSS.Domain.Models.DTOs.Reports;

public class MatrixReportDto
{
    public List<MatrixRowDto> Rows { get; set; } = new();
}

public class MatrixRowDto
{
    public string FromBic { get; set; } = string.Empty;
    public Dictionary<string, MatrixCellDto> ToBics { get; set; } = new();
}

public class MatrixCellDto
{
    public long TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}
