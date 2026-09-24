namespace pharmacy.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsPrescriptionRequired { get; set; }

        public Category? Category { get; set; }
        public ICollection<Batch> Batches { get; set; } = new List<Batch>();

        public int Stock => Batches.Sum(b => b.Quantity);
    }
}
