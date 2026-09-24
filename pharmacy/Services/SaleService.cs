using pharmacy.DTOs;
using pharmacy.Interfaces;
using pharmacy.Models;

namespace pharmacy.Services
{
    public class SaleService : ISaleService
    {
        private readonly ISaleRepository _sales;
        private readonly IProductRepository _products;
        private readonly IBatchRepository _batches;
        private readonly IUnitOfWork _uow;

        public SaleService(ISaleRepository sales, IProductRepository products, IBatchRepository batches, IUnitOfWork uow)
        {
            _sales = sales;
            _products = products;
            _batches = batches;
            _uow = uow;
        }

        public async Task<Result<List<Sale>>> GetRecentAsync(int count = 100)
            => Result<List<Sale>>.Success(await _sales.GetRecentAsync(count));

        public async Task<Result<Sale?>> GetByIdAsync(int id)
            => Result<Sale?>.Success(await _sales.GetWithDetailsAsync(id));

        public async Task<Result<int>> CheckoutAsync(CheckoutDto dto)
        {
            if (dto.Lines.Count == 0)
                return Result<int>.Failure("Cart is empty.");
            if (dto.PaidAmount < 0 || dto.Discount < 0)
                return Result<int>.Failure("Invalid payment amounts.");

            var today = DateTime.Today;
            var allocations = new List<(int BatchId, int Qty)>();
            var lines = new List<SaleDetail>();

            foreach (var line in dto.Lines)
            {
                if (line.Quantity <= 0)
                    return Result<int>.Failure("Quantity must be positive.");

                var product = await _products.GetByIdAsync(line.ProductId);
                if (product is null)
                    return Result<int>.Failure($"Product {line.ProductId} not found.");

                var candidates = await _batches.GetFefoCandidatesAsync(line.ProductId, today);
                var remaining = line.Quantity;
                var lineAlloc = new List<(int BatchId, int Qty)>();

                foreach (var batch in candidates)
                {
                    if (remaining <= 0) break;
                    var take = Math.Min(remaining, batch.Quantity);
                    lineAlloc.Add((batch.Id, take));
                    remaining -= take;
                }

                if (remaining > 0)
                    return Result<int>.Failure($"Insufficient stock for {product.Name} (need {line.Quantity}).");

                allocations.AddRange(lineAlloc);

                foreach (var (batchId, qty) in lineAlloc)
                {
                    lines.Add(new SaleDetail
                    {
                        ProductId = line.ProductId,
                        BatchId = batchId,
                        Quantity = qty,
                        UnitPrice = line.UnitPrice,
                        Subtotal = line.UnitPrice * qty
                    });
                }
            }

            foreach (var (batchId, qty) in allocations)
            {
                var batch = await _batches.GetByIdAsync(batchId);
                if (batch is null) return Result<int>.Failure("Batch vanished during checkout.");
                batch.Quantity -= qty;
                _batches.Update(batch);
            }

            var subtotal = lines.Sum(l => l.Subtotal);
            var total = subtotal - dto.Discount;
            if (total < 0) return Result<int>.Failure("Discount exceeds total.");

            var sale = new Sale
            {
                CustomerId = dto.CustomerId,
                UserId = dto.UserId,
                SaleDate = DateTime.Now,
                TotalAmount = total,
                Discount = dto.Discount,
                PaidAmount = dto.PaidAmount,
                PaymentMethod = dto.PaymentMethod,
                Details = lines
            };

            await _sales.AddAsync(sale);
            await _uow.SaveChangesAsync();
            return Result<int>.Success(sale.Id);
        }
    }
}
