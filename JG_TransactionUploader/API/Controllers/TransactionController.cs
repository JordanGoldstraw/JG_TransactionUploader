using API.Context;
using API.Interfaces;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
        public async Task<IActionResult> GetTransactions([FromQuery] string currency = null, [FromQuery] string startDate = null, [FromQuery] string endDate = null, [FromQuery] string status = null)
        {
            DateTime? start = null;
            DateTime? end = null;

            if (!string.IsNullOrEmpty(startDate))
            {
                if (!DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStartDate))
                {
                    return BadRequest("Invalid start date format. Use yyyy-MM-dd.");
                }
                start = parsedStartDate;
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                if (!DateTime.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEndDate))
                {
                    return BadRequest("Invalid end date format. Use yyyy-MM-dd.");
                }
                end = parsedEndDate;
            }

            var query = _context.Transactions.AsQueryable();

            if (!string.IsNullOrEmpty(currency))
            {
                query = query.Where(t => t.CurrencyCode == currency);
            }

            if (start.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= start.Value);
            }

            if (end.HasValue)
            {
                query = query.Where(t => t.TransactionDate <= end.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                switch (status)
                {
                    case "A":
                        query = query.Where(t => t.Status == "Approved");
                        break;
                    case "R":
                        query = query.Where(t => t.Status == "Failed" || t.Status == "Rejected");
                        break;
                    case "D":
                        query = query.Where(t => t.Status == "Finished" || t.Status == "Done");
                        break;
                    default:
                        return BadRequest("Invalid status value. Use A, R, or D.");
                }
            }

            var transactions = await query.ToListAsync();

            return Ok(transactions);
        }
    }
}
