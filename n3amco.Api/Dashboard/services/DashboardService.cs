using n3amco.Api;
using Microsoft.EntityFrameworkCore;

public class DashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetDashboard(DashboardQuery query)
    {
        var from = (query.From ?? DateTime.UtcNow.Date.AddDays(-7)).Date;
        var to = (query.To ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        // 🥇 Overview
        var salesQuery = _context.Sales
            .Where(x => x.Date >= from && x.Date <= to);

        var totalSales = await salesQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
        var totalCost = await salesQuery.SumAsync(x => (decimal?)x.TotalCost) ?? 0;

        var totalMilk = await _context.MilkCollections
            .Where(x => x.Date >= from && x.Date <= to)
            .SumAsync(x => (decimal?)x.Quantity) ?? 0;

        var supplierPayments = await _context.SupplierPayments
    .Where(x => x.Date >= from && x.Date <= to)
    .SumAsync(x => (decimal?)x.Amount) ?? 0;

        // 🥈 Sales Chart
        var salesChart = await salesQuery
.GroupBy(x => x.Date)
.Select(g => new SalesChartDto
            {
                Date = g.Key,
                TotalSales = g.Sum(x => x.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // 🥉 Top Customers
        var topCustomers = await _context.Sales
         .Where(x => x.CustomerId != null && x.Date >= from && x.Date <= to)
         .GroupBy(x => new { x.CustomerId, x.Customer.Name })
         .Select(g => new TopCustomerDto
         {
             CustomerId = g.Key.CustomerId.Value,
             Name = g.Key.Name,
             TotalSales = g.Sum(x => x.TotalAmount)
         })
         .OrderByDescending(x => x.TotalSales)
         .Take(5)
         .ToListAsync();

        // 🧊 Low Stock
        var lowStock = await _context.Products
      .Select(p => new LowStockDto
      {
          ProductId = p.Id,
          Name = p.Name,
          RemainingQuantity = _context.ProductBatches
              .Where(b => b.ProductId == p.Id)
              .Sum(b => (decimal?)b.RemainingQuantity) ?? 0
      })
      .Where(x => x.RemainingQuantity < 10)
      .OrderBy(x => x.RemainingQuantity)
      .ToListAsync();
        return new
        {
            overview = new DashboardOverviewDto
            {
                TotalSales = totalSales,
                TotalCost = totalCost,
                TotalProfit = totalSales - totalCost,
                TotalMilkCollected = totalMilk,
                TotalSupplierPayments = supplierPayments
            },
            salesChart,
            topCustomers,
            lowStock
        };
    }
}