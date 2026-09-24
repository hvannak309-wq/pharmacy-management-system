using pharmacy.Enums;

namespace pharmacy.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}
