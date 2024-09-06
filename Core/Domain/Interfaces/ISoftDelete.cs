namespace SPSS.Domain.Interfaces;

public interface ISoftDelete
{
    DateTimeOffset? DeletedOn { get; set; }
    string? DeletedBy { get; set; }
}