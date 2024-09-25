using CsvHelper.Configuration;
using Frontend.Models;

namespace Frontend.Mappers
{
    public class TransactionMapper : ClassMap<Transaction>
    {
        public TransactionMapper()
        {
            Map(m => m.TransactionId);
            Map(m => m.AccountNo);
            Map(m => m.Amount);
            Map(m => m.CurrencyCode);
            Map(m => m.TransactionDate).TypeConverterOption.Format("dd/MM/yyyy HH:mm:ss");
            Map(m => m.Status);
        }
    }
}
