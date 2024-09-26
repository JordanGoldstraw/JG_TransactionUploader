using Frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Frontend.Mappers;
using System.Net.Http;

namespace Frontend.Pages
{
    public class FileUploaderModel : PageModel
    {
        [BindProperty]
        public IFormFile File { get; set; }
        public string FileInfo { get; set; }

        private NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public async Task<IActionResult> OnPostAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                FileInfo = "No file selected.";
                return Page();
            }

            if (File.Length > 1048576)
            {
                FileInfo = "File size exceeds 1MB.";
                _logger.Info("File size exceeds the maximum limit of 1 MB. File: " + file.FileName);
                return Page();
            }

            List<Transaction> transactions;

            if (!new[] { ".csv", ".xml" }.Contains(Path.GetExtension(File.FileName).ToLower()))
            {
                FileInfo = "Invalid file type. Only CSV and XML are allowed.";
                _logger.Info("Invalid file type. File: " + file.FileName);
                return Page();
            }

            if (Path.GetExtension(File.FileName).ToLower() == ".xml")
            {
                transactions = await ParseXmlFileAsync(File);
                await SendTransactionsToApiAsync(transactions);
                FileInfo = "Transactions uploaded successfully.";
            }
            else if (Path.GetExtension(file.FileName).ToLower() == ".csv")
            {
                transactions = await ParseCsvFileAsync(file);
                await SendTransactionsToApiAsync(transactions);
                FileInfo = "Transactions uploaded successfully.";
            }
            else
            {
                FileInfo = "Only XML or CSV file handling is implemented.";
                return Page();
            }

            return Page();
        }

        private async Task<List<Transaction>> ParseXmlFileAsync(IFormFile file)
        {
            var transactions = new List<Transaction>();

            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                var xmlDoc = XDocument.Load(stream);
                transactions = xmlDoc.Descendants("Transaction")
                    .Select(node => new Transaction
                    {
                        TransactionId = node.Element("TransactionId")?.Value,
                        TransactionDate = DateTime.Parse(node.Element("TransactionDate")?.Value),
                        AccountNo = node.Element("AccountNo")?.Value,
                        Amount = decimal.Parse(node.Element("Amount")?.Value),
                        CurrencyCode = node.Element("CurrencyCode")?.Value,
                        Status = node.Element("Status")?.Value
                    }).ToList();
            }

            if (transactions.Any(t => string.IsNullOrEmpty(t.TransactionId) ||
                                      string.IsNullOrEmpty(t.AccountNo) ||
                                      t.Amount == 0 ||
                                      string.IsNullOrEmpty(t.CurrencyCode) ||
                                      t.TransactionDate == default ||
                                      string.IsNullOrEmpty(t.Status)))
            {
                FileInfo = "Invalid record found in XML file. The file has been rejected.";
                return null;
            }

            return transactions;
        }

        private async Task<List<Transaction>> ParseCsvFileAsync(IFormFile file)
        {
            var transactions = new List<Transaction>();

            using (var stream = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<TransactionMapper>();
                var records = csv.GetRecords<Transaction>();
                transactions = records.ToList();
            }

            if (transactions.Any(t => string.IsNullOrEmpty(t.TransactionId) ||
                                      string.IsNullOrEmpty(t.AccountNo) ||
                                      t.Amount == 0 ||
                                      string.IsNullOrEmpty(t.CurrencyCode) ||
                                      t.TransactionDate == default ||
                                      string.IsNullOrEmpty(t.Status)))
            {
                FileInfo = "Invalid record found in CSV file. The file has been rejected.";
                return null;
            }

            return transactions;
        }

        private async Task SendTransactionsToApiAsync(List<Transaction> transactions)
        {
            using (var client = new HttpClient())
            {
                var response = await client.PostAsJsonAsync("http://localhost:5276/api/Transaction", transactions);
                response.EnsureSuccessStatusCode();
            }
        }

        private List<Transaction> GetDummyTransactions()
        {
            return new List<Transaction>
            {
                new Transaction
                {
                    TransactionId = "Inv00001",
                    TransactionDate = DateTime.Parse("2019-01-23T13:45:10"),
                    AccountNo = "1234123412341234",
                    Amount = 200.00m,
                    CurrencyCode = "USD",
                    Status = "Done"
                },
                new Transaction
                {
                    TransactionId = "Inv00002",
                    TransactionDate = DateTime.Parse("2019-01-24T16:09:15"),
                    AccountNo = "x12347890IS",
                    Amount = 10000.00m,
                    CurrencyCode = "EUR",
                    Status = "Rejected"
                }
            };
        }
    }
}
