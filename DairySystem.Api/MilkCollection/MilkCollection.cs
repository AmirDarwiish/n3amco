using DairySystem.Api.suppliers;

public class MilkCollection
{
    public int Id { get; set; }

    public int SupplierId { get; set; }
    public int ProductId { get; set; } 

    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime Date { get; set; }

    public DateTime CreatedAt { get; set; }

    public Supplier Supplier { get; set; }
    public Product Product { get; set; }
}