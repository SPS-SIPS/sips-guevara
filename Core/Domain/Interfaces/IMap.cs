namespace SPSS.Domain.Interfaces;
public interface IMap<T>
{
    void Mapping(Profile profile) => profile.CreateMap(typeof(T), GetType());
}