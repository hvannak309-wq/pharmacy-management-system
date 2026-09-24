using pharmacy.Enums;

namespace pharmacy.Models
{
    public class Purchase
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public int UserId { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public PurchaseStatus Status { get; set; }

        public Supplier? Supplier { get; set; }
        public User? User { get; set; }
        public ICollection<PurchaseDetail> Details { get; set; } = new List<PurchaseDetail>();
    }
}
