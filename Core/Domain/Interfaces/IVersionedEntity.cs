namespace SPSS.Domain.Interfaces;

public interface IVersionedEntity
{
    long Version { get; set; }
}