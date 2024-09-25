﻿using API.Context;
using API.Interfaces;
using API.Models;
using API.Services;
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

            //await _context.Transactions.AddRangeAsync(transactions);
            //await _context.SaveChangesAsync();

            await _transactionProcessor.ProcessTransactionsAsync(transactions);

            return Ok("Transactions uploaded successfully.");
        }
    }
}
