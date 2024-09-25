using API.Models;

namespace API.Interfaces
{
    public interface ITransactionProcessor
    {
        Task ProcessTransactionsAsync(List<Transaction> transactions);
    }
}
