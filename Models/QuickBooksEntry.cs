namespace JobCompareApp.Models
{
    public class QuickBooksEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Memo { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string Rep { get; set; } = string.Empty;
        public string PONumber { get; set; } = string.Empty;
    }
}
