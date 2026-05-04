namespace SPSS.Domain.Models.DTOs;

public record PaginationFilter(int Index = 1, int Size = 100, string Sort = "");
