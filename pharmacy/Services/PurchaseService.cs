using pharmacy.DTOs;
using pharmacy.Enums;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Services
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IPurchaseRepository _purchases;
        private readonly IProductRepository _products;
        private readonly IBatchRepository _batches;
        private readonly IUnitOfWork _uow;

        public PurchaseService(IPurchaseRepository purchases, IProductRepository products, IBatchRepository batches, IUnitOfWork uow)
        {
            _purchases = purchases;
            _products = products;
            _batches = batches;
            _uow = uow;
        }

        public async Task<Result<List<Purchase>>> GetRecentAsync(int count = 100)
            => Result<List<Purchase>>.Success(await _purchases.GetRecentAsync(count));

        public async Task<Result<Purchase?>> GetByIdAsync(int id)
            => Result<Purchase?>.Success(await _purchases.GetWithDetailsAsync(id));

        public async Task<Result<int>> CreateAsync(PurchaseCreateDto dto)
        {
            if (dto.SupplierId <= 0) return Result<int>.Failure("Supplier required.");
            if (dto.Lines.Count == 0) return Result<int>.Failure("No purchase lines.");

            var details = new List<PurchaseDetail>();
            foreach (var line in dto.Lines)
            {
                if (line.Quantity <= 0) return Result<int>.Failure("Quantity must be positive.");
                if (line.ExpiryDate.Date < DateTime.Today) return Result<int>.Failure($"Batch {line.BatchNumber} already expired.");
                var product = await _products.GetByIdAsync(line.ProductId);
                if (product is null) return Result<int>.Failure($"Product {line.ProductId} not found.");

                var batch = new Batch
                {
                    ProductId = line.ProductId,
                    BatchNumber = line.BatchNumber,
                    ExpiryDate = line.ExpiryDate.Date,
                    Quantity = line.Quantity,
                    PurchasePrice = line.UnitCost
                };
                await _batches.AddAsync(batch);

                details.Add(new PurchaseDetail
                {
                    Product = product,
                    Batch = batch,
                    Quantity = line.Quantity,
                    UnitCost = line.UnitCost,
                    Subtotal = line.UnitCost * line.Quantity
                });
            }

            var purchase = new Purchase
            {
                SupplierId = dto.SupplierId,
                UserId = dto.UserId,
                PurchaseDate = DateTime.Now,
                TotalAmount = details.Sum(d => d.Subtotal),
                Status = PurchaseStatus.Received,
                Details = details
            };

            await _purchases.AddAsync(purchase);
            await _uow.SaveChangesAsync();
            return Result<int>.Success(purchase.Id);
        }
    }
}
