public class ProductBatch
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    // Quantities
    public decimal Quantity { get; set; }           // الكمية الأصلية
    public decimal RemainingQuantity { get; set; }  // المتبقي (FIFO)

    // Cost
    public decimal CostPrice { get; set; }

    // Dates
    public DateTime ProductionDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    // Tracking
    public BatchSourceType SourceType { get; set; }
    public int ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Product Product { get; set; }
}
public enum ProductType
{
    Milk = 1,
    Cheese = 2,
    Yogurt = 3,
    Other = 4
}

public enum BatchSourceType
{
    MilkCollection = 1,
    Production = 2,
    Adjustment = 3,
    Purchase=4,
}