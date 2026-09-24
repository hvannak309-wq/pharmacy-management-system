using pharmacy.Enums;
using pharmacy.Models;

namespace pharmacy.DTOs
{
    public class CheckoutLineDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CheckoutDto
    {
        public int UserId { get; set; }
        public int? CustomerId { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public List<CheckoutLineDto> Lines { get; set; } = new();
    }

    public class PurchaseLineDto
    {
        public int ProductId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class PurchaseCreateDto
    {
        public int SupplierId { get; set; }
        public int UserId { get; set; }
        public List<PurchaseLineDto> Lines { get; set; } = new();
    }

    public class ProfitRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit => Revenue - Cost;
    }

    public class DashboardDto
    {
        public decimal TodaySales { get; set; }
        public int TodaySaleCount { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public int ExpiredCount { get; set; }
    }
}
