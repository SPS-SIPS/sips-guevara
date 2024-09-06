namespace SPSS.Domain.Models.DTOs;

public abstract class SoftDeleteDto
{
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
    public bool Deleted => DeletedOn.HasValue;
}

