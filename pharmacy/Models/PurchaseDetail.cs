namespace pharmacy.Models
{
    public class PurchaseDetail
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; }
        public int ProductId { get; set; }
        public int BatchId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Subtotal { get; set; }

        public Purchase? Purchase { get; set; }
        public Product? Product { get; set; }
        public Batch? Batch { get; set; }
    }
}
