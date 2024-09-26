using API.Context;
using API.Interfaces;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ITransactionProcessor _transactionProcessor;

        public TransactionController(AppDbContext context, ITransactionProcessor transactionProcessor)
        {
            _context = context;
            _transactionProcessor = transactionProcessor;
        }

        [HttpPost]
        public async Task<IActionResult> UploadTransactions([FromBody] List<Transaction> transactions)
        {
            if (transactions == null || !transactions.Any())
            {
                return BadRequest("No transactions provided.");
            }            

            await _transactionProcessor.ProcessTransactionsAsync(transactions);

            return Ok("Transactions uploaded successfully.");
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionsByCurrency([FromQuery] string currency)
        {
            if (string.IsNullOrEmpty(currency))
            {
                return BadRequest("Currency is required.");
            }

            var transactions = await _context.Transactions
                .Where(t => t.CurrencyCode == currency)
                .ToListAsync();

            return Ok(transactions);
        }
    }
}
