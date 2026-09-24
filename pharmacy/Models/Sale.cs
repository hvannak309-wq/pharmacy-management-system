using pharmacy.Enums;

namespace pharmacy.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public int UserId { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }

        public Customer? Customer { get; set; }
        public User? User { get; set; }
        public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
    }
}
