namespace Frontend.Models
{
    public class Transaction
    {
        public string TransactionId { get; set; }
        public string AccountNo { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
    }
}
