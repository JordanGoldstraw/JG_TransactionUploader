using Frontend.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Text;
using System.Xml;

namespace Frontend.Pages
{
    public class FileUploaderModel : PageModel
    {
        [BindProperty]
        public IFormFile File { get; set; }
        [BindProperty]
        public List<SelectListItem> CurrencyOptions { get; set; }
        [BindProperty]
        public string SelectedCurrency { get; set; }
        [BindProperty]
        public DateTime? StartDate { get; set; }
        [BindProperty]
        public DateTime? EndDate { get; set; }
        [BindProperty]
        public string SelectedStatus { get; set; }

        public string FileInfo { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();

        private NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public async Task OnGetAsync()
        {
            CurrencyOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "USD", Text = "USD" },
                new SelectListItem { Value = "EUR", Text = "EUR" },
                new SelectListItem { Value = "GBP", Text = "GBP" },
                new SelectListItem { Value = "SGD", Text = "SGD" },
            };
        }

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
                    FileInfo = "Unknown format";
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

        public async Task<IActionResult> OnPostFilterTransactionsAsync()
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    var queryParams = new List<string>();
                    if (!string.IsNullOrEmpty(SelectedCurrency))
                    {
                        queryParams.Add($"currency={SelectedCurrency}");
                    }
                    if (StartDate.HasValue)
                    {
                        queryParams.Add($"startDate={StartDate.Value:yyyy-MM-dd}");
                    }
                    if (EndDate.HasValue)
                    {
                        queryParams.Add($"endDate={EndDate.Value:yyyy-MM-dd}");
                    }
                    if (!string.IsNullOrEmpty(SelectedStatus))
                    {
                        queryParams.Add($"status={SelectedStatus}");
                    }

                    var queryString = string.Join("&", queryParams);
                    var requestUrl = $"http://localhost:5276/api/Transaction?{queryString}";

                    try
                    {
                        _logger.Info($"Request URL: {requestUrl}");
                        var response = await client.GetAsync(requestUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            Transactions = JsonConvert.DeserializeObject<List<Transaction>>(json);
                        }
                        else
                        {
                            _logger.Error($"Error response: {response.StatusCode} - {response.ReasonPhrase}");
                            FileInfo = $"Error response: {response.StatusCode} - {response.ReasonPhrase}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Exception: {ex.Message}");
                    }
                }
            }

            return Page();
        }        

        private async Task<List<Transaction>> ParseXmlFileAsync(IFormFile file)
        {
            var transactions = new List<Transaction>();
            var rejectedTransactions = new List<Transaction>();

            using (var stream = file.OpenReadStream())
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(stream);

                var transactionNodes = xmlDoc.SelectNodes("//Transaction");

                foreach (XmlNode transactionNode in transactionNodes)
                {
                    try
                    {
                        var transactionId = transactionNode.Attributes["id"]?.Value;
                        var transactionDateStr = transactionNode.SelectSingleNode("TransactionDate")?.InnerText;
                        var accountNo = transactionNode.SelectSingleNode("PaymentDetails/AccountNo")?.InnerText;
                        var amountStr = transactionNode.SelectSingleNode("PaymentDetails/Amount")?.InnerText;
                        var currencyCode = transactionNode.SelectSingleNode("PaymentDetails/CurrencyCode")?.InnerText;
                        var status = transactionNode.SelectSingleNode("Status")?.InnerText;

                        DateTime transactionDate = default;
                        decimal amount = default;
                        if (string.IsNullOrEmpty(transactionId) || transactionId.Length > 50 ||
                            string.IsNullOrEmpty(transactionDateStr) || !DateTime.TryParseExact(transactionDateStr, "yyyy-MM-ddTHH:mm:ss", null, System.Globalization.DateTimeStyles.None, out transactionDate) ||
                            string.IsNullOrEmpty(accountNo) || accountNo.Length > 30 ||
                            string.IsNullOrEmpty(amountStr) || !decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out amount) ||
                            string.IsNullOrEmpty(currencyCode) || currencyCode.Length != 3 ||
                            string.IsNullOrEmpty(status) || !new[] { "Approved", "Rejected", "Done" }.Contains(status))
                        {
                            rejectedTransactions.Add(new Transaction
                            {
                                TransactionId = transactionId,
                                TransactionDate = transactionDate,
                                AccountNo = accountNo,
                                Amount = amount,
                                CurrencyCode = currencyCode,
                                Status = status
                            });
                            continue;
                        }

                        var transaction = new Transaction
                        {
                            TransactionId = transactionId,
                            TransactionDate = transactionDate,
                            AccountNo = accountNo,
                            Amount = amount,
                            CurrencyCode = currencyCode,
                            Status = status
                        };

                        transactions.Add(transaction);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Error occurred processing XML file " + file.FileName + ": " + ex.Message);
                        continue;
                    }
                }
            }

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
