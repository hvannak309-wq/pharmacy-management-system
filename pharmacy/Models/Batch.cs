namespace pharmacy.Models
{
    public class Batch
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public bool IsDisposed { get; set; }

        public Product? Product { get; set; }
        public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();
        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();

        public bool IsExpired => ExpiryDate.Date < DateTime.Today;
    }
}
