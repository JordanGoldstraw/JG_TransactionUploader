using API.Context;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : Controller
    {
        private readonly AppDbContext _context;

        public TransactionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> UploadTransactions([FromBody] List<Transaction> transactions)
        {
            if (transactions == null || !transactions.Any())
            {
                return BadRequest("No transactions provided.");
            }

            await _context.Transactions.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();

            return Ok("Transactions uploaded successfully.");
        }
    }
}
