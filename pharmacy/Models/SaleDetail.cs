namespace pharmacy.Models
{
    public class SaleDetail
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public int BatchId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }

        public Sale? Sale { get; set; }
        public Product? Product { get; set; }
        public Batch? Batch { get; set; }
    }
}
