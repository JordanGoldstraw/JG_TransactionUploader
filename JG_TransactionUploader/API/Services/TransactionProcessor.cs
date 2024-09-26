using API.Context;
using API.Interfaces;
using API.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace API.Services
{
    public class TransactionProcessor : ITransactionProcessor
    {
        private readonly AppDbContext _context;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public TransactionProcessor(AppDbContext context)
        {
            _context = context;
        }

        public async Task ProcessTransactionsAsync(List<Transaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                try
                {
                    await SaveTransactionAsync(transaction);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to save transaction {transaction.TransactionId}: {ex.Message}");
                }
            }
        }

        private async Task SaveTransactionAsync(Transaction transaction)
        {
            await using var connection = _context.Database.GetDbConnection();
            if (string.IsNullOrEmpty(connection.ConnectionString))
            {
                connection.ConnectionString = _context.Database.GetConnectionString();
            }
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();

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
