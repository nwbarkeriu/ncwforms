namespace JobCompareApp.Models
{
    public class AvionteEntry
    {
        public string Name { get; set; } = string.Empty; // Full name
        public string BillToName { get; set; } = string.Empty;
        public decimal ItemBill { get; set; }
        public decimal ItemPay { get; set; }
        public DateTime WeekWorked { get; set; }
    }
}
