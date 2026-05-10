public class SaleItem
{
    public int Id { get; set; }

    public int SaleId { get; set; }
    public int ProductId { get; set; }

    public decimal Quantity { get; set; }

    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; } 

    public decimal TotalPrice { get; set; }
    public decimal TotalCost { get; set; }

    public Sale Sale { get; set; }
    public Product Product { get; set; }
}