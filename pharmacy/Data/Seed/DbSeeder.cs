using Microsoft.EntityFrameworkCore;
using pharmacy.Enums;
using pharmacy.Helpers;
using pharmacy.Models;

namespace pharmacy.Data.Seed
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            if (await db.Users.AnyAsync()) return;

            var rng = new Random(42);

            db.Users.AddRange(
                new Models.User { Username = "admin", PasswordHash = PasswordHasher.Hash("admin123"), FullName = "Administrator", Role = UserRole.Admin },
                new Models.User { Username = "pharmacist", PasswordHash = PasswordHasher.Hash("pharma123"), FullName = "Pharmacist One", Role = UserRole.Pharmacist },
                new Models.User { Username = "cashier", PasswordHash = PasswordHasher.Hash("cash123"), FullName = "Cashier One", Role = UserRole.Cashier },
                new Models.User { Username = "cashier2", PasswordHash = PasswordHasher.Hash("cash123"), FullName = "Cashier Two", Role = UserRole.Cashier },
                new Models.User { Username = "pharma2", PasswordHash = PasswordHasher.Hash("pharma123"), FullName = "Pharmacist Two", Role = UserRole.Pharmacist }
            );

            var categories = new[]
            {
                new Category { Name = "General", Description = "General medicines" },
                new Category { Name = "Antibiotics", Description = "Antibacterial drugs" },
                new Category { Name = "Pain Relief", Description = "Analgesics and antipyretics" },
                new Category { Name = "Cardiovascular", Description = "Heart and blood pressure" },
                new Category { Name = "Diabetes", Description = "Diabetes care" },
                new Category { Name = "Gastrointestinal", Description = "Stomach and digestive" },
                new Category { Name = "Vitamins", Description = "Vitamins and supplements" },
                new Category { Name = "Respiratory", Description = "Cough, cold, asthma" },
                new Category { Name = "Dermatology", Description = "Skin care" },
                new Category { Name = "First Aid", Description = "Wound care and first aid" }
            };
            db.Categories.AddRange(categories);

            var suppliers = new[]
            {
                new Supplier { Name = "MedSupply Co", Phone = "555-0101", Email = "sales@medsupply.test", Address = "12 Industrial Way" },
                new Supplier { Name = "PharmaDirect", Phone = "555-0102", Email = "orders@pharmadirect.test", Address = "88 Harbor Rd" },
                new Supplier { Name = "HealthLine Dist", Phone = "555-0103", Email = "info@healthline.test", Address = "4 Commerce Ave" },
                new Supplier { Name = "CareBridge Pharma", Phone = "555-0104", Email = "care@carebridge.test", Address = "210 Lake St" },
                new Supplier { Name = "VitaSource", Phone = "555-0105", Email = "vita@vitasource.test", Address = "9 Park Blvd" },
                new Supplier { Name = "GlobalMeds", Phone = "555-0106", Email = "global@globalmeds.test", Address = "500 Export Dr" },
                new Supplier { Name = "City Drug Wholesale", Phone = "555-0107", Email = "city@wholesale.test", Address = "3 Market Sq" },
                new Supplier { Name = "Apex Generics", Phone = "555-0108", Email = "apex@apex.test", Address = "77 Summit Ln" }
            };
            db.Suppliers.AddRange(suppliers);

            var firstNames = new[] { "Anna", "Ben", "Clara", "David", "Eva", "Frank", "Grace", "Hugo", "Iris", "Jack", "Karen", "Leo", "Mona", "Noah", "Olga", "Paul", "Quinn", "Rita", "Sam", "Tina", "Uma", "Victor", "Wendy", "Xander", "Yara", "Zane" };
            var lastNames = new[] { "Adams", "Brown", "Clarke", "Dixon", "Evans", "Fisher", "Green", "Harris", "Ibrahim", "Jones", "Khan", "Lewis", "Miller", "Nolan", "Owen", "Patel", "Reed", "Smith", "Turner", "Usman", "Vargas", "Wright" };

            var customers = new List<Customer>();
            for (var i = 0; i < 60; i++)
            {
                customers.Add(new Customer
                {
                    Name = $"{firstNames[rng.Next(firstNames.Length)]} {lastNames[rng.Next(lastNames.Length)]}",
                    Phone = $"555-{1000 + i:D4}",
                    Email = $"customer{i + 1}@mail.test",
                    Address = $"{rng.Next(1, 999)} {lastNames[rng.Next(lastNames.Length)]} St"
                });
            }
            db.Customers.AddRange(customers);

            var productSeeds = new (string Name, int Cat, decimal Unit, decimal Cost, bool Rx)[]
            {
                ("Paracetamol 500mg", 0, 2.50m, 1.20m, false),
                ("Ibuprofen 400mg", 0, 3.75m, 1.80m, false),
                ("Aspirin 100mg", 0, 2.10m, 0.95m, false),
                ("Diclofenac 50mg", 0, 4.20m, 2.10m, false),
                ("Naproxen 250mg", 0, 5.00m, 2.40m, false),
                ("Amoxicillin 500mg", 1, 8.00m, 4.50m, true),
                ("Azithromycin 250mg", 1, 12.50m, 7.00m, true),
                ("Ciprofloxacin 500mg", 1, 10.00m, 5.50m, true),
                ("Clarithromycin 250mg", 1, 14.00m, 8.20m, true),
                ("Metronidazole 400mg", 1, 6.50m, 3.10m, true),
                ("Doxycycline 100mg", 1, 9.20m, 4.80m, true),
                ("Cephalexin 500mg", 1, 11.00m, 6.00m, true),
                ("Tramadol 50mg", 2, 7.50m, 3.90m, true),
                ("Celecoxib 200mg", 2, 15.00m, 9.00m, true),
                ("Mefenamic Acid 500mg", 2, 4.80m, 2.30m, false),
                ("Codeine Paracetamol", 2, 6.20m, 3.00m, true),
                ("Amlodipine 5mg", 3, 5.50m, 2.60m, true),
                ("Losartan 50mg", 3, 7.80m, 3.90m, true),
                ("Atenolol 50mg", 3, 4.90m, 2.20m, true),
                ("Metformin 500mg", 4, 3.60m, 1.50m, true),
                ("Glimepiride 2mg", 3, 6.10m, 3.00m, true),
                ("Gliclazide 80mg", 4, 5.40m, 2.70m, true),
                ("Insulin Glargine", 4, 45.00m, 32.00m, true),
                ("Sitagliptin 100mg", 4, 18.00m, 11.00m, true),
                ("Omeprazole 20mg", 5, 4.40m, 2.00m, false),
                ("Pantoprazole 40mg", 5, 6.80m, 3.40m, false),
                ("Ranitidine 150mg", 5, 3.20m, 1.40m, false),
                ("Ondansetron 4mg", 5, 5.90m, 2.80m, true),
                ("Loperamide 2mg", 5, 2.90m, 1.20m, false),
                ("Domperidone 10mg", 5, 4.10m, 1.90m, false),
                ("Vitamin C 1000mg", 6, 3.50m, 1.60m, false),
                ("Vitamin D3 2000IU", 6, 5.20m, 2.50m, false),
                ("Multivitamin Daily", 6, 7.00m, 3.40m, false),
                ("Calcium + D3", 6, 6.40m, 3.10m, false),
                ("Omega-3 1000mg", 6, 9.50m, 5.00m, false),
                ("Iron + Folic Acid", 6, 4.00m, 1.80m, false),
                ("Zinc 50mg", 6, 3.80m, 1.70m, false),
                ("Cough Syrup DM", 7, 5.60m, 2.70m, false),
                ("Salbutamol Inhaler", 7, 12.00m, 7.50m, true),
                ("Loratadine 10mg", 7, 3.90m, 1.80m, false),
                ("Cetirizine 10mg", 7, 3.40m, 1.50m, false),
                ("Pseudoephedrine 60mg", 7, 4.60m, 2.20m, false),
                ("Beclomethasone Spray", 7, 11.50m, 6.80m, true),
                ("Hydrocortisone Cream 1%", 8, 4.70m, 2.20m, false),
                ("Clotrimazole Cream", 8, 5.30m, 2.60m, false),
                ("Miconazole Cream", 8, 5.10m, 2.50m, false),
                ("Calamine Lotion", 8, 3.30m, 1.40m, false),
                ("Antiseptic Solution", 9, 4.20m, 1.90m, false),
                ("Adhesive Bandages", 9, 2.80m, 1.10m, false),
                ("Sterile Gauze Pads", 9, 3.10m, 1.30m, false),
                ("Elastic Bandage 5cm", 9, 4.50m, 2.00m, false),
                ("Digital Thermometer", 9, 9.00m, 5.50m, false),
                ("Blood Pressure Monitor", 9, 35.00m, 24.00m, false),
                ("Glucose Test Strips", 4, 22.00m, 15.00m, false),
                ("Lancets 100ct", 4, 8.50m, 5.00m, false),
                ("Oral Rehydration Salts", 0, 1.80m, 0.70m, false),
                ("Activated Charcoal", 0, 4.90m, 2.30m, false),
                ("Saline Nasal Spray", 7, 3.60m, 1.60m, false),
                ("Ear Drops", 8, 4.80m, 2.30m, false),
                ("Eye Drops Lubricant", 8, 5.70m, 2.90m, false),
                ("Throat Lozenges", 7, 2.60m, 1.10m, false),
                ("Heating Pad Small", 9, 12.50m, 8.00m, false),
                ("Syringe 5ml", 9, 0.60m, 0.25m, false),
                ("Alcohol Wipes 100ct", 9, 3.90m, 1.70m, false),
                ("Hand Sanitizer 250ml", 9, 4.30m, 2.00m, false),
                ("Sunscreen SPF50", 8, 8.90m, 4.80m, false),
                ("Mosquito Repellent", 8, 6.70m, 3.50m, false),
                ("Motion Sickness Patches", 5, 5.50m, 2.70m, false),
                ("Smoking Cessation Patches", 6, 16.00m, 10.00m, true),
                ("Probiotics 10B", 6, 11.20m, 6.50m, false),
                ("Melatonin 3mg", 6, 5.80m, 2.90m, false),
                ("CoQ10 100mg", 6, 14.50m, 9.00m, false)
            };

            var products = new List<Product>();
            for (var i = 0; i < productSeeds.Length; i++)
            {
                var s = productSeeds[i];
                products.Add(new Product
                {
                    Category = categories[s.Cat],
                    Name = s.Name,
                    Barcode = $"89012345{i + 1:D4}",
                    UnitPrice = s.Unit,
                    CostPrice = s.Cost,
                    ReorderLevel = rng.Next(10, 60),
                    IsPrescriptionRequired = s.Rx
                });
            }
            db.Products.AddRange(products);
            await db.SaveChangesAsync();

            var expiries = new[]
            {
                DateTime.Today.AddDays(-5),
                DateTime.Today.AddDays(rng.Next(10, 30)),
                DateTime.Today.AddDays(rng.Next(31, 60)),
                DateTime.Today.AddDays(rng.Next(61, 90)),
                DateTime.Today.AddDays(rng.Next(120, 400)),
                DateTime.Today.AddDays(rng.Next(200, 700))
            };

            foreach (var p in products)
            {
                for (var i = 0; i < expiries.Length; i++)
                {
                    db.Batches.Add(new Batch
                    {
                        ProductId = p.Id,
                        BatchNumber = $"B{p.Id:D3}-{i + 1}",
                        ExpiryDate = expiries[i],
                        Quantity = rng.Next(15, 200),
                        PurchasePrice = p.CostPrice
                    });
                }
            }
            await db.SaveChangesAsync();

            var staff = await db.Users.Where(u => u.Role != UserRole.Admin).ToListAsync();
            var allProducts = await db.Products.ToListAsync();
            var allBatches = await db.Batches.ToListAsync();
            var allSuppliers = await db.Suppliers.ToListAsync();
            var allCustomers = await db.Customers.ToListAsync();
            var payments = Enum.GetValues<PaymentMethod>();

            for (var i = 0; i < 80; i++)
            {
                var purchase = new Purchase
                {
                    Supplier = allSuppliers[rng.Next(allSuppliers.Count)],
                    User = staff[rng.Next(staff.Count)],
                    PurchaseDate = DateTime.Today.AddDays(-rng.Next(1, 180)),
                    Status = rng.Next(10) == 0 ? PurchaseStatus.Pending : PurchaseStatus.Received
                };

                var lineCount = rng.Next(2, 8);
                for (var j = 0; j < lineCount; j++)
                {
                    var p = allProducts[rng.Next(allProducts.Count)];
                    var qty = rng.Next(10, 120);
                    var batch = allBatches.FirstOrDefault(b => b.ProductId == p.Id) ?? allBatches[rng.Next(allBatches.Count)];
                    purchase.Details.Add(new PurchaseDetail
                    {
                        ProductId = p.Id,
                        BatchId = batch.Id,
                        Quantity = qty,
                        UnitCost = p.CostPrice,
                        Subtotal = qty * p.CostPrice
                    });
                }
                purchase.TotalAmount = purchase.Details.Sum(d => d.Subtotal);
                db.Purchases.Add(purchase);
            }

            for (var i = 0; i < 250; i++)
            {
                var sale = new Sale
                {
                    Customer = rng.Next(4) == 0 ? null : allCustomers[rng.Next(allCustomers.Count)],
                    User = staff[rng.Next(staff.Count)],
                    SaleDate = DateTime.Today.AddDays(-rng.Next(0, 90)).AddHours(rng.Next(8, 21)).AddMinutes(rng.Next(0, 60)),
                    PaymentMethod = payments[rng.Next(payments.Length)],
                    Discount = 0m
                };

                var lineCount = rng.Next(1, 6);
                for (var j = 0; j < lineCount; j++)
                {
                    var p = allProducts[rng.Next(allProducts.Count)];
                    var qty = rng.Next(1, 5);
                    sale.Details.Add(new SaleDetail
                    {
                        ProductId = p.Id,
                        BatchId = allBatches.First(b => b.ProductId == p.Id).Id,
                        Quantity = qty,
                        UnitPrice = p.UnitPrice,
                        Subtotal = qty * p.UnitPrice
                    });
                }
                sale.TotalAmount = sale.Details.Sum(d => d.Subtotal);
                if (rng.Next(5) == 0) sale.Discount = Math.Round(sale.TotalAmount * 0.05m, 2);
                sale.PaidAmount = sale.TotalAmount - sale.Discount;
                db.Sales.Add(sale);
            }

            await db.SaveChangesAsync();
        }
    }
}
