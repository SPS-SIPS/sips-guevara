namespace SPSS.Domain.Models.DTOs;

public abstract record Request
{
    public long Version { get; set; }
}