public class DashboardOverviewDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }

    public decimal TotalMilkCollected { get; set; }
    public decimal TotalSupplierPayments { get; set; }
}

public class SalesChartDto
{
    public DateTime Date { get; set; }
    public decimal TotalSales { get; set; }
}

public class TopCustomerDto
{
    public int CustomerId { get; set; }
    public string Name { get; set; }
    public decimal TotalSales { get; set; }
}

public class LowStockDto
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public decimal RemainingQuantity { get; set; }

}