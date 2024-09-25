using Frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Frontend.Pages
{
    public class FileUploaderModel : PageModel
    {
        [BindProperty]
        public IFormFile File { get; set; }
        public string FileInfo { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (File == null)
            {
                FileInfo = "No file selected.";
                return Page();
            }

            if (File.Length > 1048576)
            {
                FileInfo = "File size exceeds 1MB.";
                return Page();
            }

            if (!new[] { ".csv", ".xml" }.Contains(Path.GetExtension(File.FileName).ToLower()))
            {
                FileInfo = "Invalid file type. Only CSV and XML are allowed.";
                return Page();
            }

            if (Path.GetExtension(File.FileName).ToLower() == ".xml")
            {
                // var transactions = await ParseXmlFileAsync(File);
                var transactions = GetDummyTransactions();


                await SendTransactionsToApiAsync(transactions);
                FileInfo = "Transactions uploaded successfully.";
            }
            else
            {
                FileInfo = "Only XML file handling is implemented.";
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

            return transactions;
        }

        private async Task SendTransactionsToApiAsync(List<Transaction> transactions)
        {
            using (var client = new HttpClient())
            {
                // var response = await client.PostAsJsonAsync("/api/Transaction", transactions);
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


    //public class Transaction
    //{
    //    public string TransactionId { get; set; }
    //    public DateTime TransactionDate { get; set; }
    //    public string AccountNo { get; set; }
    //    public decimal Amount { get; set; }
    //    public string CurrencyCode { get; set; }
    //    public string Status { get; set; }
    //}
}
