namespace SPSS.Domain.Interfaces;
public interface ITransactionsService<T>
{
    Task<int> Post(T command, IStorageBroker broker, CancellationToken token);
}
