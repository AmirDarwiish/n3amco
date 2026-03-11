using DairySystem.Api;
using Microsoft.EntityFrameworkCore;
public class SaleService : ISaleService
{
    private readonly ApplicationDbContext _context;
    private readonly IJournalService _journalService;


    public SaleService(ApplicationDbContext context, IJournalService journalService)
    {
        _context = context;
        _journalService = journalService; 

    }

    public async Task<int> CreateSaleAsync(CreateSaleCommand request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        var sale = new Sale
        {
            Date = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CustomerId = request.CustomerId
        };

        decimal totalAmount = 0;
        decimal totalCost = 0;

        foreach (var item in request.Items)
        {
            var remainingQty = item.Quantity;
            decimal itemCost = 0;

            var batches = await _context.ProductBatches
                .Where(x => x.ProductId == item.ProductId && x.RemainingQuantity > 0)
                .OrderBy(x => x.ProductionDate)
                .ToListAsync();

            foreach (var batch in batches)
            {
                if (remainingQty <= 0)
                    break;

                var taken = Math.Min(batch.RemainingQuantity, remainingQty);

                batch.RemainingQuantity -= taken;

                itemCost += taken * batch.CostPrice;
                remainingQty -= taken;
            }

            if (remainingQty > 0)
                throw new Exception("Not enough stock");

            var saleItem = new SaleItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                SellingPrice = item.Price,
                CostPrice = itemCost / item.Quantity,
                TotalPrice = item.Quantity * item.Price,
                TotalCost = itemCost
            };

            sale.Items.Add(saleItem);

            totalAmount += saleItem.TotalPrice;
            totalCost += saleItem.TotalCost;
        }

        sale.TotalAmount = totalAmount;
        sale.TotalCost = totalCost;

        _context.Sales.Add(sale);

        // 🔥 Customer Logic
        // بعد
        if (request.CustomerId.HasValue && !request.IsPaid)
        {
            var customer = await _context.Customers.FindAsync(request.CustomerId.Value);
            if (customer != null)
            {
                customer.CurrentBalance += totalAmount;
                _context.CustomerLedgers.Add(new CustomerLedger
                {
                    CustomerId = customer.Id,
                    Amount = totalAmount,
                    Type = CustomerTransactionType.Sale,
                    Date = DateTime.UtcNow,
                    Notes = $"فاتورة #{sale.Id}"
                });
                await _context.SaveChangesAsync();
            }
        }

        await _journalService.CreateSaleEntryAsync(sale, request.IsPaid);
        await transaction.CommitAsync();

        return sale.Id;
    }
}