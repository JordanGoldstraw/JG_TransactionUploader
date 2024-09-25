
using API.Context;
using API.Interfaces;
using API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace API.Services
{
    public class TransactionProcessor : ITransactionProcessor
    {
        private readonly AppDbContext _context;

        public TransactionProcessor(AppDbContext context)
        {
            _context = context;
        }

        public async Task ProcessTransactionsAsync(List<Transaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                await SaveTransactionAsync(transaction);
            }
        }

        private async Task SaveTransactionAsync(Transaction transaction)
        {
            var connection = _context.Database.GetDbConnection();
            await using (connection)
            {
                await connection.OpenAsync();
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = "Transaction_Create";
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@TransactionId", transaction.TransactionId));
                    command.Parameters.Add(new SqlParameter("@TransactionDate", transaction.TransactionDate));
                    command.Parameters.Add(new SqlParameter("@AccountNo", transaction.AccountNo));
                    command.Parameters.Add(new SqlParameter("@Amount", transaction.Amount));
                    command.Parameters.Add(new SqlParameter("@CurrencyCode", transaction.CurrencyCode));
                    command.Parameters.Add(new SqlParameter("@Status", transaction.Status));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
