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
using System.Text;

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

            var extension = Path.GetExtension(file.FileName).ToLower();
            List<Transaction> transactions;

            switch (extension)
            {
                case ".xml":
                    transactions = await ParseXmlFileAsync(file);
                    break;
                case ".csv":
                    transactions = await ParseCsvFileAsync(file);
                    break;
                default:
                    FileInfo = "Only XML or CSV file handling is allowed.";
                    return Page();
            }

            if (transactions == null || transactions.Count == 0)
            {
                FileInfo = "Failed to upload Transactions.";
                return Page();
            }

            await SendTransactionsToApiAsync(transactions);
            FileInfo = "Transactions uploaded successfully.";
            return Page();
        }

        private async Task<List<Transaction>> ParseXmlFileAsync(IFormFile file)
        {
            var transactions = new List<Transaction>();
            var rejectedTransactions = new List<Transaction>();

            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                var xmlDoc = XDocument.Load(stream);
                transactions = xmlDoc.Descendants("Transaction")
                    .Select(node => new Transaction
                    {
                        TransactionId = node.Element("TransactionId")?.Value,
                        TransactionDate = DateTime.Parse(node.Element("TransactionDate")?.Value),
                        AccountNo = node.Element("AccountNo")?.Value,
                        Amount = decimal.TryParse(node.Element("Amount")?.Value, out var amount) ? amount : 0,
                        CurrencyCode = node.Element("CurrencyCode")?.Value,
                        Status = node.Element("Status")?.Value
                    }).ToList();
            }

            rejectedTransactions = transactions.Where(t => string.IsNullOrEmpty(t.TransactionId) ||
                                                           string.IsNullOrEmpty(t.AccountNo) ||
                                                           t.Amount == 0 ||
                                                           string.IsNullOrEmpty(t.CurrencyCode)).ToList();

            if (rejectedTransactions.Any())
            {
                _logger.Info("File is rejected: " + file.FileName + ". Rejected transactions below");
                foreach (var transaction in rejectedTransactions)
                {
                    _logger.Info($"Rejected Transaction: {transaction.TransactionId}, {transaction.TransactionDate}, {transaction.AccountNo}, {transaction.Amount}, {transaction.CurrencyCode}, {transaction.Status}");
                }
                _logger.Info("End of rejected transactions.");
                return null;
            }

            return transactions;
        }

        private async Task<List<Transaction>> ParseCsvFileAsync(IFormFile file)
        {
            var transactions = new List<Transaction>();
            var rejectedTransactions = new List<Transaction>();

            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                string headerLine = await stream.ReadLineAsync();
                while (!stream.EndOfStream)
                {
                    var line = await stream.ReadLineAsync();
                    var values = ParseCsvLine(line);

                    if (values.Length != 6)
                    {
                        _logger.Info($"Rejected Line: {line} - Incorrect number of columns.");
                        continue;
                    }

                    var transaction = new Transaction
                    {
                        TransactionId = values[0],
                        AccountNo = values[1],
                        Amount = decimal.TryParse(values[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount) ? amount : 0,
                        CurrencyCode = values[3],
                        TransactionDate = DateTime.TryParseExact(values[4], "dd/MM/yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out var date) ? date : default,
                        Status = values[5]
                    };

                    transactions.Add(transaction);
                }
            }

            rejectedTransactions = transactions.Where(t => string.IsNullOrEmpty(t.TransactionId) || t.TransactionId.Length > 50 ||
                                                           string.IsNullOrEmpty(t.AccountNo) || t.AccountNo.Length > 30 ||
                                                           t.Amount == 0 ||
                                                           string.IsNullOrEmpty(t.CurrencyCode) || t.CurrencyCode.Length != 3 ||
                                                           t.TransactionDate == default ||
                                                           string.IsNullOrEmpty(t.Status) || !new[] { "Approved", "Failed", "Finished" }.Contains(t.Status)).ToList();

            if (rejectedTransactions.Any())
            {
                foreach (var transaction in rejectedTransactions)
                {
                    _logger.Info($"Rejected Transaction: {transaction.TransactionId}, {transaction.TransactionDate}, {transaction.AccountNo}, {transaction.Amount}, {transaction.CurrencyCode}, {transaction.Status}");
                }
                return null;
            }

            return transactions;
        }

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];

                if (ch == '"' && !inQuotes)
                {
                    inQuotes = true;
                }
                else if (ch == '"' && inQuotes)
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            values.Add(current.ToString());
            return values.ToArray();
        }

        private async Task SendTransactionsToApiAsync(List<Transaction> transactions)
        {
            using (var client = new HttpClient())
            {
                var response = await client.PostAsJsonAsync("http://localhost:5276/api/Transaction", transactions);
                response.EnsureSuccessStatusCode();
            }
        }        
    }
}
